using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using BioDivCollector.WebApp.Helpers;
using FormFactory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace BioDivCollector.WebApp.Controllers
{
    public class RecordsController : Controller
    {
        private BioDivContext db = new BioDivContext();
        private GeneralPluginExtension _generalPluginExtension;

        public RecordsController(GeneralPluginExtension generalPluginExtension)
        {
            _generalPluginExtension = generalPluginExtension;
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            Record r = await db.Records
                .Include(m=>m.ProjectGroup).ThenInclude(pg=>pg.Group).ThenInclude(g=>g.GroupUsers)
                .Where(m => m.RecordId == id).FirstOrDefaultAsync();
            if (r == null) return StatusCode(404);
            // No right for it, User is not in Group

            if ((r.ProjectGroup.Group.GroupUsers.Any(m => m.UserId == user.UserId)) || User.IsInRole("DM"))
            {
                r.StatusId = StatusEnum.deleted;
                ChangeLog cl = new ChangeLog() { Log ="deleted record", User = user };
                db.ChangeLogs.Add(cl);
                ChangeLogRecord clr = new ChangeLogRecord() { ChangeLog = cl, Record = r };
                db.ChangeLogsRecords.Add(clr);

                db.Entry(r).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Content("OK");
            }
            
            // No right for it, User is not in Group

            return StatusCode(403);
            
        }

        // Copy all values from the last record from the user with the same form
        public void CopyRecordFromLastEntry(Record record, Form form, User user)
        {
            try
            {
                List<ChangeLogRecord> lastRecordChangeLogs = db.ChangeLogsRecords.AsNoTracking()
                    .Include(m => m.ChangeLog)
                    .Include(m => m.Record).ThenInclude(r => r.TextData)
                    .Include(m => m.Record).ThenInclude(r => r.NumericData)
                    .Include(m => m.Record).ThenInclude(r => r.BooleanData)
                    .Include(m => m.Record).ThenInclude(r => r.BinaryData)
                    .Include(m => m.Record).ThenInclude(r => r.Form).ThenInclude(m => m.FormFormFields).ThenInclude(m => m.FormField)
                    .Where(m => m.ChangeLog.UserId == user.UserId && m.Record.FormId == form.FormId && m.ChangeLog.Log.Contains("created") && m.Record.StatusId != StatusEnum.deleted && m.Record != record).ToList();
                if (lastRecordChangeLogs == null) return;

                ChangeLogRecord lastRecordChangeLog = lastRecordChangeLogs.OrderBy(m => m.ChangeLog.ChangeDate).Last();

                foreach (FormFormField fff in lastRecordChangeLog.Record.Form.FormFormFields)
                {
                    if ((fff.FormField.FieldTypeId == FieldTypeEnum.Choice) || (fff.FormField.FieldTypeId == FieldTypeEnum.Text) || (fff.FormField.FieldTypeId == FieldTypeEnum.DateTime))
                    {
                        TextData td = lastRecordChangeLog.Record.TextData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                        if (td != null)
                        {
                            TextData tdCopy = new TextData() { Id = Guid.NewGuid(), FormFieldId = fff.FormFieldId, Record = record, Value = td.Value };
                            //if (record.TextData == null) record.TextData = new List<TextData>();
                            record.TextData.Add(tdCopy);
                            db.TextData.Add(tdCopy);
                        }
                    }
                    else if (fff.FormField.FieldTypeId == FieldTypeEnum.Number)
                    {
                        NumericData td = lastRecordChangeLog.Record.NumericData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                        if (td != null)
                        {
                            NumericData tdCopy = new NumericData() { Id = Guid.NewGuid(), FormFieldId = fff.FormFieldId, Record = record, Value = td.Value };
                            //if (record.NumericData == null) record.NumericData = new List<NumericData>();
                            record.NumericData.Add(tdCopy);
                            db.NumericData.Add(tdCopy);
                            db.Entry(tdCopy).State = EntityState.Added;
                        }
                    }
                    else if (fff.FormField.FieldTypeId == FieldTypeEnum.Boolean)
                    {
                        BooleanData td = lastRecordChangeLog.Record.BooleanData.Where(m => m.FormFieldId == fff.FormFieldId).FirstOrDefault();
                        if (td != null)
                        {
                            BooleanData tdCopy = new BooleanData() { Id = Guid.NewGuid(), FormFieldId = fff.FormFieldId, Record = record, Value = td.Value };
                            //if (record.BooleanData == null) record.BooleanData = new List<BooleanData>();
                            record.BooleanData.Add(tdCopy);
                            db.BooleanData.Add(tdCopy);
                            db.Entry(tdCopy).State = EntityState.Added;
                        }
                    }
                    // TODO: BinaryData
                }
            }
            catch (Exception e)
            {
                // No form found... we cannot copy anything
                return;
            }

            //db.SaveChanges();

        }

        public async Task<IActionResult> AddToGeometry(Guid GeometryId, Guid ProjectId, Guid GroupId, int FormId, bool CopyFromLast = false)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            List<Group> myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();
            List<Project> projects = new List<Project>();
            if (User.IsInRole("DM")) projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);

                if (User.IsInRole("EF")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF));
                if (User.IsInRole("PK")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                if (User.IsInRole("PL")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            

            ReferenceGeometry rg = await db.Geometries.FindAsync(GeometryId);
            Form f = await db.Forms.FindAsync(FormId);

            // Check if correct rights for user
            if ((myGroups.Any(m => m.GroupId == GroupId)) && (projects.Any(m => m.ProjectId == ProjectId)) && (rg != null) && (f != null))
            {
                Record r = new Record() { Form = f, Geometry = rg, ProjectGroupGroupId = GroupId, ProjectGroupProjectId = ProjectId };
                ChangeLog cl = new ChangeLog() { Log = "created new record", User = user };
                db.ChangeLogs.Add(cl);
                ChangeLogRecord clr = new ChangeLogRecord() { ChangeLog = cl, Record = r };
                db.ChangeLogsRecords.Add(clr);
                db.Records.Add(r);
                await db.SaveChangesAsync();
                if (CopyFromLast) CopyRecordFromLastEntry(r, f, user);
                await db.SaveChangesAsync();

                return Content("OK");
            }


            return StatusCode(403);
        }

        public async Task<IActionResult> AddToProject(Guid ProjectId, Guid GroupId, int FormId, bool CopyFromLast = false)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            List<Group> myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();
            List<Project> projects = new List<Project>();
            if (User.IsInRole("DM")) projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);

                if (User.IsInRole("EF")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF));
                if (User.IsInRole("PK")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                if (User.IsInRole("PL")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            

            Project p = await db.Projects.FindAsync(ProjectId);
            Form f = await db.Forms.FindAsync(FormId);

            // Check if correct rights for user
            if ((myGroups.Any(m => m.GroupId == GroupId)) && (projects.Any(m => m.ProjectId == ProjectId)) && (p != null) && (f != null))
            {
                Record r = new Record() { Form = f, ProjectGroupGroupId = GroupId, ProjectGroupProjectId = ProjectId };
                ChangeLog cl = new ChangeLog() { Log = "created new record", User = user };
                db.ChangeLogs.Add(cl);
                ChangeLogRecord clr = new ChangeLogRecord() { ChangeLog = cl, Record = r };
                db.ChangeLogsRecords.Add(clr);
                db.Records.Add(r);
                await db.SaveChangesAsync();
                if (CopyFromLast) CopyRecordFromLastEntry(r, f, user);
                await db.SaveChangesAsync();

                return Content("OK");
            }


            return StatusCode(403);
        }

        public static readonly string[] formats = { 
    // Basic formats
    "yyyyMMddTHHmmsszzz",
    "yyyyMMddTHHmmsszz",
    "yyyyMMddTHHmmssZ",
    // Extended formats
    "yyyy-MM-ddTHH:mm:sszzz",
    "yyyy-MM-ddTHH:mm:sszz",
    "yyyy-MM-ddTHH:mm:ssZ",
    // All of the above with reduced accuracy
    "yyyyMMddTHHmmzzz",
    "yyyyMMddTHHmmzz",
    "yyyyMMddTHHmmZ",
    "yyyy-MM-ddTHH:mmzzz",
    "yyyy-MM-ddTHH:mmzz",
    "yyyy-MM-ddTHH:mmZ",
    // Accuracy reduced to hours
    "yyyyMMddTHHzzz",
    "yyyyMMddTHHzz",
    "yyyyMMddTHHZ",
    "yyyy-MM-ddTHHzzz",
    "yyyy-MM-ddTHHzz",
    "yyyy-MM-ddTHHZ",

    // simple
    "dd.MM.yyyy"



    };


        [HttpPost]
        public async Task<IActionResult> Save(IFormCollection form)
        {
            try
            {
                JObject parameters = form.ToJObject();
                string recordID = parameters.GetValue("RecordId").ToString();
                if (recordID != null)
                {
                    Record r = await db.Records.Include(m => m.TextData).ThenInclude(td => td.FormField)
                        .Include(u => u.NumericData).ThenInclude(td => td.FormField)
                        .Include(u => u.BooleanData).ThenInclude(td => td.FormField)
                        .Include(u => u.BinaryData).ThenInclude(td => td.FormField) 
                        .Include(u => u.Form).ThenInclude(m => m.FormFormFields).ThenInclude(fff => fff.FormField)
                        .Include(u => u.Form).ThenInclude(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo=>mo.PublicMotherFormField)
                        .Where(m => m.RecordId == new Guid(recordID)).FirstOrDefaultAsync();

                    foreach (FormField ff in r.Form.FormFormFields.Select(fff=>fff.FormField))
                    {
                        if (ff.FieldTypeId == FieldTypeEnum.Text)
                        {
                            if (parameters.GetValue("Field_" + ff.FormFieldId) != null)
                            {
                                string newValue = parameters.GetValue("Field_" + ff.FormFieldId).ToString();
                                TextData td = r.TextData.Where(m => m.FormField!=null && m.FormField.FormFieldId == ff.FormFieldId).FirstOrDefault();
                                if (td == null)
                                {
                                    td = new TextData() { FormField = ff, Record = r, Id = Guid.NewGuid(), Value = newValue };
                                    r.TextData.Add(td);
                                    db.Entry(td).State = EntityState.Added;
                                    db.Entry(r).State = EntityState.Modified;
                                }
                                else
                                {
                                    td.Value = newValue;
                                    db.Entry(td).State = EntityState.Modified;
                                }

                            }
                        }
                        else if (ff.FieldTypeId == FieldTypeEnum.DateTime)
                        {
                            if (parameters.GetValue("Field_" + ff.FormFieldId) != null)
                            {
                                string newValue = parameters.GetValue("Field_" + ff.FormFieldId).ToString();
                                DateTime myDT;
                                if (newValue != "")
                                {
                                        myDT = DateTime.ParseExact(newValue.Replace("{0:", " ").Replace("}", ""), formats, CultureInfo.InvariantCulture, DateTimeStyles.None);


                                        string zulu = myDT
                                 .ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");

                                        TextData td = r.TextData.Where(m => m.FormField!=null && m.FormField.FormFieldId == ff.FormFieldId).FirstOrDefault();
                                        if (td == null)
                                        {
                                            td = new TextData() { FormField = ff, Record = r, Id = Guid.NewGuid(), Value = zulu };
                                            r.TextData.Add(td);
                                            db.Entry(td).State = EntityState.Added;
                                            db.Entry(r).State = EntityState.Modified;
                                        }
                                        else
                                        {
                                            td.Value = zulu;
                                            db.Entry(td).State = EntityState.Modified;
                                        }
                                }

                            }
                        }
                        else if (ff.FieldTypeId == FieldTypeEnum.Choice)
                        {
                            if (parameters.GetValue("Field_" + ff.FormFieldId) != null)
                            {
                                string newValue = parameters.GetValue("Field_" + ff.FormFieldId).ToString();
                                await db.Entry(ff).Collection(m => m.FieldChoices).LoadAsync();
                                FieldChoice fc = ff.FieldChoices.Where(m => m.Text == newValue).FirstOrDefault();
                                if (ff.PublicMotherFormField != null)
                                {
                                    await db.Entry(ff.PublicMotherFormField).Collection(m => m.FieldChoices).LoadAsync();
                                    fc = ff.PublicMotherFormField.FieldChoices.Where(m => m.Text == newValue).FirstOrDefault();
                                }

                                // do we have fieldchoices with option|label?
                                if (fc==null)
                                {if (ff.PublicMotherFormField != null)
                                    {
                                        foreach (FieldChoice fcSplit in ff.PublicMotherFormField.FieldChoices)
                                        {

                                            if ((fcSplit.Text.Contains("|" + newValue)) || (fcSplit.Text.Contains("| " + newValue)))
                                            {
                                                fc = fcSplit;
                                                newValue = fcSplit.Text.Split('|')[0];
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (FieldChoice fcSplit in ff.FieldChoices)
                                        {

                                            if ((fcSplit.Text.Contains("|" + newValue)) || (fcSplit.Text.Contains("| " + newValue)))
                                            {
                                                fc = fcSplit;
                                                newValue = fcSplit.Text.Split('|')[0];
                                            }
                                        }
                                    }
                                }

                                TextData td = r.TextData.Where(m => m.FormField!=null && m.FormField.FormFieldId == ff.FormFieldId).FirstOrDefault();
                                if (td == null)
                                {
                                    td = new TextData() { FormField = ff, Record = r, Id = Guid.NewGuid(), Value = newValue, FieldChoice = fc };
                                    r.TextData.Add(td);
                                    db.Entry(td).State = EntityState.Added;
                                    db.Entry(r).State = EntityState.Modified;
                                }
                                else
                                {
                                    td.Value = newValue;
                                    td.FieldChoice = fc;
                                    db.Entry(td).State = EntityState.Modified;
                                }

                            }
                        }
                        else if (ff.FieldTypeId == FieldTypeEnum.Boolean)
                        {
                            if (parameters.GetValue("Field_" + ff.FormFieldId) != null)
                            {
                                bool newValue = (bool)parameters.GetValue("Field_" + ff.FormFieldId).ToObject<bool>();
                                BooleanData bd = r.BooleanData.Where(m => m.FormField.FormFieldId == ff.FormFieldId).FirstOrDefault();
                                if (bd == null)
                                {
                                    bd = new BooleanData() { FormField = ff, Record = r, Id = Guid.NewGuid(), Value = newValue };
                                    r.BooleanData.Add(bd);
                                    db.Entry(bd).State = EntityState.Added;
                                    db.Entry(r).State = EntityState.Modified;
                                }
                                else
                                {
                                    bd.Value = newValue;
                                    db.Entry(bd).State = EntityState.Modified;
                                }

                            }
                        }

                    }

                    User user = Helpers.UserHelper.GetCurrentUser(User, db);

                    ChangeLog cl = new ChangeLog() { Log = user.UserId + " changed the record", User = user };
                    ChangeLogRecord clr = new ChangeLogRecord() { Record = r, ChangeLog = cl };
                    if (r.RecordChangeLogs == null) r.RecordChangeLogs = new List<ChangeLogRecord>();
                    r.RecordChangeLogs.Add(clr);

                    await db.SaveChangesAsync();
                }
                return Content("OK");
            }
            catch (Exception e)
            {
                return Content(e.ToString()); ;
            }

        }

        public async Task<IActionResult> Move(Guid from, Guid to)
        {
            Record r = await db.Records.Where(m => m.RecordId == from).FirstOrDefaultAsync();
            if (r == null) return Content("Error");
            ReferenceGeometry rg = await db.Geometries.Where(m => m.GeometryId == to).FirstOrDefaultAsync();
            if (rg == null) return Content("Error");

            r.GeometryId = rg.GeometryId;
            db.Entry(r).State = EntityState.Modified;
            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            ChangeLog cl = new ChangeLog() { Log = "Moved Record to new Geometry", User = user };
            ChangeLogRecord cr = new ChangeLogRecord() { ChangeLog = cl, Record = r };
            db.ChangeLogs.Add(cl);
            db.ChangeLogsRecords.Add(cr);

            await db.SaveChangesAsync();

            return Content("OK");

        }



        /// <summary>
        /// Get all Records for a project and show it as a table
        /// </summary>
        /// <param name="id">ProjectId</param>
        /// <returns></returns>
        public async Task<IActionResult> RecordsPerProjectAsTable(Guid id)
        {
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

            /*Project p = await db.Projects
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.TextData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.NumericData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.BooleanData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff=>fff.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo=>mo.PublicMotherFormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.ProjectGroup.Group)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.Geometry)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog).ThenInclude(cl => cl.User)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).Where(pg => pg.StatusId != StatusEnum.deleted)
                .Where(m => m.ProjectId == id)
                .Where(m => m.StatusId != StatusEnum.deleted).FirstOrDefaultAsync();*/

            Project p = await db.Projects
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).FirstOrDefaultAsync();

            await db.Entry(p).Collection(m => m.ProjectGroups).LoadAsync();

            foreach (ProjectGroup pg in p.ProjectGroups)
            {
                await db.Entry(pg).Collection(m => m.Geometries).LoadAsync();
                await db.Entry(pg).Collection(m => m.Records).Query().
                    Include(u => u.TextData).ThenInclude(td => td.FormField).
                    Include(u => u.NumericData).ThenInclude(td => td.FormField).
                    Include(u => u.BooleanData).ThenInclude(td => td.FormField).
                    Include(u => u.BinaryData).ThenInclude(td => td.FormField).
                    Include(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo => mo.PublicMotherFormField).
                    Include(u => u.ProjectGroup.Group).
                    Include(u => u.Geometry).
                    Include(u => u.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog).ThenInclude(cl => cl.User)
                    .Where(pg => pg.StatusId != StatusEnum.deleted)
                    .LoadAsync();

            }




            if (p == null) return StatusCode(500);
            if (!projects.Any(m => m.ProjectId == p.ProjectId)) return RedirectToAction("NotAllowed", "Home");

            ProjectViewModel pvm = new ProjectViewModel() { Project = p };
            pvm.Records = new List<RecordViewModel>();
            pvm.Forms = new List<Form>();

            List<Group> myGroups;
            if (User.IsInRole("DM")) myGroups= await db.Groups.ToListAsync();
            else if ((User.IsInRole("PK")) || (User.IsInRole("PL")))
            {
                await db.Entry(p).Reference(m => m.ProjectManager).LoadAsync();
                await db.Entry(p).Reference(m => m.ProjectConfigurator).LoadAsync();
                if ((p.ProjectConfigurator.UserId==user.UserId) || (p.ProjectManager.UserId == user.UserId)) myGroups = await db.Groups.ToListAsync();
                else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();
            }
            else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();

            foreach (ProjectGroup g in p.ProjectGroups)
            {
                List<Record> records = g.Records.Where(m => m.StatusId != StatusEnum.deleted && m.Geometry?.StatusId != StatusEnum.deleted).ToList();
                foreach (Record r in records)
                {
                    bool isReadOnly = true;
                    if ((g.GroupStatusId != GroupStatusEnum.Gruppendaten_gueltig) && (g.GroupStatusId != GroupStatusEnum.Gruppendaten_erfasst))
                        if (myGroups.Where(m => m.GroupId == g.GroupId).Count() > 0) isReadOnly = false;
                    RecordViewModel rvm = new RecordViewModel() { Record = r };
                    rvm.Readonly = isReadOnly;
                    if (!pvm.Forms.Any(m => m.FormId == r.FormId)) pvm.Forms.Add(r.Form);
                    pvm.Records.Add(rvm);
                }

                // add all geometries without records
                List<ReferenceGeometry> geometries = g.Geometries.Where(m => m.Records.Count == 0 && m.StatusId != StatusEnum.deleted).ToList();
                foreach (ReferenceGeometry rg in geometries)
                {
                    bool isReadOnly = true;
                    if ((g.GroupStatusId != GroupStatusEnum.Gruppendaten_gueltig) && (g.GroupStatusId != GroupStatusEnum.Gruppendaten_erfasst))
                        if (myGroups.Where(m => m.GroupId == g.GroupId).Count() > 0) isReadOnly = false;

                    await db.Entry(rg).Collection(m => m.GeometryChangeLogs).Query().Include(u => u.ChangeLog).ThenInclude(usr=>usr.User).LoadAsync();
                    ChangeLogGeometry lastChange = rg.GeometryChangeLogs.OrderBy(cl => cl.ChangeLogId).Last();

                    Record rNew = new Record() { Geometry = rg };

                    ChangeLog cl = new ChangeLog() { User = lastChange.ChangeLog.User, ChangeDate = lastChange.ChangeLog.ChangeDate };
                    ChangeLogRecord clr = new ChangeLogRecord() { ChangeLog = cl, Record = rNew };
                    rNew.RecordChangeLogs.Add(clr);

                    RecordViewModel rvm = new RecordViewModel() { Record = rNew };
                    rvm.Readonly = isReadOnly;
                    pvm.Records.Add(rvm);
                }

            }
            if (!erfassendeProjects.Contains(p)) ViewData["ReadOnly"] = true;
            else ViewData["ReadOnly"] = false;


            if ((User.IsInRole("DM")) || (User.IsInRole("PK")) || (User.IsInRole("PL"))) ViewData["CanChangeGroup"] = true;
            else ViewData["CanChangeGroup"] = false;

            return View(pvm);
        }

        /// <summary>
        /// Get all Records for a project
        /// </summary>
        /// <param name="id">ProjectId</param>
        /// <returns></returns>
        public async Task<IActionResult> RecordsPerProject(Guid id, bool withOnlyGeometries = false)
        {
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

            /*Project p = await db.Projects
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.TextData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.NumericData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.BooleanData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo=>mo.PublicMotherFormField)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.ProjectGroup.Group)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.Geometry)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(u => u.RecordChangeLogs).ThenInclude(rcl=>rcl.ChangeLog).ThenInclude(cl => cl.User)
                .Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).Where(pg=>pg.StatusId!=StatusEnum.deleted)
                .Where(m => m.ProjectId == id)
                .Where(m => m.StatusId != StatusEnum.deleted).FirstOrDefaultAsync();*/

            Project p = await db.Projects
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).FirstOrDefaultAsync();

            await db.Entry(p).Collection(m => m.ProjectGroups).LoadAsync();

            foreach (ProjectGroup pg in p.ProjectGroups)
            {
                await db.Entry(pg).Collection(m => m.Geometries).LoadAsync();
                await db.Entry(pg).Collection(m => m.Records).Query().
                    Include(u => u.TextData).ThenInclude(td => td.FormField).
                    Include(u => u.NumericData).ThenInclude(td => td.FormField).
                    Include(u => u.BooleanData).ThenInclude(td => td.FormField).
                    Include(u => u.BinaryData).ThenInclude(td => td.FormField).
                    Include(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo=>mo.PublicMotherFormField).
                    Include(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(h => h.HiddenFieldChoices).
                    Include(u => u.ProjectGroup.Group).
                    Include(u => u.Geometry).
                    Include(u => u.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog).ThenInclude(cl => cl.User)
                    .Where(pg => pg.StatusId != StatusEnum.deleted)
                    .LoadAsync();

            }


            if (p == null) return StatusCode(500);
            if (!projects.Any(m => m.ProjectId == p.ProjectId)) return RedirectToAction("NotAllowed", "Home");

            ProjectViewModel pvm = new ProjectViewModel() { Project = p };
            pvm.Records = new List<RecordViewModel>();

            List<Group> myGroups;
            if (User.IsInRole("DM")) myGroups = await db.Groups.ToListAsync();
            else if ((User.IsInRole("PK")) || (User.IsInRole("PL")))
            {
                await db.Entry(p).Reference(m => m.ProjectManager).LoadAsync();
                await db.Entry(p).Reference(m => m.ProjectConfigurator).LoadAsync();
                if ((p.ProjectConfigurator.UserId == user.UserId) || (p.ProjectManager.UserId == user.UserId)) myGroups = await db.Groups.ToListAsync();
                else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();
            }
            else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();

            foreach (ProjectGroup g in p.ProjectGroups)
            {
                List<Record> records = g.Records.Where(m => m.StatusId != StatusEnum.deleted && m.Geometry == null).ToList();
                if (withOnlyGeometries) records = g.Records.Where(m => m.StatusId != StatusEnum.deleted && m.Geometry != null).ToList();

                foreach (Record r in records)
                {
                    bool isReadOnly = true;
                    if ((g.GroupStatusId != GroupStatusEnum.Gruppendaten_gueltig) && (g.GroupStatusId != GroupStatusEnum.Gruppendaten_erfasst))
                        if (myGroups.Where(m => m.GroupId == g.GroupId).Count() > 0) isReadOnly = false;

                    RecordViewModel rvm = new RecordViewModel() { Record = r };

                    List<PropertyVm> dynamicForm = new List<PropertyVm>();

                    // BDC Guid
                    PropertyVm dynamicFieldGUID = new PropertyVm(typeof(string), "Field_" + r.RecordId);
                    dynamicFieldGUID.DisplayName = "BDCGuid";
                    dynamicFieldGUID.Value = r.BDCGuid;
                    dynamicFieldGUID.GetCustomAttributes = () => new object[] { new Helpers.FormFactory.GuidAttribute() };
                    dynamicForm.Add(dynamicFieldGUID);

                    if (r.Form != null)
                    {
                        foreach (FormField ff in r.Form.FormFormFields.Select(fff=>fff.FormField).OrderBy(m => m.Order))
                        {
                            FormField origFormField = ff;
                            if (ff.PublicMotherFormField != null) origFormField = ff.PublicMotherFormField;

                            if (origFormField.FieldTypeId == FieldTypeEnum.Text)
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(string), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                TextData td = r.TextData.Where(m => m.FormField == ff).FirstOrDefault();
                                if (td != null) dynamicField.Value = td.Value;

                                // Check if there is a standardvalue plugin
                                string standardValue = "";
                                foreach (IPlugin plugin in _generalPluginExtension.Plugins)
                                {
                                    if (plugin is BaseStandardValueGenerator)
                                    {
                                        standardValue = ((BaseStandardValueGenerator)plugin).GetStandardValue(ff, null, r, user);
                                    }
                                }
                                if (standardValue != "")
                                {
                                    dynamicField.Value = standardValue;
                                    if (ff.StandardValue.StartsWith("="))
                                        dynamicField.GetCustomAttributes = () => new object[] { new Helpers.FormFactory.StandardValueAttribute() };
                                }

                                dynamicField.NotOptional = ff.Mandatory;
                                dynamicForm.Add(dynamicField);
                            }
                            else if (origFormField.FieldTypeId == FieldTypeEnum.DateTime)
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(DateTime), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                TextData td = r.TextData.Where(m => m.FormField == ff).FirstOrDefault();
                                DateTime myDT = new DateTime();
                                try
                                {
                                    myDT = DateTime.ParseExact(td.Value.Replace("{0:", " ").Replace("}", ""), formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                                }
                                catch (Exception e)
                                {

                                }

                                if (td != null) dynamicField.Value = myDT;

                                dynamicField.NotOptional = ff.Mandatory;
                                dynamicForm.Add(dynamicField);
                            }
                            else if (origFormField.FieldTypeId == FieldTypeEnum.Binary)
                            {
                                if (r.BinaryData.Count() == 0)
                                {
                                    PropertyVm dynamicField = new PropertyVm(typeof(BinaryData), "Field_" + ff.FormFieldId.ToString());
                                    dynamicField.DisplayName = origFormField.Title;
                                    dynamicField.DataAttributes = new Dictionary<string, string> { { "recordid", r.RecordId.ToString() }, { "formfieldid", ff.FormFieldId.ToString() } };
                                    dynamicField.NotOptional = ff.Mandatory;
                                    dynamicField.GetCustomAttributes = () => new object[] { new FormFactory.Attributes.LabelOnRightAttribute() };
                                    dynamicForm.Add(dynamicField);
                                }
                                else
                                {
                                    foreach (BinaryData bd in r.BinaryData.Where(m => m.FormField == ff))
                                    {
                                        PropertyVm dynamicField = new PropertyVm(typeof(BinaryData), "Field_" + ff.FormFieldId.ToString());
                                        dynamicField.DisplayName = origFormField.Title;
                                        if (bd != null) dynamicField.Value = bd.Id;
                                        dynamicField.DataAttributes = new Dictionary<string, string> { { "recordid", r.RecordId.ToString() }, { "formfieldid", ff.FormFieldId.ToString() } };
                                        dynamicField.NotOptional = ff.Mandatory;
                                        dynamicField.GetCustomAttributes = () => new object[] { new FormFactory.Attributes.LabelOnRightAttribute() };
                                        dynamicForm.Add(dynamicField);
                                    }
                                }
                            }
                            else if (origFormField.FieldTypeId == FieldTypeEnum.Choice)
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(string), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                TextData td = r.TextData.Where(m => m.FormField == ff).FirstOrDefault();
                                await db.Entry(origFormField).Collection(m => m.FieldChoices).LoadAsync();
                                if (td != null)
                                {
                                    string text = td.Value;

                                    // check if the choices have format value|label. Search for the label
                                    foreach (FieldChoice fc in origFormField.FieldChoices.OrderBy(m => m.Order))
                                    {
                                        if (fc.Text.Contains("|"))
                                        {
                                            string[] value = fc.Text.Split("|");
                                            if (value[0].TrimEnd(' ') == text) text = value[1];
                                        }
                                    }

                                    dynamicField.Value = text;
                                }
                                if (origFormField.FieldChoices != null)
                                {
                                    List<string> choices = new List<string>();
                                    foreach (FieldChoice fc in origFormField.FieldChoices.OrderBy(m => m.Order))
                                    {
                                        // only add the fieldchoice when it is not in the HiddenFieldChoiceList of the main formfield (not the public)
                                        if (!ff.HiddenFieldChoices.Where(m => m.FieldChoice == fc && m.FormField == ff).Any())
                                        {
                                            // split by | for different value and text
                                            string text = fc.Text;
                                            string[] value = text.Split("|");
                                            if (value.Length > 1) text = value[1].TrimStart(' ');
                                            choices.Add(text);
                                        }
                                            
                                    }
                                    dynamicField.Choices = choices;
                                }
                                dynamicField.NotOptional = ff.Mandatory;
                                dynamicForm.Add(dynamicField);
                            }
                            else if (origFormField.FieldTypeId == FieldTypeEnum.Boolean)
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(bool), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                BooleanData bd = r.BooleanData.Where(m => m.FormField == origFormField).FirstOrDefault();
                                if (bd != null) dynamicField.Value = bd.Value;
                                dynamicField.NotOptional = ff.Mandatory;
                                dynamicField.GetCustomAttributes = () => new object[] { new FormFactory.Attributes.LabelOnRightAttribute() };
                                dynamicForm.Add(dynamicField);
                            }
                            else if (origFormField.FieldTypeId == FieldTypeEnum.Header)
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(string), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                dynamicField.GetCustomAttributes = () => new object[] { new Helpers.FormFactory.HeaderAttribute() };
                                dynamicForm.Add(dynamicField);
                            }

                        }

                        PropertyVm dynamicHiddenField = new PropertyVm(typeof(string), "RecordId");
                        dynamicHiddenField.Value = r.RecordId.ToString();
                        dynamicHiddenField.NotOptional = true;
                        dynamicHiddenField.IsHidden = true;
                        dynamicForm.Add(dynamicHiddenField);

                        if (isReadOnly)
                        {
                            foreach (PropertyVm pv in dynamicForm)
                            {
                                pv.Readonly = true;
                            }
                        }
                        rvm.Readonly = isReadOnly;
                        rvm.DynamicForm = dynamicForm;
                        rvm.Group = r.ProjectGroup.Group;
                        pvm.Records.Add(rvm);
                    }

                }
            }
            ViewData["withOnlyGeometries"] = withOnlyGeometries;
            if (!erfassendeProjects.Contains(p)) ViewData["ReadOnly"] = true;
            else ViewData["ReadOnly"] = false;

            if ((User.IsInRole("DM")) || (User.IsInRole("PK")) || (User.IsInRole("PL"))) ViewData["CanChangeGroup"] = true;
            else ViewData["CanChangeGroup"] = false;

            if (withOnlyGeometries) return View("RecordsPerProjectPerGeometry", pvm);
            return View(pvm);
        }

        public static async Task CreateDynamicView(BioDivContext db, ReferenceGeometry rg, ProjectGroup g, List<Group> myGroups, GeometrieViewModel gvm, GeneralPluginExtension generalPluginExtension, User user)
        {
            foreach (Record r in rg.Records.Where(m => m.StatusId != StatusEnum.deleted))
            {
                bool isReadOnly = true;
                if (g == null) isReadOnly = false;
                else if ((g.GroupStatusId != GroupStatusEnum.Gruppendaten_gueltig) && (g.GroupStatusId != GroupStatusEnum.Gruppendaten_erfasst))
                    if (myGroups.Where(m => m.GroupId == r.ProjectGroupGroupId).Count() > 0) isReadOnly = false;

                RecordViewModel rvm = new RecordViewModel() { Record = r };

                List<PropertyVm> dynamicForm = new List<PropertyVm>();

                // BDC Guid
                PropertyVm dynamicFieldGUID = new PropertyVm(typeof(string), "Field_" + r.RecordId);
                dynamicFieldGUID.DisplayName = "BDCGuid";
                dynamicFieldGUID.Value = r.BDCGuid;
                dynamicFieldGUID.GetCustomAttributes = () => new object[] { new Helpers.FormFactory.GuidAttribute() };
                dynamicForm.Add(dynamicFieldGUID);

                if (r.Form != null)
                {
                    foreach (FormField ff in r.Form.FormFormFields.Select(fff => fff.FormField).OrderBy(m => m.Order))
                    {
                        FormField origFormField = ff;
                        if (ff.PublicMotherFormField != null) origFormField = ff.PublicMotherFormField;

                        if (origFormField.FieldTypeId == FieldTypeEnum.Text)
                        {
                            PropertyVm dynamicField = new PropertyVm(typeof(string), "Field_" + ff.FormFieldId.ToString());
                            dynamicField.DisplayName = origFormField.Title;
                            TextData td = r.TextData.Where(m => m.FormField == ff).FirstOrDefault();
                            if (td != null) dynamicField.Value = td.Value;

                            // Check if there is a standardvalue plugin
                            string standardValue = "";
                            foreach (IPlugin p in generalPluginExtension.Plugins)
                            {
                                if ( p is BaseStandardValueGenerator)
                                {
                                    standardValue = ((BaseStandardValueGenerator)p).GetStandardValue(ff, rg, r, user);
                                }
                            }

                            if (standardValue != "")
                            {
                                dynamicField.Value = standardValue;
                                if (ff.StandardValue.StartsWith("="))
                                    dynamicField.GetCustomAttributes = () => new object[] { new Helpers.FormFactory.StandardValueAttribute() };
                            }

                            dynamicField.NotOptional = ff.Mandatory;
                            dynamicForm.Add(dynamicField);
                        }
                        else if (origFormField.FieldTypeId == FieldTypeEnum.DateTime)
                        {
                            PropertyVm dynamicField = new PropertyVm(typeof(DateTime), "Field_" + ff.FormFieldId.ToString());
                            dynamicField.DisplayName = origFormField.Title;
                            TextData td = r.TextData.Where(m => m.FormField == ff).FirstOrDefault();
                            DateTime myDT = new DateTime();
                            try
                            {
                                myDT = DateTime.ParseExact(td.Value.Replace("{0:", " ").Replace("}", ""), formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                                if (td != null) dynamicField.Value = myDT;
                            }
                            catch (Exception e)
                            {

                            }


                            dynamicField.NotOptional = ff.Mandatory;
                            dynamicForm.Add(dynamicField);
                        }
                        else if (origFormField.FieldTypeId == FieldTypeEnum.Binary)
                        {
                            if (r.BinaryData.Count() == 0)
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(BinaryData), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                dynamicField.DataAttributes = new Dictionary<string, string> { { "recordid", r.RecordId.ToString() }, { "formfieldid", ff.FormFieldId.ToString() } };
                                dynamicField.NotOptional = ff.Mandatory;
                                dynamicField.GetCustomAttributes = () => new object[] { new FormFactory.Attributes.LabelOnRightAttribute() };
                                dynamicForm.Add(dynamicField);
                            }
                            else
                            {
                                PropertyVm dynamicField = new PropertyVm(typeof(BinaryData), "Field_" + ff.FormFieldId.ToString());
                                dynamicField.DisplayName = origFormField.Title;
                                dynamicField.DataAttributes = new Dictionary<string, string> { { "recordid", r.RecordId.ToString() }, { "formfieldid", ff.FormFieldId.ToString() } };
                                dynamicField.NotOptional = ff.Mandatory;
                                dynamicField.GetCustomAttributes = () => new object[] { new FormFactory.Attributes.LabelOnRightAttribute() };
                                dynamicForm.Add(dynamicField);
                                List<Guid> guids = new List<Guid>();
                                foreach (BinaryData bd in r.BinaryData.Where(m => m.FormField == ff))
                                {
                                    guids.Add(bd.Id);
                                }

                                dynamicField.Value = guids;
                            }
                        }
                        else if (origFormField.FieldTypeId == FieldTypeEnum.Choice)
                        {
                            PropertyVm dynamicField = new PropertyVm(typeof(string), "Field_" + ff.FormFieldId.ToString());
                            dynamicField.DisplayName = origFormField.Title;
                            TextData td = r.TextData.Where(m => m.FormField == ff).FirstOrDefault();

                            await db.Entry(origFormField).Collection(m => m.FieldChoices).LoadAsync();
                            if (td != null)
                            {
                                string text = td.Value;

                                // check if the choices have format value|label. Search for the label
                                foreach (FieldChoice fc in origFormField.FieldChoices.OrderBy(m => m.Order))
                                {
                                    if (fc.Text.Contains("|"))
                                    {
                                        string[] value = fc.Text.Split("|");
                                        if (value[0].TrimEnd(' ') == text) text = value[1];
                                    }
                                }

                                dynamicField.Value = text;
                            }


                            if (origFormField.FieldChoices != null)
                            {
                                List<string> choices = new List<string>();
                                foreach (FieldChoice fc in origFormField.FieldChoices.OrderBy(m => m.Order))
                                {
                                    // only add the fieldchoice when it is not in the HiddenFieldChoiceList of the main formfield (not the public)
                                    if (!ff.HiddenFieldChoices.Where(m => m.FieldChoice == fc && m.FormField == ff).Any())
                                    {
                                        // split by | for different value and text
                                        string text = fc.Text;
                                        string[] value = text.Split("|");
                                        if (value.Length > 1) text = value[1].TrimStart(' ');
                                        choices.Add(text);
                                    }
                                }
                                dynamicField.Choices = choices;
                            }
                            dynamicField.NotOptional = ff.Mandatory;
                            dynamicForm.Add(dynamicField);
                        }
                        else if (origFormField.FieldTypeId == FieldTypeEnum.Boolean)
                        {
                            PropertyVm dynamicField = new PropertyVm(typeof(bool), "Field_" + ff.FormFieldId.ToString());
                            dynamicField.DisplayName = origFormField.Title;
                            BooleanData bd = r.BooleanData.Where(m => m.FormField == ff).FirstOrDefault();
                            if (bd != null) dynamicField.Value = bd.Value;
                            dynamicField.NotOptional = ff.Mandatory;
                            dynamicField.GetCustomAttributes = () => new object[] { new FormFactory.Attributes.LabelOnRightAttribute() };
                            dynamicForm.Add(dynamicField);
                        }
                        else if (origFormField.FieldTypeId == FieldTypeEnum.Header)
                        {
                            PropertyVm dynamicField = new PropertyVm(typeof(string), "Field_" + ff.FormFieldId.ToString());
                            dynamicField.DisplayName = origFormField.Title;
                            dynamicField.GetCustomAttributes = () => new object[] { new Helpers.FormFactory.HeaderAttribute() };
                            dynamicForm.Add(dynamicField);
                        }

                    }

                    PropertyVm dynamicHiddenField = new PropertyVm(typeof(string), "RecordId");
                    dynamicHiddenField.Value = r.RecordId.ToString();
                    dynamicHiddenField.NotOptional = true;
                    dynamicHiddenField.IsHidden = true;
                    dynamicForm.Add(dynamicHiddenField);


                    if (isReadOnly)
                    {
                        foreach (PropertyVm pv in dynamicForm)
                        {
                            pv.Readonly = true;
                        }
                    }
                    rvm.Readonly = isReadOnly;
                    rvm.DynamicForm = dynamicForm;
                    rvm.Group = r.ProjectGroup.Group;
                    gvm.Records.Add(rvm);
                }
            }

            return;
        }


        public async Task<IActionResult> RecordsPerGeometry(Guid id, bool withOnlyGeometries = false)
        {
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

            /*Project p = await db.Projects
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.TextData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.NumericData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.BooleanData).ThenInclude(td => td.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo=>mo.PublicMotherFormField)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.ProjectGroup.Group)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.Geometry)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).ThenInclude(u => u.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog).ThenInclude(cl => cl.User)
                .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries).ThenInclude(pg => pg.Records).Where(pg => pg.StatusId != StatusEnum.deleted)
                .Where(m => m.ProjectId == id)
                .Where(m => m.StatusId != StatusEnum.deleted).FirstOrDefaultAsync();*/

            Project p = await db.Projects
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == id).FirstOrDefaultAsync();

            await db.Entry(p).Collection(m => m.ProjectGroups).LoadAsync();

            foreach (ProjectGroup pg in p.ProjectGroups)
            {
                await db.Entry(pg).Collection(m => m.Geometries).LoadAsync();

                foreach (ReferenceGeometry rg in pg.Geometries)
                {

                    await db.Entry(rg).Collection(m => m.Records).Query().
                        Include(u => u.TextData).ThenInclude(td => td.FormField).
                        Include(u => u.NumericData).ThenInclude(td => td.FormField).
                        Include(u => u.BooleanData).ThenInclude(td => td.FormField).
                        Include(u => u.BinaryData).ThenInclude(td => td.FormField).
                        Include(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo => mo.PublicMotherFormField).
                        Include(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(h => h.HiddenFieldChoices).
                        Include(u => u.ProjectGroup.Group).
                        Include(u => u.Geometry).
                        Include(u => u.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog).ThenInclude(cl => cl.User)
                        .Where(pg => pg.StatusId != StatusEnum.deleted)
                        .LoadAsync();
                }

            }




            if (p == null) return StatusCode(500);
            if (!projects.Any(m => m.ProjectId == p.ProjectId)) return RedirectToAction("NotAllowed", "Home");

            List<GeometrieViewModel> gvms = new List<GeometrieViewModel>();

            List<Group> myGroups;
            if (User.IsInRole("DM")) myGroups = await db.Groups.ToListAsync();
            else if ((User.IsInRole("PK")) || (User.IsInRole("PL")))
            {
                await db.Entry(p).Reference(m => m.ProjectManager).LoadAsync();
                await db.Entry(p).Reference(m => m.ProjectConfigurator).LoadAsync();
                if ((p.ProjectConfigurator.UserId == user.UserId) || (p.ProjectManager.UserId == user.UserId)) myGroups = await db.Groups.ToListAsync();
                else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();
            }
            else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();

            foreach (ProjectGroup g in p.ProjectGroups)
            {
                List<ReferenceGeometry> geometries = g.Geometries.Where(m => m.StatusId != StatusEnum.deleted).ToList();
                foreach (ReferenceGeometry rg in geometries)
                {
                    GeometrieViewModel gvm = new GeometrieViewModel() { Geometry = rg };
                    gvm.Records = new List<RecordViewModel>();

                    await CreateDynamicView(db, rg, g, myGroups, gvm, _generalPluginExtension, user);

                    if (gvms.Any(m=>m.Geometry.GeometryId==gvm.Geometry.GeometryId))
                    {
                        GeometrieViewModel alreadyExist = gvms.Where(m => m.Geometry.GeometryId == gvm.Geometry.GeometryId).First();
                        alreadyExist.Records.AddRange(gvm.Records);
                    }
                    else gvms.Add(gvm);
                }
            }
            ViewData["withOnlyGeometries"] = withOnlyGeometries; 
            if (!erfassendeProjects.Contains(p)) ViewData["ReadOnly"] = true;
            else ViewData["ReadOnly"] = false;
            GeometriesViewModel returnObject = new GeometriesViewModel() { Project = p, GeometrieViewModels = gvms };
            return View(returnObject);
        }
    }

    public class GeometriesViewModel
    {
        public Project Project { get; set; }
        public List<GeometrieViewModel> GeometrieViewModels { get; set; }
    }

    public class RecordViewModel
    {
        public Record Record { get; set; }
        public List<PropertyVm> DynamicForm { get; set; }
        public bool Readonly { get; set; }
        public Group Group { get; set; }
    }
}
