using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.WebApp.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using GeoAPI.Geometries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using RunProcessAsTask;

namespace BioDivCollector.WebApp.Controllers
{
    public class ProjectsController : Controller
    {
        private BioDivContext db = new BioDivContext();

        public IConfiguration Configuration { get; }

        public ProjectsController(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        private async Task<List<ProjectPocoForIndex>> CreateProjectPocoForIndex(User user)
        {
            List<Project> projects = new List<Project>();
            List<Project> editProjectSetting = new List<Project>();

            List<Project> erfassendeProjects = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            }
            if (User.IsInRole("PL"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            }

            editProjectSetting = projects;

            List<Project> nurLesendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE);
            nurLesendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(nurLesendeProjects);
            projects.AddRange(erfassendeProjects);



            List<ProjectPocoForIndex> newProjectList = new List<ProjectPocoForIndex>();


            foreach (Project p in projects.Distinct())
            {
                List<ProjectGroup> pgs = await db.ProjectsGroups
                    .Where(k => k.ProjectId == p.ProjectId).ToListAsync();

                int recordCount = 0;
                int geometryCount = 0;
                string myGroup = "";
                foreach (ProjectGroup pg in pgs)
                {
                    await db.Entry(pg).Collection(mm => mm.Geometries).Query().Include(mmm => mmm.Records).LoadAsync();
                    await db.Entry(pg).Collection(mm => mm.Records).LoadAsync();

                    geometryCount += pg.Geometries.Where(m => m.StatusId != StatusEnum.deleted).Count();
                    recordCount += pg.Geometries.Where(m => m.StatusId != StatusEnum.deleted).Select(m => m.Records.Where(zz => zz.StatusId != StatusEnum.deleted).Count()).Sum();
                    recordCount += pg.Records.Where(m => m.StatusId != StatusEnum.deleted && m.GeometryId == null).Count();

                    await db.Entry(pg).Reference(zzz => zzz.Group).Query().Include(m => m.GroupUsers).LoadAsync();

                    if (pg.Group.GroupUsers.Where(o => o.UserId == user.UserId).Any()) myGroup = pg.Group.GroupName;
                }


                await db.Entry(p).Reference(m => m.ProjectConfigurator).LoadAsync();
                await db.Entry(p).Reference(m => m.ProjectManager).LoadAsync();
                await db.Entry(p).Reference(m => m.ProjectStatus).LoadAsync();

                ProjectPocoForIndex pp = new ProjectPocoForIndex() { Project = p, RecordCount = recordCount, GeometryCount = geometryCount, MyGroup = myGroup };
                if (!erfassendeProjects.Contains(p)) pp.IsReadOnly = true;
                if (editProjectSetting.Contains(p)) pp.IsPKOrPLOrDM = true;
                else pp.IsReadOnly = false;
                newProjectList.Add(pp);

            }

            return newProjectList;
        }


        public async Task<IActionResult> Index()
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            ViewData["Username"] = user.UserId;

            return View(await CreateProjectPocoForIndex(user));
        }

        public async Task<IActionResult> ExportProjectList()
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            List<ProjectPocoForIndex> projectPocos = await CreateProjectPocoForIndex(user);

            var stream = new MemoryStream();
            using (var writeFile = new StreamWriter(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
            {
                var csv = new CsvWriter(writeFile, new CultureInfo("de-CH"));
                csv.Context.RegisterClassMap<ProjectPocoForExportMap>();
                csv.WriteRecords(projectPocos);
            }
            stream.Position = 0; //reset stream
            return File(stream, "application/octet-stream", "Projects.csv");
        }



        public async Task<IActionResult> Delete(Guid id)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            List<Project> projects = new List<Project>();
            if (User.IsInRole("DM")) projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
            if (User.IsInRole("PK")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            if (User.IsInRole("PL")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));

            Project changeProject = projects.Where(m => m.ProjectId == id).FirstOrDefault();
            if (changeProject == null) return NotFound();

            changeProject.StatusId = StatusEnum.deleted;
            ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Deleted Project", User = user };
            ChangeLogProject clp = new ChangeLogProject() { ChangeLog = cl, Project = changeProject };
            db.ChangeLogs.Add(cl);
            db.ChangeLogsProjects.Add(clp);
            db.Entry(changeProject).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DetailsDataEdit(Guid? id)
        {
            if (id == null)
            {
                string idstring = HttpContext.Session.GetString("Project");
                if (idstring == null) return RedirectToAction("Index");
                id = new Guid(idstring);
            }

            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            List<Project> projects = new List<Project>();
            List<Project> erfassendeProjects = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            }
            if (User.IsInRole("PL"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            }

            List<Project> nurLesendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE);
            nurLesendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(nurLesendeProjects);
            projects.AddRange(erfassendeProjects);


            if (projects.Where(m => m.ProjectId == id).Count() == 0) return StatusCode(StatusCodes.Status403Forbidden);

            /*Project newProject = db.Projects
                    .Include(m => m.Status)
                    .Include(m => m.ProjectStatus)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Records)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.GroupStatus)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.GroupStatus)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.Creator)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.GroupUsers).ThenInclude(gu => gu.User)
                    .Include(m => m.ProjectManager)
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).First();*/


            Project newProject = db.Projects
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).First();

            await db.Entry(newProject).Reference(m => m.ProjectConfigurator).LoadAsync();
            await db.Entry(newProject).Reference(m => m.ProjectManager).LoadAsync();
            await db.Entry(newProject).Reference(m => m.ProjectStatus).LoadAsync();
            await db.Entry(newProject).Reference(m => m.Status).LoadAsync();

            await db.Entry(newProject).Collection(m => m.ProjectGroups).LoadAsync();

            foreach (ProjectGroup pg in newProject.ProjectGroups)
            {
                await db.Entry(pg).Collection(m => m.Geometries).LoadAsync();
                await db.Entry(pg).Collection(m => m.Records).LoadAsync();

                await db.Entry(pg).Reference(m => m.Group).Query().Include(zz => zz.GroupStatus).Include(zz => zz.Creator).Include(zz => zz.GroupUsers).ThenInclude(gu => gu.User).LoadAsync();
            }

            if (newProject == null) return StatusCode(500);
            HttpContext.Session.SetString("Project", id.ToString());
            ViewData["ProjectName"] = newProject.ProjectName;
            ViewData["Username"] = user.UserId;
            ViewData["ProjectId"] = newProject.ProjectId;


            if (!erfassendeProjects.Contains(newProject))
            {
                ViewData["ReadOnly"] = true;
                if (nurLesendeProjects.Contains(newProject)) ViewData["OnlyLE"] = true;
                else ViewData["OnlyLE"] = false;
            }
            else
            {
                ViewData["ReadOnly"] = false;
                ViewData["OnlyLE"] = false;
            }


            foreach (ProjectGroup gr in newProject.ProjectGroups)
            {
                if (gr.Group.GroupUsers.Any(m => m.UserId == user.UserId)) ViewData["MyGroup"] = gr.GroupId;

            }
            return View(newProject);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                string idstring = HttpContext.Session.GetString("Project");
                if (idstring == null) return RedirectToAction("Index");
                id = new Guid(idstring);
            }

            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            List<Project> projects = new List<Project>();
            List<Project> erfassendeProjects = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            }
            if (User.IsInRole("PL"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            }

            List<Project> nurLesendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE);
            nurLesendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(nurLesendeProjects);
            projects.AddRange(erfassendeProjects);


            if (projects.Where(m => m.ProjectId == id).Count() == 0) return StatusCode(StatusCodes.Status403Forbidden);

            /*Project newProject = db.Projects
                    .Include(m => m.Status)
                    .Include(m => m.ProjectStatus)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Records)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.GroupStatus)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.GroupStatus)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.Creator)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.GroupUsers).ThenInclude(gu => gu.User)
                    .Include(m => m.ProjectManager)
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).First();*/

            Project newProject = db.Projects
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).First();

            await db.Entry(newProject).Reference(m => m.ProjectConfigurator).LoadAsync();
            await db.Entry(newProject).Reference(m => m.ProjectManager).LoadAsync();
            await db.Entry(newProject).Reference(m => m.ProjectStatus).LoadAsync();
            await db.Entry(newProject).Reference(m => m.Status).LoadAsync();

            await db.Entry(newProject).Collection(m => m.ProjectGroups).LoadAsync();
            await db.Entry(newProject).Collection(m => m.ProjectThirdPartyTools).Query().Include(m=>m.ThirdPartyTool).LoadAsync();

            newProject.ProjectThirdPartyToolsString = string.Join(",", newProject.ProjectThirdPartyTools.Select(m => m.ThirdPartyTool.Name).ToList());
            
            foreach (ProjectGroup pg in newProject.ProjectGroups)
            {
                await db.Entry(pg).Collection(m => m.Geometries).LoadAsync();
                await db.Entry(pg).Collection(m => m.Records).LoadAsync();

                await db.Entry(pg).Reference(m => m.Group).Query().Include(zz=>zz.GroupStatus).Include(zz=>zz.Creator).Include(zz=>zz.GroupUsers).ThenInclude(gu => gu.User).LoadAsync();
            }




            if (newProject == null) return StatusCode(500);
            HttpContext.Session.SetString("Project", id.ToString());
            ViewData["ProjectName"] = newProject.ProjectName;
            ViewData["Username"] = user.UserId;
            ViewData["ProjectId"] = newProject.ProjectId;


            if (!erfassendeProjects.Contains(newProject))
            {
                ViewData["ReadOnly"] = true;
                if (nurLesendeProjects.Contains(newProject)) ViewData["OnlyLE"] = true;
                else ViewData["OnlyLE"] = false;
            }
            else
            {
                ViewData["ReadOnly"] = false;
                ViewData["OnlyLE"] = false;
            }


            foreach (ProjectGroup gr in newProject.ProjectGroups)
            {
                if (gr.Group.GroupUsers.Any(m => m.UserId == user.UserId)) ViewData["MyGroup"] = gr.GroupId;

            }
            return View(newProject);
        }

        public IActionResult Create()
        {
            if (User.IsInRole("DM")) return View();
            return RedirectToAction("NotAllowed", "Home");
        }

        // POST: Layers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectName, ProjectNumber, Description, ID_Extern, OGD, ProjectManager")] ProjectPoco projectp)
        {
            Project project = new Project() { ProjectName = projectp.ProjectName, ProjectNumber = projectp.ProjectNumber, Description = projectp.Description, ID_Extern = projectp.ID_Extern, OGD = projectp.OGD };
            if (ModelState.IsValid)
            {
                User user = Helpers.UserHelper.GetCurrentUser(User, db);


                ChangeLog cl = new ChangeLog() { Log = "Created new Project " + project.ProjectName, User = user };
                ChangeLogProject cll = new ChangeLogProject() { ChangeLog = cl, Project = project };
                project.ProjectChangeLogs = new List<ChangeLogProject>();
                project.ProjectChangeLogs.Add(cll);
                project.ProjectManager = await db.Users.FindAsync(projectp.ProjectManager);
                // add ProjectManager to PM Group
                UsersController uc = new UsersController(Configuration);
                await uc.AddRoleToUser(project.ProjectManager.UserId, "PL");
                project.ProjectStatusId = ProjectStatusEnum.Projekt_bereit;
                ProjectStatus ps = await db.ProjectStatuses.Where(m => m.Id == ProjectStatusEnum.Projekt_bereit).FirstOrDefaultAsync();
                project.ProjectStatus = ps;


                db.Add(project);
                await db.SaveChangesAsync();

                HttpContext.Session.SetString("Project", project.ProjectId.ToString());

                return RedirectToAction("Details", "Projects", new { @id = project.ProjectId });
            }
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("ProjectId, ProjectName, ProjectNumber, Description, ProjectThirdPartyToolsString, ProjectConfigurator, ProjectManager, ID_Extern, OGD")] ProjectPoco project)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Project pOld = await db.Projects.Include(m => m.ProjectConfigurator).Include(m => m.ProjectManager).Include(m=>m.ProjectThirdPartyTools).ThenInclude(ptp=>ptp.ThirdPartyTool).Where(m => m.ProjectId == project.ProjectId).FirstOrDefaultAsync();
                    pOld.ProjectName = project.ProjectName;
                    pOld.ProjectNumber = project.ProjectNumber;
                    pOld.Description = project.Description;
                    pOld.ID_Extern = project.ID_Extern;
                    pOld.OGD = project.OGD;

                    if (project.ProjectConfigurator != null)
                    {
                        User newPC = await db.Users.FindAsync(project.ProjectConfigurator);
                        pOld.ProjectConfigurator = newPC;
                        UsersController uc = new UsersController(Configuration);
                        await uc.AddRoleToUser(pOld.ProjectConfigurator.UserId, "PK");
                    }

                    if (project.ProjectManager != null)
                    {
                        User newPM = await db.Users.FindAsync(project.ProjectManager);
                        pOld.ProjectManager = newPM;
                        UsersController uc = new UsersController(Configuration);
                        await uc.AddRoleToUser(pOld.ProjectManager.UserId, "PL");
                    }

                    if (project.ProjectThirdPartyToolsString != null)
                    {
                        pOld.ProjectThirdPartyTools.RemoveRange(0, pOld.ProjectThirdPartyTools.Count);
                        string[] tools = project.ProjectThirdPartyToolsString.Split(',');
                        foreach (string tool in tools)
                        {
                            if (!pOld.ProjectThirdPartyTools.Where(m=>m.ThirdPartyTool.Name == tool).Any())
                            {
                                ProjectThirdPartyTool ptpt = new ProjectThirdPartyTool() { Project = pOld, ThirdPartyTool = db.ThirdPartyTools.Where(tpt => tpt.Name == tool).First() };
                                pOld.ProjectThirdPartyTools.Add(ptpt);
                            }
                        }
                    }

                    db.Update(pOld);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View("Details", project);
        }

        public IActionResult Export()
        {
            return View();
        }

        private async Task<string> ImportTable(string tablename, string geometriecolumn, List<Project> erfassendeProjects, User user)
        {

            string changes = "OK";
            try
            {
                using (var context = new BioDivContext())
                {
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        if (geometriecolumn != "") command.CommandText = "select *, st_astext(" + geometriecolumn + ") as wkt_geometrie from " + tablename + " order by bdcguid_projekt";
                        else command.CommandText = "select * from " + tablename + " order by bdcguid_projekt";
                        command.CommandType = CommandType.Text;

                        context.Database.OpenConnection();

                        try
                        {

                            using (var result = command.ExecuteReader())
                            {
                                var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();
                                Guid loadedProjectIndex = new Guid();

                                Project p = new Project();
                                while (result.Read())
                                {
                                    ReferenceGeometry rg = null;
                                    Record r = null;

                                    bool isChanged = false;

                                    int projectindex = names.IndexOf("bdcguid_projekt");
                                    int geometryIndex = names.IndexOf("bdcguid_geometrie");
                                    int recordIndex = names.IndexOf("bdcguid_beobachtung");

                                    if (result[projectindex] != null)
                                    {
                                        Guid newGuid = Guid.Parse(result.GetString(projectindex));
                                        if (newGuid != loadedProjectIndex)
                                        {
                                            loadedProjectIndex = newGuid;
                                            // load the existing project
                                            p = await db.Projects
                                                .Include(m => m.ProjectGroups).ThenInclude(gp => gp.Geometries).ThenInclude(g => g.Records).ThenInclude(r => r.Form).ThenInclude(f => f.FormFormFields).ThenInclude(ff => ff.FormField)
                                                .Include(m => m.ProjectGroups).ThenInclude(gp => gp.Geometries).ThenInclude(g => g.Records).ThenInclude(r => r.TextData)
                                                .Include(m => m.ProjectGroups).ThenInclude(gp => gp.Geometries).ThenInclude(g => g.Records).ThenInclude(r => r.BooleanData)
                                                .Include(m => m.ProjectGroups).ThenInclude(gp => gp.Geometries).ThenInclude(g => g.Records).ThenInclude(r => r.NumericData)
                                                .Include(m => m.ProjectGroups).ThenInclude(g => g.Records).ThenInclude(r => r.Form).ThenInclude(f => f.FormFormFields).ThenInclude(ff => ff.FormField)
                                                .Include(m => m.ProjectGroups).ThenInclude(g => g.Records).ThenInclude(r => r.TextData)
                                                .Include(m => m.ProjectGroups).ThenInclude(g => g.Records).ThenInclude(r => r.BooleanData)
                                                .Include(m => m.ProjectGroups).ThenInclude(g => g.Records).ThenInclude(r => r.NumericData)
                                                .Include(m => m.ProjectGroups).ThenInclude(m => m.Group)
                                                .Where(m => m.ProjectId == loadedProjectIndex).FirstOrDefaultAsync();

                                            // No rights to change the projects
                                            if (!erfassendeProjects.Any(m => m.ProjectId == p.ProjectId))
                                            {
                                                changes += "<li><b>ACHTUNG</b>: Keine Erfasser-Rechte um das Projekt zu ändern " + p.ProjectId + "! Die Daten dieses Projektes werden nicht importiert</li>";
                                                p = null;
                                            }



                                        }
                                        if (p.ProjectId != null)
                                        {
                                            ProjectGroup pg = db.ProjectsGroups.Where(m => m.ProjectId == p.ProjectId && m.ReadOnly == false && m.Group.GroupUsers.Any(u => u.UserId == user.UserId)).FirstOrDefault();
                                            if ((result[geometryIndex] != null) && (!result.IsDBNull(geometryIndex)))
                                            {

                                                foreach (ProjectGroup checkpg in p.ProjectGroups)
                                                {
                                                    rg = checkpg.Geometries.Where(m => m.GeometryId == Guid.Parse(result.GetString(geometryIndex))).FirstOrDefault();
                                                    if (rg != null) break;
                                                }

                                                //rg = p.ProjectGroups.Select(m => m.Geometries.Where(g => g.GeometryId == Guid.Parse(result.GetString(geometryIndex))).First()).FirstOrDefault();

                                                if (rg == null)
                                                {
                                                    rg = new ReferenceGeometry() { GeometryId = Guid.Parse(result.GetString(geometryIndex)), ProjectGroup = pg, StatusId = StatusEnum.unchanged };
                                                    db.Entry(rg).State = EntityState.Added;
                                                    db.Geometries.Add(rg);
                                                    await db.SaveChangesAsync();
                                                    changes += "<li>Neue Geometrie mit Guid " + result.GetString(geometryIndex) + " erstellt</li>";
                                                    ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "New Geometry by Importing", User = user };
                                                    ChangeLogGeometry clr = new ChangeLogGeometry() { Geometry = rg, ChangeLog = cl };
                                                    db.ChangeLogsGeometries.Add(clr);

                                                }

                                                // do we have a new geometry name?
                                                if ((names.Contains("geometriename")) && (result["geometriename"] != null) && (!result.IsDBNull("geometriename")))
                                                {
                                                    if (rg.GeometryName != result.GetString("geometriename"))
                                                    {
                                                        changes += "<li>Neuer Geometriename " + result.GetString("geometriename") + " (ehemals " + rg.GeometryName + ")";
                                                        rg.GeometryName = result.GetString("geometriename");


                                                        db.Entry(rg).State = EntityState.Modified;
                                                        ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Renamed Geometry by Importing", User = user };
                                                        ChangeLogGeometry clr = new ChangeLogGeometry() { Geometry = rg, ChangeLog = cl };
                                                        db.ChangeLogsGeometries.Add(clr);
                                                    }
                                                }

                                                // did we change the geometry?
                                                if ((names.Contains("wkt_geometrie")) && (result["wkt_geometrie"] != null) && (!result.IsDBNull("wkt_geometrie")))
                                                {
                                                    string geom = result.GetString("wkt_geometrie");
                                                    WKTReader reader = new WKTReader();
                                                    Geometry geometrie = reader.Read(geom);
                                                    geometrie.SRID = 4326;

                                                    if ((geometriecolumn == "polygon") && ((rg.Polygon == null) || (rg.Polygon != geometrie)))
                                                    {
                                                        changes += "<li>Geometrie " + rg.GeometryName + " angepasst</li>";
                                                        rg.Polygon = (Polygon)geometrie;
                                                        db.Entry(rg).State = EntityState.Modified;
                                                        ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Geometry changed by Importing", User = user };
                                                        ChangeLogGeometry clr = new ChangeLogGeometry() { Geometry = rg, ChangeLog = cl };
                                                        db.ChangeLogsGeometries.Add(clr);
                                                    }
                                                    else if ((geometriecolumn == "line") && ((rg.Line == null) || (rg.Line != geometrie)))
                                                    {
                                                        changes += "<li>Geometrie" + rg.GeometryName + "  angepasst</li>";
                                                        rg.Line = (LineString)geometrie;
                                                        db.Entry(rg).State = EntityState.Modified;
                                                        ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Geometry changed by Importing", User = user };
                                                        ChangeLogGeometry clr = new ChangeLogGeometry() { Geometry = rg, ChangeLog = cl };
                                                        db.ChangeLogsGeometries.Add(clr);
                                                    }
                                                    else if ((geometriecolumn == "point") && ((rg.Point == null) || (rg.Point != geometrie)))
                                                    {
                                                        changes += "<li>Geometrie " + rg.GeometryName + " angepasst</li>";
                                                        rg.Point = (Point)geometrie;
                                                        db.Entry(rg).State = EntityState.Modified;
                                                        ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Geometry changed by Importing", User = user };
                                                        ChangeLogGeometry clr = new ChangeLogGeometry() { Geometry = rg, ChangeLog = cl };
                                                        db.ChangeLogsGeometries.Add(clr);
                                                    }
                                                }

                                                // was it deleted? Then make it new again
                                                if (rg.StatusId==StatusEnum.deleted)
                                                {
                                                    changes += "<li>Geometrie " + rg.GeometryName + " wieder aktiviert (undelete)</li>";
                                                    rg.StatusId = StatusEnum.unchanged;
                                                    db.Entry(rg).State = EntityState.Modified;
                                                    ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Geometry undeleted by Importing", User = user };
                                                    ChangeLogGeometry clr = new ChangeLogGeometry() { Geometry = rg, ChangeLog = cl };
                                                    db.ChangeLogsGeometries.Add(clr);
                                                }
                                            }
                                            if ((result[recordIndex] != null) && (!result.IsDBNull(recordIndex)))
                                            {
                                                string recordIDString = result.GetString(recordIndex);
                                                Guid parsedGuid2;
                                                if (Guid.TryParse(result.GetString(recordIndex), out parsedGuid2))
                                                {
                                                    if ((rg != null) && (rg.Records.Any(m => m.RecordId == Guid.Parse(result.GetString(recordIndex)))))
                                                    {
                                                        r = rg.Records.Where(m => m.RecordId == Guid.Parse(result.GetString(recordIndex))).FirstOrDefault();
                                                    }
                                                    else
                                                    {
                                                        foreach (ProjectGroup checkpg in p.ProjectGroups)
                                                        {
                                                            r = checkpg.Records.Where(m => m.RecordId == Guid.Parse(result.GetString(recordIndex))).FirstOrDefault();
                                                            if (r != null) break;
                                                        }

                                                    }
                                                }
                                            }
                                            Guid parsedGuid;
                                            if ((!result.IsDBNull(recordIndex)) && ((Guid.TryParse(result.GetString(recordIndex), out parsedGuid))))
                                            {
                                                // create a new Record)
                                                if ((r == null) && (result[recordIndex] != null) && (!result.IsDBNull(recordIndex)))
                                                {

                                                    //ProjectGroup pg = p.ProjectGroups.Where(m => m.ReadOnly == false && m.Group.GroupUsers.Any(u => u.UserId == user.UserId)).FirstOrDefault();
                                                    if (rg != null)
                                                    {
                                                        r = new Record() { Geometry = rg, ProjectGroup = pg, StatusId = StatusEnum.unchanged, RecordId = Guid.Parse(result.GetString(recordIndex)) };

                                                    }
                                                    else
                                                    {
                                                        r = new Record() { ProjectGroup = pg, StatusId = StatusEnum.unchanged, RecordId = Guid.Parse(result.GetString(recordIndex)) };
                                                    }

                                                    // search first import field == get the form. But first ignore the allgemein columns, because they could be from an other form
                                                    bool formfound = false;
                                                    foreach (string columnname in names.Where(m => m.Contains("_") && !m.Contains("Allgemein")).ToList())
                                                    {
                                                         if ((!result.IsDBNull(columnname)) && (!formfound))
                                                        {
                                                            // ignore default boolean values... We don't know if this is filled out or default...
                                                            if (result.GetFieldType(columnname) != typeof(Boolean))
                                                            {

                                                                string[] getID = columnname.Split("_");
                                                                try
                                                                {
                                                                    int fieldId;
                                                                    Int32.TryParse(getID[0], out fieldId);
                                                                    if (fieldId == 0) Int32.TryParse(getID[1], out fieldId);

                                                                    FormField ff = await db.FormFields.Include(m => m.FormFieldForms).ThenInclude(m => m.Form).Where(m => m.FormFieldId == fieldId).FirstOrDefaultAsync();
                                                                    if (ff != null)
                                                                    {
                                                                        r.Form = ff.FormFieldForms.First().Form;
                                                                        formfound = true;
                                                                        break;
                                                                    }
                                                                }
                                                                catch (Exception e)
                                                                {

                                                                }
                                                            }

                                                        }
                                                    }
                                                    //if nothing found try it with the allgemein
                                                    if (r.Form == null)
                                                    {
                                                        foreach (string columnname in names.Where(m => m.Contains("_") && m.Contains("Allgemein")).ToList())
                                                        {
                                                            if (!result.IsDBNull(columnname))
                                                            {
                                                                string[] getID = columnname.Split("_");
                                                                try
                                                                {
                                                                    int fieldId;
                                                                    Int32.TryParse(getID[0], out fieldId);
                                                                    if (fieldId == 0) Int32.TryParse(getID[1], out fieldId);
                                                                    FormField ff = await db.FormFields.Include(m => m.FormFieldForms).ThenInclude(m => m.Form).Where(m => m.FormFieldId == fieldId).FirstOrDefaultAsync();
                                                                    if (ff != null)
                                                                    {
                                                                        r.Form = ff.FormFieldForms.First().Form;
                                                                    }
                                                                }
                                                                catch (Exception e)
                                                                {

                                                                }

                                                            }
                                                        }
                                                    }
                                                    db.Entry(r).State = EntityState.Added;
                                                    db.Records.Add(r);

                                                    changes += "<li>Beobachtung mit Guid " + r.RecordId + " erstellt</li>";
                                                    ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Created Record by Importing", User = user };
                                                    ChangeLogRecord clr = new ChangeLogRecord() { Record = r, ChangeLog = cl };
                                                    db.ChangeLogsRecords.Add(clr);


                                                }

                                                // there is already a record, update the content
                                                if ((r != null) && (r.Form != null))
                                                {
                                                    foreach (FormFormField fff in r.Form.FormFormFields)
                                                    {
                                                        string importColumnName = null;
                                                        foreach (string columnname in names)
                                                        {
                                                            if (columnname.StartsWith("f_" + fff.FormFieldId.ToString())) importColumnName = columnname;

                                                        }
                                                        if ((importColumnName != null) && (!result.IsDBNull(importColumnName)))
                                                        {
                                                            if ((fff.FormField.FieldTypeId == FieldTypeEnum.Text) || (fff.FormField.FieldTypeId == FieldTypeEnum.DateTime))
                                                            {
                                                                TextData t = r.TextData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                                                                if (t != null)
                                                                {
                                                                    if (t.Value != result.GetValue(importColumnName).ToString())
                                                                    {
                                                                        t.Value = result.GetValue(importColumnName).ToString();
                                                                        db.Entry(t).State = EntityState.Modified;
                                                                        isChanged = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    t = new TextData() { FormField = fff.FormField, Record = r, Value = result.GetValue(importColumnName).ToString(), Id = Guid.NewGuid() };
                                                                    db.TextData.Add(t);
                                                                    db.Entry(t).State = EntityState.Added;
                                                                    isChanged = true;
                                                                }
                                                            }

                                                            if (fff.FormField.FieldTypeId == FieldTypeEnum.Choice)
                                                            {
                                                                TextData t = r.TextData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                                                                if (t != null)
                                                                {
                                                                    if (t.Value != result.GetValue(importColumnName).ToString())
                                                                    {
                                                                        t.Value = result.GetValue(importColumnName).ToString();
                                                                        FieldChoice fc = await db.FieldChoices.Where(m => m.Text == t.Value && m.FormField == fff.FormField).FirstOrDefaultAsync();
                                                                        //if (fff.FormField.FieldChoices.Any(m => m.Text == t.Value)) t.FieldChoice = fff.FormField.FieldChoices.Where(m => m.Text == t.Value).First();
                                                                        if (fc != null) t.FieldChoice = fc;
                                                                        db.Entry(t).State = EntityState.Modified;
                                                                        isChanged = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    t = new TextData() { FormField = fff.FormField, Record = r, Value = result.GetValue(importColumnName).ToString(), Id = Guid.NewGuid() };
                                                                    db.TextData.Add(t);
                                                                    db.Entry(t).State = EntityState.Added;
                                                                    isChanged = true;
                                                                }
                                                            }

                                                            if (fff.FormField.FieldTypeId == FieldTypeEnum.Boolean)
                                                            {
                                                                BooleanData b = r.BooleanData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                                                                if (b != null)
                                                                {
                                                                    bool convertedBoolean = false;
                                                                    try
                                                                    {
                                                                        string buuli = result.GetString(importColumnName);
                                                                        if ((buuli.ToLower() == "true") || (buuli == "1") || (buuli.ToLower() == "wahr")) convertedBoolean = true;
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        convertedBoolean = result.GetBoolean(importColumnName);
                                                                    }

                                                                    if (b.Value != convertedBoolean)
                                                                    {
                                                                        b.Value = convertedBoolean;
                                                                        db.Entry(b).State = EntityState.Modified;
                                                                        isChanged = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    bool convertedBoolean = false;
                                                                    try
                                                                    {
                                                                        string buuli = result.GetString(importColumnName);
                                                                        if ((buuli == "true") || (buuli == "1")) convertedBoolean = true;
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        convertedBoolean = result.GetBoolean(importColumnName);
                                                                    }
                                                                    b = new BooleanData() { FormField = fff.FormField, Record = r, Value = convertedBoolean, Id = Guid.NewGuid() };
                                                                    db.BooleanData.Add(b);
                                                                    db.Entry(b).State = EntityState.Added;
                                                                    isChanged = true;
                                                                }
                                                            }

                                                            if (fff.FormField.FieldTypeId == FieldTypeEnum.Number)
                                                            {
                                                                NumericData n = r.NumericData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                                                                if (n != null)
                                                                {
                                                                    if (n.Value != result.GetDouble(importColumnName))
                                                                    {
                                                                        n.Value = result.GetDouble(importColumnName);
                                                                        db.Entry(n).State = EntityState.Modified;
                                                                        isChanged = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    n = new NumericData() { FormField = fff.FormField, Record = r, Value = result.GetDouble(importColumnName), Id = Guid.NewGuid() };
                                                                    db.NumericData.Add(n);
                                                                    db.Entry(n).State = EntityState.Added;
                                                                    isChanged = true;
                                                                }
                                                            }
                                                        }
                                                    }

                                                    if (isChanged)
                                                    {
                                                        changes += "<li>Beobachtung (" + r.Form.Title + ") mit Guid " + r.RecordId + " angepasst</li>";
                                                        ChangeLog cl = new ChangeLog() { ChangeDate = DateTime.Now, Log = "Modified Record by Importing", User = user };
                                                        ChangeLogRecord clr = new ChangeLogRecord() { Record = r, ChangeLog = cl };
                                                        db.ChangeLogsRecords.Add(clr);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    await db.SaveChangesAsync();
                                }
                            }

                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            return changes;
        }

        public IActionResult Import()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, this.db);
            List<Project> erfassendeProjects = new List<Project>();

            if (User.IsInRole("DM"))
            {
                erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(this.db, user, RoleEnum.DM); ;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(this.db, user, RoleEnum.EF);

            string prefix = "import_" + RandomString(4);

            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            string filePath = dataDir + "\\Import\\" + file.FileName;
            if (file.Length > 0)
            {
                // full path to file in temp location

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            // TODO: Move GDAL Dir to config



            /* local */
            /*psi.FileName = @"C:\gdal\bin\gdal\apps\ogr2ogr.exe";
            psi.WorkingDirectory = @"C:\gdal\bin\gdal\apps";
            psi.EnvironmentVariables["GDAL_DATA"] = @"C:\gdal\bin\gdal-data";
            psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\gdal\bin\gdal\plugins";
            psi.EnvironmentVariables["PATH"] = "C:\\gdal\\bin;" + psi.EnvironmentVariables["PATH"];*/


            string db = Configuration["Environment:DB"];
            string host = Configuration["Environment:DBHost"];
            string dbuser = Configuration["Environment:DBUser"];
            string dbpassword = Configuration["Environment:DBPassword"];

            string pgstring = " PG:\"dbname = '" + db + "' user = '" + dbuser + "' password = '" + dbpassword + "' host = '" + host + "'\"";

            psi.FileName = @"C:\Program Files\GDAL\ogr2ogr.exe";
            psi.WorkingDirectory = @"C:\Program Files\GDAL";
            psi.EnvironmentVariables["GDAL_DATA"] = @"C:\Program Files\GDAL\gdal-data";
            psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\Program Files\GDAL\gdal-plugins";
            psi.EnvironmentVariables["PATH"] = "C:\\Program Files\\GDAL;" + psi.EnvironmentVariables["PATH"];

            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;

            string format = System.IO.Path.GetExtension(filePath);

            string changes = "";

            if (format == ".gpkg")
            {
                // TODO: Move db info out of here
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_points\" \"" + filePath + "\" -sql \"select* from points\"";
                var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.ToString() });
                string returnMessage = await ImportTable(prefix + "_points", "point", erfassendeProjects, user);
                if (!returnMessage.StartsWith("OK")) return Json(new ExportProcess() { Error = returnMessage });
                changes += returnMessage;
                await this.db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS " + prefix + "_points;");

                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_lines\" \"" + filePath + "\" -sql \"select* from lines\"";
                OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.ToString() });
                returnMessage = await ImportTable(prefix + "_lines", "line", erfassendeProjects, user);
                if (!returnMessage.StartsWith("OK")) return Json(new ExportProcess() { Error = returnMessage });
                changes += returnMessage;
                await this.db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS " + prefix + "_lines;");

                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_polygones\" \"" + filePath + "\" -sql \"select* from polygones\"";
                OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.ToString() });
                returnMessage = await ImportTable(prefix + "_polygones", "polygon", erfassendeProjects, user);
                if (!returnMessage.StartsWith("OK")) return Json(new ExportProcess() { Error = returnMessage });
                changes += returnMessage;
                await this.db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS " + prefix + "_polygones;");

                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_nogeometry\" \"" + filePath + "\" -sql \"select* from records_without_geometries\"";
                OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.ToString() });
                returnMessage = await ImportTable(prefix + "_nogeometry", "", erfassendeProjects, user);
                if (!returnMessage.StartsWith("OK")) return Json(new ExportProcess() { Error = returnMessage });
                changes += returnMessage;
                await this.db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS " + prefix + "_nogeometry;");
            }
            else if ((format == ".xlsx") || (format == ".csv"))
            {
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_nogeometry\" \"" + filePath + "\" --config OGR_XLSX_HEADERS FORCE";
                var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.FirstOrDefault().ToString() + " ("+ psi.Arguments+")" });
                string returnMessage = await ImportTable(prefix + "_nogeometry", "", erfassendeProjects, user);
                if (!returnMessage.StartsWith("OK")) return Json(new ExportProcess() { Error = returnMessage });
                changes += returnMessage;
                await this.db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS " + prefix + "_nogeometry;");
            }

            changes = "Folgende Änderungen / Neuerungen wurden erfolgreich übernommen: <ul>" + changes + "</ul>";

            return View("ImportOK", changes.Replace("OK",""));
            
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Export(string format, string efonly, string projects)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, this.db);
            List<Project> erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(this.db, user, RoleEnum.EF);

            // First create the project specific views

            List<Form> forms = new List<Form>();

            List<string> exportprojects = new List<string>();

            foreach (string p in projects.Split(","))
            {
                Project project = await this.db.Projects.Include(m => m.ProjectForms).ThenInclude(m => m.Form).ThenInclude(m => m.FormFormFields).ThenInclude(m => m.FormField).Where(m => m.ProjectId == new Guid(p)).FirstOrDefaultAsync();
                if ((efonly!="on") || ((efonly == "on") && erfassendeProjects.Contains(project)))
                {
                    exportprojects.Add(p);
                    foreach (ProjectForm pf in project.ProjectForms)
                    {
                        forms.Add(pf.Form);
                    }
                }
            }

            string prefix = "exp_" + RandomString(4);

            int maxLength = 64;
            if (format == "gpkg") maxLength = 31;
            await FormsController.CreateViews(this.db, forms.Distinct().ToList(), false, prefix, true, maxLength);


            string exportf = Path.GetRandomFileName();
            string[] fname = exportf.Split(".");

            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            if (!Directory.Exists(dataDir + "//Export")) Directory.CreateDirectory(dataDir + "//Export");
            string exportfilename = dataDir + "//Export//" + fname[0] + "." + format;


            ProcessStartInfo psi = new ProcessStartInfo();
            // TODO: Move GDAL Dir to config



            /* local */
            /*psi.FileName = @"C:\gdal\bin\gdal\apps\ogr2ogr.exe";
            psi.WorkingDirectory = @"C:\gdal\bin\gdal\apps";
            psi.EnvironmentVariables["GDAL_DATA"] = @"C:\gdal\bin\gdal-data";
            psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\gdal\bin\gdal\plugins";
            psi.EnvironmentVariables["PATH"] = "C:\\gdal\\bin;" + psi.EnvironmentVariables["PATH"];*/

            string db = Configuration["Environment:DB"];
            string host = Configuration["Environment:DBHost"];
            string dbuser = Configuration["Environment:DBUSer"];
            string dbpassword = Configuration["Environment:DBPassword"];


            psi.FileName = @"C:\Program Files\GDAL\ogr2ogr.exe";
            psi.WorkingDirectory = @"C:\Program Files\GDAL";
            psi.EnvironmentVariables["GDAL_DATA"] = @"C:\Program Files\GDAL\gdal-data";
            psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\Program Files\GDAL\gdal-plugins";
            psi.EnvironmentVariables["PATH"] = "C:\\Program Files\\GDAL;" + psi.EnvironmentVariables["PATH"];

            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;

            string pgstring = " PG:\"dbname = '" + db + "' user = '" + dbuser + "' password = '" + dbpassword + "' host = '" + host + "'\"";

            if (format == "gpkg")
            {
                // TODO: Move db info out of here
                psi.Arguments = "-f GPKG " + exportfilename + pgstring + " \"" + prefix + "_point_view\" -where \"\\\"bdcguid_projekt\\\" in ('" + String.Join("', '", exportprojects.ToArray()) + "')\" -nln \"points\"";

                var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });

                psi.Arguments = psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"" + prefix + "_line_view\" -where \"\\\"bdcguid_projekt\\\" in ('" + String.Join("', '", exportprojects.ToArray()) + "')\" -nln \"lines\"";
                OgrOgrResult = await ProcessEx.RunAsync(psi);
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });
                psi.Arguments = psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"" + prefix + "_polygon_view\" -where \"\\\"bdcguid_projekt\\\" in ('" + String.Join("', '", exportprojects.ToArray()) + "')\" -nln \"polygones\"";
                OgrOgrResult = await ProcessEx.RunAsync(psi);
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });
                psi.Arguments = psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"" + prefix + "_records_without_geometries\" -where \"\\\"bdcguid_projekt\\\" in ('" + String.Join("', '", exportprojects.ToArray()) + "') AND \\\"bdcguid_geometrie\\\" is null \" -nln \"records_without_geometries\"";
                OgrOgrResult = await ProcessEx.RunAsync(psi);
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });
            }
            else if (format == "csv")
            {
                psi.Arguments = "-f CSV " + exportfilename + pgstring + " \"" + prefix + "_records_without_geometries\" -where \"\\\"bdcguid_projekt\\\" in ('" + String.Join("', '", exportprojects.ToArray()) + "')\"";

                var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });

            }
            else if (format == "xlsx")
            {
                psi.Arguments = "-f CSV " + dataDir + "//Export//" + fname[0] + ".csv " + pgstring + " \"" + prefix + "_records_without_geometries\" -where \"\\\"bdcguid_projekt\\\" in ('" + String.Join("', '", exportprojects.ToArray()) + "')\"";

                var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });

                psi.Arguments = "-f XLSX " + exportfilename + " " + dataDir + "//Export//" + fname[0] + ".csv";

                OgrOgrResult = ProcessEx.RunAsync(psi).Result;
                if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });

            }
            //Stream stream = System.IO.File.OpenRead(exportfilename);

            await this.db.Database.ExecuteSqlRawAsync("DROP VIEW IF EXISTS " + prefix + "_point_view; DROP VIEW IF EXISTS " + prefix + "_line_view; DROP VIEW IF EXISTS " + prefix + "_polygon_view;");
            await this.db.Database.ExecuteSqlRawAsync("DROP VIEW IF EXISTS " + prefix + "_records_without_geometries;");

            //if (stream == null)
            //    return NotFound(); // returns a NotFoundResult with Status404NotFound response.

            

            return Json(new ExportProcess() { Filename = fname[0] + "." + format });

        }

        public IActionResult Download(string filename)
        {
            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            if (!Directory.Exists(dataDir + "//Export")) Directory.CreateDirectory(dataDir + "//Export");
            string exportfilename = dataDir + "//Export//" + filename;
            Stream stream = System.IO.File.OpenRead(exportfilename);
            if (stream == null)
                return NotFound(); // returns a NotFoundResult with Status404NotFound response.

            string[] fname = filename.Split(".");
            return File(stream, "application/octet-stream", "bdc_export." + fname[1]);
        }

        public async Task<IActionResult> GetThirdPartyTools()
        {
            List<ThirdPartyTool> thirdPartyTool = await db.ThirdPartyTools.ToListAsync();
            string json = JsonConvert.SerializeObject(thirdPartyTool);
            return Content(json, "application/json");
        }


        public async Task<IActionResult> GetUsersByRole(Guid? id, string role, string search)
        {
            Project project = new Project();

            // As only EF I am not able to search the user db
            if ((!User.IsInRole("DM") && (!User.IsInRole("PL")) && (!User.IsInRole("PK")))) return RedirectToAction("NotAllowed", "Home");

            if (id == null)
            {
                project = await db.Projects
               .Include(m => m.ProjectConfigurator)
               .FirstOrDefaultAsync(m => m.ProjectId == id);
            }
            List<string> usersList;
            if ((role == null) || (role == "")) usersList = UserHelper.GetAllUsers(Configuration["JWT:Admin-Token-Url"], Configuration["JWT:Admin-Url"], Configuration["JWT:Admin-Key"], Configuration["JWT:Admin-User"], Configuration["JWT:Admin-Password"]);
            else usersList = UserHelper.GetAllUsersByRole(role, Configuration["JWT:Admin-Url"], Configuration["JWT:ClientId"], Configuration["JWT:Admin-Token-Url"], Configuration["JWT:Admin-Key"], Configuration["JWT:Admin-User"], Configuration["JWT:Admin-Password"]);
            List<SelectedPorjectUserPoco> returnList = new List<SelectedPorjectUserPoco>();
            foreach (string userid in usersList)
            {
                DB.Models.Domain.User u = await db.Users.FindAsync(userid);
                if (u != null)
                {
                    SelectedPorjectUserPoco sup = new SelectedPorjectUserPoco() { myUser = u, myProject = project };
                    if (search == null) returnList.Add(sup);
                    else if ((u != null) && (sup.Name.ToLower().Contains(search.ToLower()))) returnList.Add(sup);
                }
            }

            string json = JsonConvert.SerializeObject(returnList);
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> CreateThirdPartyTools(string Name)
        {
            try
            {
                ThirdPartyTool tpt = new ThirdPartyTool() { Name = Name };
                db.Entry(tpt).State = EntityState.Added;
                db.ThirdPartyTools.Add(tpt);
                await db.SaveChangesAsync();

                List<ThirdPartyTool> allTPT = new List<ThirdPartyTool>();
                allTPT.Add(tpt);
                return Json(new { items = allTPT });
            }
            catch (Exception e)
            {
                
            }

            return Json(null);
        }


        [HttpPost]
        public async Task<IActionResult> EditPK([FromBody] Users data)
        {
            var project = await db.Projects
                .Include(m => m.ProjectConfigurator)
                .FirstOrDefaultAsync(m => m.ProjectId == data.guid);
            if (project == null)
            {
                return NotFound();
            }

            /*foreach (GroupUser guOld in project.GroupUsers)
            {
                db.GroupsUsers.Remove(guOld);
            }


            foreach (UserIds userid in data.items)
            {
                DB.Models.Domain.User u = await db.Users.FindAsync(userid.value);
                if (u != null)
                {
                    GroupUser gu = new GroupUser() { Group = mgroup, User = u };
                    db.GroupsUsers.Add(gu);
                }

            }*/

            if (data.items.Count() > 0)
            {
                DB.Models.Domain.User u = db.Users.Find(data.items[0].value);
                project.ProjectConfigurator = u;
            }


            await db.SaveChangesAsync();


            return Content("OK", "application/json");
        }


        /// <summary>
        /// Get the available Layers and the selected layers for this project
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> LayersList(Guid id)
        {
            Project p = await db.Projects.Include(m => m.ProjectLayers).Where(m => m.ProjectId == id).FirstOrDefaultAsync();

            User me = UserHelper.GetCurrentUser(User, db);

            List<Layer> allLayers = await db.Layers.Where(m => m.Public == true || m.LayerUsers.Any(u => u.UserId == me.UserId)).ToListAsync();

            string returnlist = "[ ";
            foreach (Layer we in allLayers)
            {
                if (p.ProjectLayers.Where(m => m.LayerId == we.LayerId).Count() > 0)
                    returnlist += "{\"item\":\"" + we.Title + "\",\"value\":\"" + we.LayerId + "\",\"selected\":true},";
                else returnlist += "{\"item\":\"" + we.Title + "\",\"value\":\"" + we.LayerId + "\"},";
            }


            returnlist = returnlist.Substring(0, returnlist.Length - 1) + "]";
            return Content(returnlist, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> EditProjectLayers([FromBody] LayersList data)
        {
            Project p = await db.Projects.Include(m => m.ProjectLayers).Where(m => m.ProjectId == data.project).FirstOrDefaultAsync();
            foreach (ProjectLayer plOld in p.ProjectLayers)
            {
                db.ProjectsLayers.Remove(plOld);
            }

            foreach (Layers l in data.items)
            {
                Layer layer = await db.Layers.Where(m => m.LayerId == Int32.Parse(l.value)).FirstAsync();
                if (layer != null)
                {
                    ProjectLayer pl = new ProjectLayer() { Layer = layer, Project = p };
                    db.ProjectsLayers.Add(pl);
                }
            }

            await db.SaveChangesAsync();


            return Content("OK", "application/json");
        }

        public async Task<IActionResult> GroupsList(Guid id, bool onlyreadonly)
        {
            Project p = await db.Projects
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(k => k.Records)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Records)
                .Where(m => m.ProjectId == id).FirstOrDefaultAsync();

            User me = UserHelper.GetCurrentUser(User, db);

            List<Group> allGroups = await db.Groups
                .Include(m => m.GroupStatus)
                .Include(m => m.GroupChangeLogs).ThenInclude(u => u.ChangeLog).ThenInclude(uu => uu.User)
                //.Where(m => m.CreatorId == me.UserId || m.GroupChangeLogs.First().ChangeLog.User.UserId==me.UserId)
                .ToListAsync();

            string returnlist = "[ ";
            foreach (Group gr in allGroups.Distinct())
            {
                if (p.ProjectGroups.Any(m => m.GroupId == gr.GroupId && m.ReadOnly == onlyreadonly))
                {
                    ProjectGroup pg = p.ProjectGroups.Where(m => m.GroupId == gr.GroupId).Where(m => m.ReadOnly == onlyreadonly).First();
                    int? anzGeometrien = pg.Geometries?.Where(u => u.StatusId != StatusEnum.deleted).Count();
                    int? anzRecords = pg.Records?.Where(m => m.StatusId != StatusEnum.deleted && m.Geometry == null).Count();
                    int? anz2Records = pg.Geometries?.Where(m => m.StatusId != StatusEnum.deleted).SelectMany(m => m.Records.Where(tt => tt.StatusId != StatusEnum.deleted)).Where(z => z.StatusId != StatusEnum.deleted).Count();
                    int? totalRecords = anzRecords + anz2Records;


                    // do we have any records or geometries --> not able to remove it...
                    if ((anzGeometrien > 0) || (totalRecords > 0))
                    {
                        returnlist += "{\"item\":\"" + gr.GroupName + " (" + gr.GroupStatus.Description + ")" + "\",\"value\":\"" + gr.GroupId + "\",\"selected\":true, \"disabled\" : true},";
                    }
                    else returnlist += "{\"item\":\"" + gr.GroupName + " (" + gr.GroupStatus.Description + ")" + "\",\"value\":\"" + gr.GroupId + "\",\"selected\":true},";
                }
                else returnlist += "{\"item\":\"" + gr.GroupName + " (" + gr.GroupStatus.Description + ")" + "\",\"value\":\"" + gr.GroupId + "\"},";
            }

            returnlist = returnlist.Substring(0, returnlist.Length - 1) + "]";
            return Content(returnlist, "application/json");
        }


        [HttpPost]
        public async Task<IActionResult> EditProjectGroup([FromBody] GroupsList data)
        {
            Project p = await db.Projects
                .Include(m => m.ProjectGroups).ThenInclude(p => p.Records)
                .Include(m => m.ProjectGroups).ThenInclude(p => p.Geometries).Where(m => m.ProjectId == data.project).FirstOrDefaultAsync();

            List<ProjectGroup> oldPgGroups = p.ProjectGroups.Where(m => m.ReadOnly == data.onlyreadonly).ToList();
            List<Group> newPgGroups = new List<Group>();


            foreach (Groups g in data.items)
            {
                Group gr = await db.Groups.Where(m => m.GroupId == new Guid(g.value)).FirstAsync();
                if (gr != null)
                {
                    if (oldPgGroups.Any(m => m.GroupId == gr.GroupId))
                    {
                        newPgGroups.Add(gr);
                    }
                    else
                    {
                        ProjectGroup pg = new ProjectGroup() { Group = gr, Project = p, ReadOnly = (bool)data.onlyreadonly };
                        db.ProjectsGroups.Add(pg);
                    }
                }
            }

            // delete all not used projectgroups and only if there is no records or geometrie
            List<ProjectGroup> toRemovePGs = oldPgGroups?.Where(m => newPgGroups.All(m2 => m2.GroupId != m.GroupId)).ToList();
            foreach (ProjectGroup pg in toRemovePGs)
            {
                if ((pg.Geometries.Where(m => m.StatusId != StatusEnum.deleted).Count() == 0) && (pg.Records.Where(m => m.StatusId != StatusEnum.deleted).Count() == 0))
                {
                    p.ProjectGroups.Remove(pg);
                    db.ProjectsGroups.Remove(pg);
                }
            }

            await db.SaveChangesAsync();


            return Content("OK", "application/json");
        }

        public async Task<IActionResult> ChangeProjectState(Guid id, Guid groupId, GroupStatusEnum newState)
        {
            Project p = await db.Projects
                .Include(m => m.ProjectConfigurator)
                .Include(m => m.ProjectManager)
                .Include(m => m.ProjectGroups).ThenInclude(m => m.Group).ThenInclude(m => m.GroupUsers).Where(m => m.ProjectId == id).FirstOrDefaultAsync();
            if (p == null)
            {
                return NotFound();
            }

            ProjectGroup pg = p.ProjectGroups.Where(m => m.GroupId == groupId).FirstOrDefault();
            if (pg == null)
            {
                return NotFound();
            }

            User me = UserHelper.GetCurrentUser(User, db);

            // change to Gruppendaten_erfasst only if user is in group
            if ((newState == GroupStatusEnum.Gruppendaten_erfasst) && (pg.Group.GroupUsers.Any(m => m.UserId == me.UserId)))
            {
                pg.GroupStatusId = newState;
            }
            if ((User.IsInRole("DM")) || (p.ProjectManager.UserId == me.UserId) || (p.ProjectConfigurator.UserId == me.UserId))
            {
                pg.GroupStatusId = newState;
            }

            // check if every group is gültig --> close the project
            bool allgueltig = false;
            bool hastNotGueltig = false;
            foreach (ProjectGroup pg_tocheck in p.ProjectGroups.Where(m => m.ReadOnly == false))
            {
                if (pg_tocheck.GroupStatusId == GroupStatusEnum.Gruppendaten_gueltig) allgueltig = true;
                else
                {
                    hastNotGueltig = true;
                }
            }
            if ((!hastNotGueltig) && (allgueltig))
            {
                p.ProjectStatusId = ProjectStatusEnum.Projekt_gueltig;
                db.Entry(p).State = EntityState.Modified;
            }


            db.Entry(pg).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return Content("OK", "application/json");

        }

        public async Task<IActionResult> ProjectLayersList(Guid? id)
        {
            if (id == null)
            {
                string idstring = HttpContext.Session.GetString("Project");
                if (idstring == null) return StatusCode(500);
                id = new Guid(idstring);
            }

            User me = UserHelper.GetCurrentUser(User, db);

            Project p = await db.Projects.Include(m => m.ProjectLayers).ThenInclude(m => m.Layer).ThenInclude(m => m.LayerUsers).Where(m => m.ProjectId == id).FirstOrDefaultAsync();

            List<ProjectLayer> projectLayers = p.ProjectLayers.Where(u => u.Layer.Public || u.Layer.LayerUsers.Any(z => z.UserId == me.UserId)).ToList();

            foreach (ProjectLayer pl in projectLayers)
            {
                UserHasProjectLayer upl = db.UsersHaveProjectLayers.Where(m => m.User == me && m.Layer == pl.Layer && m.Project == p).FirstOrDefault();
                if (upl != null)
                {
                    pl.Visible = upl.Visible;
                    pl.Transparency = upl.Transparency * 100;
                    pl.Order = upl.Order;
                }
                else
                {
                    pl.Visible = false;
                    pl.Transparency = 100;
                    pl.Order = 0;
                }
            }

            List<ProjectLayer> sortedProjectLayers = new List<ProjectLayer>();
            foreach (ProjectLayer pl in projectLayers.OrderBy(m => m.Order)) sortedProjectLayers.Add(pl);

            return View(sortedProjectLayers);
        }

        public async Task<IActionResult> Map(Guid? id, Guid? geometryId)
        {
            if (id == null)
            {
                string idstring = HttpContext.Session.GetString("Project");
                if (idstring == null) return RedirectToAction("Index");
                id = new Guid(idstring);
            }

            User user = Helpers.UserHelper.GetCurrentUser(User, db);


            List<Project> projects = new List<Project>();
            List<Project> erfassendeProjects = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            }
            if (User.IsInRole("PL"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            }

            projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE));
            projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(erfassendeProjects);

            if (projects.Where(m => m.ProjectId == id).Count() == 0) return StatusCode(StatusCodes.Status403Forbidden);

            /*Project newProject = db.Projects
                    .Include(m => m.Status)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Records)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.GroupUsers)
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).First();*/
            Project newProject = db.Projects
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).First();

            await db.Entry(newProject).Collection(m => m.ProjectGroups).LoadAsync();

            foreach (ProjectGroup pg in newProject.ProjectGroups)
            {
                await db.Entry(pg).Collection(m => m.Geometries).LoadAsync();
                await db.Entry(pg).Collection(m => m.Records).LoadAsync();

                await db.Entry(pg).Reference(m => m.Group).Query().Include(zz => zz.GroupStatus).Include(zz => zz.Creator).Include(zz => zz.GroupUsers).ThenInclude(gu => gu.User).LoadAsync();
            }



            if (newProject == null) return StatusCode(500);
            HttpContext.Session.SetString("Project", id.ToString());
            ViewData["ProjectName"] = newProject.ProjectName;
            ViewData["ProjectId"] = newProject.ProjectId;

            if (!erfassendeProjects.Contains(newProject)) ViewData["ReadOnly"] = true;
            else ViewData["ReadOnly"] = false;

            foreach (ProjectGroup gr in newProject.ProjectGroups)
            {
                if (gr.Group.GroupUsers.Any(m => m.UserId == user.UserId)) ViewData["MyGroup"] = gr.GroupId;
            }


            ViewData["workspace"] = Configuration["Environment:DB"];
            if (geometryId != null) ViewData["geometryId"] = geometryId;

            return View(newProject);
        }

        private bool ProjectExists(Guid id)
        {
            return db.Projects.Any(e => e.ProjectId == id);
        }

        /// <summary>
        /// Get all readable Projects for User for Export-Selecting Box
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetProjects()
        {
            DB.Models.Domain.User user = UserHelper.GetCurrentUser(User, db);

            List<Project> projects = new List<Project>();
            List<Project> erfassendeProjects = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            }
            if (User.IsInRole("PL"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            }

            List<Project> nurLesendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE);
            nurLesendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(nurLesendeProjects);
            projects.AddRange(erfassendeProjects);

            List<SelectedProjectPoco> returnList = new List<SelectedProjectPoco>();
            foreach (Project p in projects.Distinct())
            {
                SelectedProjectPoco sup = new SelectedProjectPoco() { myProject = p, myGroup = null, selected = false };
                returnList.Add(sup);
            }

            string json = JsonConvert.SerializeObject(returnList);
            return Content(json, "application/json");
        }
    }

    public class ProjectPocoForIndex
    {
        public Project Project { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsPKOrPLOrDM { get; set; }

        public int RecordCount { get; set; }
        public int GeometryCount { get; set; }

        public string MyGroup { get; set; }

    }

    public class ProjectPocoForExportMap : ClassMap<ProjectPocoForIndex>
    {
        public ProjectPocoForExportMap()
        {
            Map(m => m.Project.ProjectName).Index(0).Name("Name");
            Map(m => m.Project.ProjectNumber).Index(1).Name("Projektnummer");
            Map(m => m.Project.BDCGuid).Index(2).Name("BDC Guid");
            Map(m => m.Project.ID_Extern).Index(3).Name("ID_Extern");
            Map(m => m.Project.Description).Index(4).Name("Beschreibung");
            Map(m => m.Project.OGD).Index(5).Name("OGD");
            Map(m => m.MyGroup).Index(6).Name("Gruppe");
            Map(m => m.GeometryCount).Index(7).Name("Anzahl Geometrien");
            Map(m => m.RecordCount).Index(8).Name("Anzahl Beobachtungen");
            Map(m => m.Project.ProjectConfigurator.FirstName).Index(9).Name("Koordinator Vorname");
            Map(m => m.Project.ProjectConfigurator.Name).Index(10).Name("Koordinator Nachname");
            Map(m => m.Project.ProjectManager.FirstName).Index(11).Name("Projektleiter Vorname");
            Map(m => m.Project.ProjectManager.Name).Index(12).Name("Projektleiter Nachname");
            Map(m => m.Project.ProjectStatus.Description).Index(13).Name("Projektstatus");
        }
    }


    public class LayersList
    {
        public Layers[] items { get; set; }
        public Guid project { get; set; }
    }

    public class Groups
    {
        public string item { get; set; }
        public string value { get; set; }
    }


    public class GroupsList
    {
        public Groups[] items { get; set; }
        public Guid project { get; set; }
        public bool onlyreadonly { get; set; }
    }

    public class Layers
    {
        public string item { get; set; }
        public string value { get; set; }
    }

    public class SelectedPorjectUserPoco
    {
        [Newtonsoft.Json.JsonIgnore]
        public DB.Models.Domain.User myUser { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public Project myProject { get; set; }
        public string Name { get { return myUser.FirstName + " " + myUser.Name + " (" + myUser.Email + ")"; } }
        public string UserId { get { return myUser.UserId; } }

    }

    public class ProjectPoco
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectNumber { get; set; }
        public string Description { get; set; }
        public string ID_Extern { get; set; }
        public bool OGD { get; set; }
        public string ProjectConfigurator { get; set; }
        public string ProjectManager { get; set; }
        public string ProjectThirdPartyToolsString { get; set; }
    }


    public class ProjectViewModel
    {
        public Project Project { get; set; }
        public List<RecordViewModel> Records { get; set; }
        public List<Form> Forms { get; set; }
    }

    public class ExportProcess
    {
        public string Error { get; set; }
        public string Output { get; set; }
        public string Filename { get; set; }

    }
}
