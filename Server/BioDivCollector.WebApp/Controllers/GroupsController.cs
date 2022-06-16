using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BioDivCollector.DB.Models.Domain;
using RestSharp;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using BioDivCollector.WebApp.Helpers;

namespace BioDivCollector.WebApp.Controllers
{
    public class GroupsController : Controller
    {
        private BioDivContext _context = new BioDivContext();
        public IConfiguration Configuration { get; }

        public GroupsController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // GET: GroupsGetUsersByRole
        public async Task<IActionResult> Index()
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, _context);
            var bioDivContext =  _context.Groups.Include(p => p.GroupStatus)
                .Include(p => p.GroupProjects).ThenInclude(p => p.Project.ProjectConfigurator)
                .Include(p => p.GroupChangeLogs).ThenInclude(pchl => pchl.ChangeLog)
                .Include(p => p.GroupUsers).ThenInclude(pg => pg.User)
                .Include(p => p.GroupProjects).ThenInclude(pp => pp.Project)
                .Include(p => p.Status);

            // users pl and pm projects
            List<Project> projects = await _context.Projects.Include(m => m.ProjectGroups).Where(m => m.ProjectManager == user || m.ProjectConfigurator == user).ToListAsync();

            List<GroupViewModel> groups = new List<GroupViewModel>();
            foreach (Group g in await bioDivContext.ToListAsync())
            {
                if ((g.GroupChangeLogs.Count() > 0) && (g.GroupChangeLogs?.First()?.ChangeLog?.User == user))
                {
                    GroupViewModel gvm = new GroupViewModel() { Group = g };
                    gvm.Editable = true;
                    gvm.ShowOnly = false;
                    groups.Add(gvm);
                }
                else if (g.CreatorId == user.UserId)
                {
                    GroupViewModel gvm = new GroupViewModel() { Group = g };
                    gvm.Editable = true;
                    gvm.ShowOnly = false;
                    groups.Add(gvm);
                }
                else if (User.IsInRole("DM"))
                {
                    GroupViewModel gvm = new GroupViewModel() { Group = g };
                    gvm.Editable = true;
                    gvm.ShowOnly = false;
                    groups.Add(gvm);
                }
                else if ((User.IsInRole("PL")) || (User.IsInRole("PM")))
                {
                    GroupViewModel gvm = new GroupViewModel() { Group = g };
                    gvm.Editable = false;

                    if (projects.Where(m => m.ProjectGroups.Select(m => m.Group).Contains(g)).Any()) gvm.Editable = true;

                    gvm.ShowOnly = true;
                    groups.Add(gvm);

                }
                // only readable
                else if (g.GroupUsers.Any(m => m.UserId == user.UserId))
                {
                    GroupViewModel gvm = new GroupViewModel() { Group = g };
                    gvm.Editable = false;
                    gvm.ShowOnly = false;
                    groups.Add(gvm);
                }
            }

            return View(groups.Distinct());
        }


        public async Task<IActionResult> GetUsersByRole(Guid? id, string role)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mgroup = await _context.Groups
                .Include(m => m.GroupUsers)
                .FirstOrDefaultAsync(m => m.GroupId == id);
            if (mgroup == null)
            {
                return NotFound();
            }

            List<string> usersList = new List<string>();
            if (role == "ALL") usersList = UserHelper.GetAllUsers(Configuration["JWT:Admin-Token-Url"], Configuration["JWT:Admin-Url"], Configuration["JWT:Admin-Key"], Configuration["JWT:Admin-User"], Configuration["JWT:Admin-Password"]);
            else usersList = UserHelper.GetAllUsersByRole(role, Configuration["JWT:Admin-Url"], Configuration["JWT:ClientId"], Configuration["JWT:Admin-Token-Url"], Configuration["JWT:Admin-Key"], Configuration["JWT:Admin-User"], Configuration["JWT:Admin-Password"]);
            
            List<SelectedUserPoco> returnList = new List<SelectedUserPoco>();
            foreach (string userid in usersList)
            {
                DB.Models.Domain.User u = await _context.Users.FindAsync(userid);
                SelectedUserPoco sup = new SelectedUserPoco() { myUser = u, myGroup = mgroup };
                if (u != null) returnList.Add(sup);
            }

            string json = JsonConvert.SerializeObject(returnList);
            return Content(json, "application/json");
        }


        [HttpPost]
        public async Task<IActionResult> EditUsersInGroup([FromBody] Users data)
        {
            var mgroup = await _context.Groups
                .Include(m => m.GroupUsers)
                .FirstOrDefaultAsync(m => m.GroupId == data.guid);
            if (mgroup == null)
            {
                return NotFound();
            }

            foreach (GroupUser guOld in mgroup.GroupUsers)
            {
                _context.GroupsUsers.Remove(guOld);
            }


            foreach (UserIds userid in data.items)
            {
                DB.Models.Domain.User u = await _context.Users.FindAsync(userid.value);
                if (u!=null)
                {
                    GroupUser gu = new GroupUser() { Group = mgroup, User = u };
                    _context.GroupsUsers.Add(gu);
                }

            }


                await _context.SaveChangesAsync();


            return Content("OK", "application/json");
        }


        public async Task<IActionResult> GetReadonlyUsersByProject(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            List<ProjectGroup> readOnlyGroups = await _context.ProjectsGroups
                .Include(m => m.Group).ThenInclude(u => u.GroupUsers).ThenInclude(g => g.User)
                .Where(m => m.ReadOnly == true && m.ProjectId == id).ToListAsync();

            
            List<string> usersList = UserHelper.GetAllUsers(Configuration["JWT:Admin-Token-Url"], Configuration["JWT:Admin-Url"], Configuration["JWT:Admin-Key"], Configuration["JWT:Admin-User"], Configuration["JWT:Admin-Password"]);
            List<SelectedUserPoco> returnList = new List<SelectedUserPoco>();
            foreach (string userid in usersList)
            {
                DB.Models.Domain.User u = await _context.Users.FindAsync(userid);
                if (u != null)
                {
                    SelectedUserPoco sup = new SelectedUserPoco() { myUser = u };
                    if (readOnlyGroups.Any(m => m.Group.GroupUsers.Any(z => z.UserId == u.UserId)))
                    {
                        sup.selected = true;
                    }
                    returnList.Add(sup);
                }
            }

            string json = JsonConvert.SerializeObject(returnList);
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> EditReadOnlyUsersInProject([FromBody] Users data)
        {
            Project p = await _context.Projects.FindAsync(data.guid);
            List<ProjectGroup> readOnlyGroups = await _context.ProjectsGroups
                 .Include(m => m.Group).ThenInclude(u => u.GroupUsers).ThenInclude(g => g.User)
                 .Where(m => m.ReadOnly == true && m.ProjectId == data.guid).ToListAsync();
            if (readOnlyGroups?.Count() == 0)
            {
                Group gr = new Group() { GroupName = "Lesende für " + p.ProjectName };
                gr.GroupStatusId = GroupStatusEnum.Gruppendaten_gueltig;
                ProjectGroup newReadonly = new ProjectGroup() { Group = gr, Project = p, ReadOnly = true };
                readOnlyGroups = new List<ProjectGroup>();
                readOnlyGroups.Add(newReadonly);
                _context.ProjectsGroups.Add(newReadonly);
            }
            else
            {
                foreach (GroupUser guOld in readOnlyGroups.First().Group?.GroupUsers)
                {
                    _context.GroupsUsers.Remove(guOld);
                }
            }


            foreach (UserIds userid in data.items)
            {
                DB.Models.Domain.User u = await _context.Users.FindAsync(userid.value);
                if (u != null)
                {
                    GroupUser gu = new GroupUser() { Group = readOnlyGroups.First().Group, User = u };
                    _context.GroupsUsers.Add(gu);
                }

            }


            await _context.SaveChangesAsync();


            return Content("OK", "application/json");
        }



        public async Task<IActionResult> GetProjectsForGroup(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mgroup = await _context.Groups
                .Include(m => m.GroupProjects).ThenInclude(m => m.Geometries)
                .Include(m => m.GroupProjects).ThenInclude(m => m.Records)
                .Include(m => m.GroupUsers)
                .FirstOrDefaultAsync(m => m.GroupId == id);
            if (mgroup == null)
            {
                return NotFound();
            }

            DB.Models.Domain.User me = UserHelper.GetCurrentUser(User, _context);

            List<Project> projects;
            if (User.IsInRole("DM")) projects = await _context.Projects.Include(m => m.ProjectConfigurator).Include(m => m.ProjectManager).Include(m => m.ProjectGroups).ThenInclude(m => m.Group).Where(m => m.StatusId != StatusEnum.deleted).ToListAsync();
            else projects = await _context.Projects.Include(m => m.ProjectConfigurator).Include(m => m.ProjectManager).Include(m => m.ProjectGroups).ThenInclude(m => m.Group).Where(m => m.ProjectConfigurator.UserId == me.UserId || m.ProjectManager.UserId == me.UserId).Where(m=>m.StatusId!=StatusEnum.deleted).ToListAsync();            
            List<SelectedProjectPoco> returnList = new List<SelectedProjectPoco>();
            foreach (Project p in projects)
            {
                SelectedProjectPoco sup = new SelectedProjectPoco() { myProject = p, myGroup = mgroup };

                returnList.Add(sup);
            }

            string json = JsonConvert.SerializeObject(returnList);
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> EditProjectsForGroup([FromBody] Projects data)
        {
            var mgroup = await _context.Groups
                .Include(m => m.GroupProjects).ThenInclude(m=>m.Geometries)
                .Include(m => m.GroupProjects).ThenInclude(m => m.Records)
                .FirstOrDefaultAsync(m => m.GroupId == data.guid);
            if (mgroup == null)
            {
                return NotFound();
            }

            List<ProjectGroup> oldPgGroups = mgroup.GroupProjects.ToList();
            List<Project> newPGroups = new List<Project>();

            foreach (ProjectIds projectid in data.items)
            {
                Project p = await _context.Projects.FindAsync(new Guid(projectid.value));

                if (p != null)
                {
                    if (oldPgGroups.Any(m => m.ProjectId == p.ProjectId))
                    {
                        newPGroups.Add(p);
                    }
                    else
                    {
                        ProjectGroup pg = new ProjectGroup() { Project = p, Group = mgroup };
                        _context.ProjectsGroups.Add(pg);
                    }
                }

            }

            // delete all not used projectgroups and only if there is no records or geometrie
            List<ProjectGroup> toRemovePGs = oldPgGroups?.Where(m => newPGroups.All(m2 => m2.ProjectId != m.ProjectId)).ToList();
            foreach (ProjectGroup pg in toRemovePGs)
            {
                if ((pg.Geometries.Where(m=>m.StatusId!=StatusEnum.deleted).Count() == 0) && (pg.Records.Where(m => m.StatusId != StatusEnum.deleted).Count() == 0))
                {
                    mgroup.GroupProjects.Remove(pg);
                    _context.ProjectsGroups.Remove(pg);
                }
            }


            await _context.SaveChangesAsync();


            return Content("OK", "application/json");
        }

        //bac20201208 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        // Forms
        //public async Task<IActionResult> GetFormsForGroup(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var mgroup = await _context.Groups
        //        .Include(m => m.GroupUsers)
        //        .FirstOrDefaultAsync(m => m.GroupId == id);
        //    if (mgroup == null)
        //    {
        //        return NotFound();
        //    }

        //    DB.Models.Domain.User me = UserHelper.GetCurrentUser(User, _context);

        //    List<Form> forms = await _context.Forms.Include(m => m.FormGroups).ThenInclude(fg => fg.Group).ToListAsync();
        //    List<SelectedFormsPoco> returnList = new List<SelectedFormsPoco>();
        //    foreach (Form f in forms)
        //    {
        //        SelectedFormsPoco sup = new SelectedFormsPoco() { myForm = f, myGroup = mgroup };
        //        returnList.Add(sup);
        //    }

        //    string json = JsonConvert.SerializeObject(returnList);
        //    return Content(json, "application/json");
        //} 


        //bac20201208 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        //[HttpPost]
        //public async Task<IActionResult> EditFormsForGroup([FromBody] Forms data)
        //{
        //    var mgroup = await _context.Groups
        //        .Include(m => m.GroupForms)
        //        .FirstOrDefaultAsync(m => m.GroupId == data.guid);
        //    if (mgroup == null)
        //    {
        //        return NotFound();
        //    }

        //    foreach (GroupForm gfOld in mgroup.GroupForms)
        //    {
        //        _context.GroupsForms.Remove(gfOld);
        //    }


        //    foreach (FormIds formid in data.items)
        //    {
        //        Form f = await _context.Forms.FindAsync(Int32.Parse(formid.value));

        //        if (f != null)
        //        {
        //            GroupForm gf = new GroupForm() { Form = f, Group = mgroup };
        //            _context.GroupsForms.Add(gf);
        //        }

        //    }


        //    await _context.SaveChangesAsync();


        //    return Content("OK", "application/json");
        //}       

        // GET: Groups/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mgroup = await _context.Groups
                .Include(m => m.GroupStatus)
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.GroupId == id);
            if (mgroup == null)
            {
                return NotFound();
            }

            return View(mgroup);
        }

        // GET: Groups/Create
        public IActionResult Create()
        {
            ViewData["GroupStatusId"] = new SelectList(_context.GroupStatuses, "Id", "Description");
            ViewData["StatusId"] = new SelectList(_context.Statuses, "Id", "Description");
            return View();
        }

        // POST: Groups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,GroupName,ID_Extern,GroupStatusId,StatusId")] Group mgroup)
        {
            if (ModelState.IsValid)
            { 
                mgroup.GroupId = Guid.NewGuid();
                User user = Helpers.UserHelper.GetCurrentUser(User, _context);
                ChangeLog cl = new ChangeLog() { User = user, Log = "created group" };
                ChangeLogGroup clg = new ChangeLogGroup() { ChangeLog = cl, Group = mgroup };
                mgroup.GroupChangeLogs = new List<ChangeLogGroup>();
                mgroup.GroupChangeLogs.Add(clg);
                mgroup.CreatorId = user.UserId;

                _context.Add(mgroup);
                await _context.SaveChangesAsync();
                return RedirectToAction("Edit","Groups",new { @id = mgroup.GroupId });
            }
            ViewData["GroupStatusId"] = new SelectList(_context.GroupStatuses, "Id", "Description", mgroup.GroupStatusId);
            ViewData["StatusId"] = new SelectList(_context.Statuses, "Id", "Description", mgroup.StatusId);
            return View(mgroup);
        }

        // GET: Groups/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mgroup = await _context.Groups.Include(m => m.GroupProjects).ThenInclude(m => m.Project)
                .Include(m => m.GroupChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User)
                .Where(m => m.GroupId == id).FirstOrDefaultAsync();
            if (mgroup == null)
            {
                return NotFound();
            }
            ViewData["GroupStatusId"] = new SelectList(_context.GroupStatuses, "Id", "Description", mgroup.GroupStatusId);
            ViewData["StatusId"] = new SelectList(_context.Statuses, "Id", "Description", mgroup.StatusId);
            return View(mgroup);
        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("GroupId,GroupName,ID_Extern,GroupStatusId, CreatorId")] Group mgroup)
        {
            if (id != mgroup.GroupId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
                Status ok = await _context.Statuses.Where(m => m.Id == StatusEnum.changed).FirstOrDefaultAsync();
                mgroup.StatusId = ok.Id;

                // if group has Gruppe bereit -> change the corresponding projects to Bereit
                if (mgroup.GroupStatusId == GroupStatusEnum.Gruppe_bereit)
                {
                    List<ProjectGroup> pgs = await _context.ProjectsGroups.Where(m => m.GroupId == mgroup.GroupId).ToListAsync();

                    foreach (ProjectGroup pg in pgs)
                    {
                        Project p = await _context.Projects.Include(m=>m.ProjectStatus).Where(m => m.ProjectId == pg.ProjectId).FirstOrDefaultAsync();
                        if (p.ProjectStatus.Id == ProjectStatusEnum.Projekt_neu)
                        {
                            ProjectStatus bereit = await _context.ProjectStatuses.Where(m => m.Id == ProjectStatusEnum.Projekt_bereit).FirstAsync();
                            p.ProjectStatus = bereit;
                            _context.Entry(p).State = EntityState.Modified;
                        }
                    }
                }

                try
                {
                    _context.Update(mgroup);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(mgroup.GroupId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            //}
        }

        // GET: Groups/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mgroup = await _context.Groups
                .Include(m => m.GroupStatus)
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.GroupId == id);
            if (mgroup == null)
            {
                return NotFound();
            }

            return View(mgroup);
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var mgroup = await _context.Groups.Include(m => m.GroupProjects).ThenInclude(m => m.Project).Where(m => m.GroupId == id).FirstOrDefaultAsync();
            if ((mgroup != null) && (mgroup.GroupProjects.Count == 0))
            {
                _context.Groups.Remove(mgroup);
                _context.Entry(mgroup).State = EntityState.Deleted;
            }
            else if (mgroup.GroupProjects.Count > 0)
            {
                // maybe we have deleted projects that have still references (only status of deleted project is changed). So we clean all this references
                foreach (ProjectGroup pg in mgroup.GroupProjects)
                {
                    if (pg.Project.StatusId == StatusEnum.deleted)
                    {
                        _context.Entry(pg).Collection(m => m.Geometries).Load();
                        foreach (ReferenceGeometry rg in pg.Geometries)
                        {
                            _context.Entry(rg).Collection(m => m.Records).Load();
                            rg.Records.RemoveAll(m => m.ProjectGroupProjectId == pg.ProjectId && m.ProjectGroupGroupId == pg.GroupId);
                        }
                        pg.Geometries.RemoveAll(m => m.ProjectGroupProjectId == pg.ProjectId && m.ProjectGroupGroupId == pg.GroupId);
                        _context.Entry(pg).Collection(m => m.Records).Load();
                        pg.Records.RemoveAll(m => m.ProjectGroupProjectId == pg.ProjectId && m.ProjectGroupGroupId == pg.GroupId);
                    }
                }
                // try to delete the group
                try
                {
                    _context.Groups.Remove(mgroup);
                    _context.Entry(mgroup).State = EntityState.Deleted;
                    await _context.SaveChangesAsync();
                }
                finally
                {
                }

                return RedirectToAction(nameof(Index));
            }
            else if (mgroup != null)
            {
                mgroup.StatusId = StatusEnum.deleted;
                _context.Entry(mgroup).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(Guid id)
        {
            return _context.Groups.Any(e => e.GroupId == id);
        }
    }

    public class SelectedUserPoco
    {
        [JsonIgnore]
        public DB.Models.Domain.User myUser { get; set; }
        [JsonIgnore]
        public Group myGroup { get; set; }
        public string item { get { return myUser.FirstName + " " + myUser.Name + " (" + myUser.Email + ")"; } }
        public string value { get { return myUser.UserId;  } }

        private bool _selected;
        public bool selected
        {
            get
            {
                if (myGroup == null) return _selected;
                foreach (GroupUser gu in myGroup.GroupUsers)
                {
                    if (myUser.UserId == gu.UserId) return true;
                }
                return false;
            }
            set
            {
                _selected = value;
            }
        }
    }

    public class SelectedProjectPoco
    {
        [JsonIgnore]
        public DB.Models.Domain.Project myProject { get; set; }
        [JsonIgnore]
        public Group myGroup { get; set; }
        public string item { get { return myProject.ProjectName; } }
        public string value { get { return myProject.ProjectId.ToString(); } }
        private bool _selected;
        public bool selected
        {
            get
            {
                if (myGroup == null) return _selected;
                foreach (ProjectGroup pg in myProject.ProjectGroups)
                {
                    if (pg.GroupId == myGroup.GroupId) return true;
                }
                return false;
            }

            set
            {
                _selected = value;
            }
        }
    }

    public class GroupViewModel : Group
    {
        public Group Group { get; set; }
        public bool Editable { get; set; }
        public bool ShowOnly { get; set; }
    }

}
