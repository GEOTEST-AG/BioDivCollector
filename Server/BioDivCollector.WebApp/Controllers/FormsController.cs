﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using AspNetCore;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.WebApp.Helpers;
using FormFactory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using RunProcessAsTask;

namespace BioDivCollector.WebApp.Controllers
{
    public class FormsController : Controller
    {
        private BioDivContext db = new BioDivContext();
        public IConfiguration Configuration { get; }
        public FormsController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            List<Form> forms = await db.Forms.Include(m => m.FormProjects).ThenInclude(fp => fp.Project)
                .Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField)
                .Include(m => m.FormChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User)
                .ToListAsync();

            List<FormPoco> fps = new List<FormPoco>();

            if (User.IsInRole("DM"))
            {
                foreach (Form f in forms)
                {
                    FormPoco fp = new FormPoco() { Form = f, Editable = true, Author = f.FormChangeLogs?.First().ChangeLog?.User };

                    fp.RecordsCount = db.Records.Where(m => m.FormId == f.FormId).Count();

                    fps.Add(fp);
                }
                return View(fps);
            }

            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            foreach (Form f in forms)
            {
                FormPoco fp = new FormPoco() { Form = f, Editable = false, Author = f.FormChangeLogs?.First().ChangeLog?.User };
                if (f.FormChangeLogs?.First().ChangeLog?.User == user) fp.Editable = true;
                fp.RecordsCount = db.Records.Where(m => m.FormId == f.FormId).Count();
                fps.Add(fp);
            }
            return View(fps);

        }

        public async Task<IActionResult> EditBigFields(int id)
        {
            if ((!User.IsInRole("DM")) && (!User.IsInRole("PL")) && (!User.IsInRole("PK"))) return RedirectToAction("NotAllowed", "Home");


            FormField ff = await db.FormFields.Include(m => m.FieldChoices)
                .Include(m=>m.PublicMotherFormField).ThenInclude(zz=>zz.FieldChoices)              
                .Where(m => m.FormFieldId == id).FirstOrDefaultAsync();

            await db.Entry(ff).Collection(m => m.HiddenFieldChoices).LoadAsync();

            List<BigFieldChoice> bfcs = new List<BigFieldChoice>();
            foreach (FieldChoice fc in ff.FieldChoices)
            {
                BigFieldChoice bfc = new BigFieldChoice() { FieldChoice = fc, isHidden = true };
                if (ff.HiddenFieldChoices.Where(m => m.FormField == ff && m.FieldChoice == fc).Any()) bfc.isHidden = false;
                bfcs.Add(bfc);
            }
            if (ff.PublicMotherFormField != null)
            {
                foreach (FieldChoice fc in ff.PublicMotherFormField?.FieldChoices)
                {
                    BigFieldChoice bfc = new BigFieldChoice() { FieldChoice = fc, isHidden = true };
                    if (ff.HiddenFieldChoices.Where(m => m.FormField == ff && m.FieldChoice == fc).Any()) bfc.isHidden = false;
                    bfcs.Add(bfc);
                }
            }
            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            ViewData["readonly"] = false;

            FormFormField firstFormFormField = await db.FormsFormFields.
                    Include(m => m.Form).ThenInclude(cl => cl.FormChangeLogs).ThenInclude(fcl => fcl.ChangeLog).ThenInclude(xx => xx.User)
                    .Where(m => m.FormFieldId == ff.FormFieldId).FirstOrDefaultAsync();
            if ((firstFormFormField != null) && (firstFormFormField.Form.FormChangeLogs != null) && (firstFormFormField.Form.FormChangeLogs.Count > 0))
            {
                if (firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.UserId != user.UserId) ViewData["readonly"] = true;

            }

            if (User.IsInRole("DM")) ViewData["readonly"] = false;

            ViewData["FormFieldId"] = ff.FormFieldId;
            return View(bfcs);

        }

        public async Task<IActionResult> EditBigFieldsState(int id, int fieldchoice, bool state)
        {
            HiddenFieldChoice hfc = await db.HiddenFieldChoices.Where(m => m.FormField.FormFieldId == id && m.FieldChoice.FieldChoiceId == fieldchoice).FirstOrDefaultAsync();
            if ((hfc == null) && (state==false))
            {
                FormField ff = await db.FormFields.FindAsync(id);
                FieldChoice fc = await db.FieldChoices.FindAsync(fieldchoice);

                hfc = new HiddenFieldChoice() { FormField = ff, FieldChoice = fc };
                db.HiddenFieldChoices.Add(hfc);
                db.Entry(hfc).State = EntityState.Added;
            }
            else if ((hfc != null) && (state==true))
            {
                db.HiddenFieldChoices.Remove(hfc);
                db.Entry(hfc).State = EntityState.Deleted;
            }

            await db.SaveChangesAsync();
            return Content("OK");


        }

        public async Task<IActionResult> EditBigFieldsText(int id, int fieldchoice, string text)
        {
            
            FieldChoice fc = await db.FieldChoices.FindAsync(fieldchoice);

            fc.Text = text;
            db.Entry(fc).State = EntityState.Modified;
            

            await db.SaveChangesAsync();
            return Content("OK");
        }

        public async Task<IActionResult> EditBigFieldsOrder(int id, int fieldchoice, int order)
        {

            FieldChoice fc = await db.FieldChoices.FindAsync(fieldchoice);

            fc.Order = order;
            db.Entry(fc).State = EntityState.Modified;


            await db.SaveChangesAsync();
            return Content("OK");
        }

        public async Task<IActionResult> EditBigFieldsRemove(int id, int fieldchoice)
        {

            FieldChoice fc = await db.FieldChoices.Include(m=>m.HiddenFieldChoices).Where(m=>m.FieldChoiceId==fieldchoice).FirstOrDefaultAsync();

            HiddenFieldChoice hfc = await db.HiddenFieldChoices.Where(m => m.FormField.FormFieldId == id && m.FieldChoice.FieldChoiceId == fieldchoice).FirstOrDefaultAsync();
            if (hfc != null) 
            {
                fc.HiddenFieldChoices.Remove(hfc);
                db.HiddenFieldChoices.Remove(hfc);
                db.Entry(hfc).State = EntityState.Deleted;
            } 
            db.FieldChoices.Remove(fc);
            db.Entry(fc).State = EntityState.Deleted;


            await db.SaveChangesAsync();
            return RedirectToAction("EditBigFields", new { @id = id });
        }

        public async Task<IActionResult> EditBigFieldsAdd(int id, string text)
        {
            FormField ff = await db.FormFields.FindAsync(id);
            FieldChoice fc = new FieldChoice() { FormField = ff, Text = text };
            if (ff.FieldChoices != null) fc.Order = ff.FieldChoices.Count();
            else ff.FieldChoices = new List<FieldChoice>();

            ff.FieldChoices.Add(fc);

            db.Entry(ff).State = EntityState.Modified;


            await db.SaveChangesAsync();
            return RedirectToAction("EditBigFields", new { @id = id });
        }

        private async Task<List<LookupTableViewModel>> CreateLookupTableViewModel()
        {
            List<FormField> formFields = await db.FormFields.Include(m => m.FormFieldForms).ThenInclude(m => m.Form).ToListAsync();

            List<LookupTableViewModel> ltvms = new List<LookupTableViewModel>();
            foreach (FormField ff in formFields)
            {
                LookupTableViewModel ltvm = new LookupTableViewModel() { FormField = ff };
                ltvm.UsedInForms = new List<Form>();
                ltvm.UsedInForms.AddRange(ff.FormFieldForms?.Select(m => m.Form));

                // formfield is public mother, so add all children formfields-forms
                if ((ff.Public == true) && (ff.PublicMotherFormFieldFormFieldId == null))
                {
                    List<FormField> children = await db.FormFields.Where(m => m.PublicMotherFormFieldFormFieldId == ff.FormFieldId).ToListAsync();
                    foreach (FormField child in children)
                    {
                        ltvm.UsedInForms.AddRange(child.FormFieldForms.Select(m => m.Form));
                    }
                }

                ltvms.Add(ltvm);
            }

            return ltvms;
        }


        /// <summary>
        /// Shows a table with all form fields and Id's for the wfs
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> LookupTable()
        {
            return View(await CreateLookupTableViewModel());
        }

        /// <summary>
        /// Exports a QGIS QML Attribute table with the alias for all wfs fields
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ExportQGISQmlAttributeTable()
        {
            List<LookupTableViewModel> ltvms = await CreateLookupTableViewModel();

            var stream = new MemoryStream();
            using (var writeFile = new StreamWriter(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writeFile.WriteLine("<!DOCTYPE qgis PUBLIC 'http://mrcc.com/qgis.dtd' 'SYSTEM'>");
                writeFile.WriteLine("<qgis version=\"3.16.4-Hannover\" styleCategories=\"Fields\">");
                writeFile.WriteLine("  <aliases>");
                writeFile.WriteLine("    <alias field=\"bdcguid_projekt\" index=\"0\" name=\"\"/>");
                writeFile.WriteLine("    <alias field=\"projekt_id_extern\" index=\"1\" name=\"\"/>");
                writeFile.WriteLine("    <alias field=\"projektname\" index=\"2\" name=\"\"/>");
                writeFile.WriteLine("    <alias field=\"bdcguid_geometrie\" index=\"3\" name=\"\"/>");
                writeFile.WriteLine("    <alias field=\"geometriename\" index=\"4\" name=\"\"/>");
                writeFile.WriteLine("    <alias field=\"bdcguid_beobachtung\" index=\"5\" name=\"\"/>");

                int index = 6;

                foreach (LookupTableViewModel ltvm in ltvms)
                {
                    string id = "";
                    if ((ltvm.FormField.Public == true) && (ltvm.FormField.PublicMotherFormField == null))
                    {
                        id = "a_" + ltvm.FormField.FormFieldId;
                    }
                    else if (ltvm.FormField.Public == false)
                    {
                        id = "f_" + ltvm.FormField.FormFieldId;
                    }


                    writeFile.WriteLine("    <alias field=\"" + id + "\" index=\"" + index + "\" name=\"" + ltvm.FormField.Title.Replace("<br>","").Replace("<","").Replace(">","") + "\"/>");
                    index++;

                }
                writeFile.WriteLine("  </aliases>");
                writeFile.WriteLine("</qgis>");

            }
            stream.Position = 0; //reset stream
            return File(stream, "application/octet-stream", "style.qml");

        }

        public async Task<IActionResult> Create(string name)
        {
            if ((!User.IsInRole("DM")) && (!User.IsInRole("PL")) && (!User.IsInRole("PK"))) return RedirectToAction("NotAllowed", "Home");
            Form f = new Form() { Title = name };
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            ChangeLog cl = new ChangeLog() { Log = "Created new Form " + name, User = user };
            ChangeLogForm clf = new ChangeLogForm() { ChangeLog = cl, Form = f };
            db.ChangeLogs.Add(cl);
            db.ChangeLogsForms.Add(clf);
            db.Forms.Add(f);

            await db.SaveChangesAsync();

            return Content(f.FormId.ToString());
        }

        public async Task<IActionResult> Copy(int id, string name)
        {
            if ((!User.IsInRole("PK")) && (!User.IsInRole("PL")) && (!User.IsInRole("DM"))) return RedirectToAction("NotAllowed", "Home");

            Form origForm = await db.Forms.Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(f => f.FieldChoices)
                .Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(f => f.HiddenFieldChoices)
                .Include(mother => mother.FormFormFields).ThenInclude(mo => mo.FormField).ThenInclude(mo => mo.PublicMotherFormField).ThenInclude(mo => mo.FieldChoices)
                .Where(m => m.FormId == id).FirstOrDefaultAsync();
            if (origForm == null) return StatusCode(500);


            Form f = new Form() { Title = name };
            f.FormFormFields = new List<FormFormField>();
            foreach (FormFormField fff in origForm.FormFormFields)
            {
                if (fff.FormField.Public)
                {
                    FormField origMotherFormField = fff.FormField;
                    if (fff.FormField.PublicMotherFormField != null) origMotherFormField = fff.FormField.PublicMotherFormField;
                    FormField ff2 = new FormField()
                    {
                        Description = origMotherFormField.Description,
                        FieldTypeId = origMotherFormField.FieldTypeId,
                        Mandatory = fff.FormField.Mandatory,
                        Order = fff.FormField.Order,
                        Public = origMotherFormField.Public,
                        Source = origMotherFormField.Source,
                        Title = origMotherFormField.Title,
                        UseInRecordTitle = fff.FormField.UseInRecordTitle,
                        PublicMotherFormField = origMotherFormField
                    };

                    FormFormField fff2 = new FormFormField() { Form = f, FormField = ff2 };
                    db.FormsFormFields.Add(fff2);
                    f.FormFormFields.Add(fff2);
                }
                else
                {
                    FormField ff2 = new FormField()
                    {
                        Description = fff.FormField.Description,
                        FieldTypeId = fff.FormField.FieldTypeId,
                        Mandatory = fff.FormField.Mandatory,
                        Order = fff.FormField.Order,
                        Public = fff.FormField.Public,
                        Source = fff.FormField.Source,
                        Title = fff.FormField.Title,
                        UseInRecordTitle = fff.FormField.UseInRecordTitle
                    };

                    if (fff.FormField.FieldTypeId == FieldTypeEnum.Choice)
                    {
                        ff2.FieldChoices = new List<FieldChoice>();

                        foreach (FieldChoice fc in fff.FormField.FieldChoices)
                        {
                            FieldChoice newChoice = new FieldChoice() { FormField = ff2, Order = fc.Order, Text = fc.Text };
                            db.FieldChoices.Add(newChoice);
                            ff2.FieldChoices.Add(newChoice);
                        }
                    }
                    FormFormField fff2 = new FormFormField() { Form = f, FormField = ff2 };
                    db.FormsFormFields.Add(fff2);
                    f.FormFormFields.Add(fff2);

                }
            }


            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            ChangeLog cl = new ChangeLog() { Log = "Created new Form " + name, User = user };
            ChangeLogForm clf = new ChangeLogForm() { ChangeLog = cl, Form = f };
            db.ChangeLogs.Add(cl);
            db.ChangeLogsForms.Add(clf);
            db.Forms.Add(f);

            await db.SaveChangesAsync();

            return Content(f.FormId.ToString());
        }

        public async Task<IActionResult> Delete(int id)
        {
            if ((!User.IsInRole("PK")) && (!User.IsInRole("PL")) && (!User.IsInRole("DM"))) return RedirectToAction("NotAllowed", "Home");

            Form origForm = await db.Forms.Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(f => f.FieldChoices).Where(m => m.FormId == id).FirstOrDefaultAsync();
            if (origForm == null) return StatusCode(500);

            // Delete all referenzes
            foreach (FormFormField toRemoveFFF in origForm.FormFormFields)
            {
                List<TextData> referenzeTextDatas = await db.TextData.Where(m => m.FormFieldId == toRemoveFFF.FormFieldId).ToListAsync();
                foreach (TextData td in referenzeTextDatas)
                {
                    db.TextData.Remove(td);
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                List<BooleanData> referenzeBooleanDatas = await db.BooleanData.Where(m => m.FormFieldId == toRemoveFFF.FormFieldId).ToListAsync();
                foreach (BooleanData td in referenzeBooleanDatas)
                {
                    db.BooleanData.Remove(td);
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                List<NumericData> referenzeNumericDatas = await db.NumericData.Where(m => m.FormFieldId == toRemoveFFF.FormFieldId).ToListAsync();
                foreach (NumericData td in referenzeNumericDatas)
                {
                    db.NumericData.Remove(td);
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                db.FormsFormFields.Remove(toRemoveFFF);
                db.Entry(toRemoveFFF).State = EntityState.Deleted;
            }

            List<Record> toRemoveRecords = await db.Records.Include(m => m.TextData).Include(m => m.BooleanData).Include(m => m.NumericData).Where(m => m.FormId == origForm.FormId).ToListAsync();
            foreach (Record r in toRemoveRecords)
            {
                foreach (TextData td in r.TextData)
                {
                    db.TextData.Remove(td);
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                foreach (BooleanData td in r.BooleanData)
                {
                    db.BooleanData.Remove(td);
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                foreach (NumericData td in r.NumericData)
                {
                    db.NumericData.Remove(td);
                    db.Entry(td).State = EntityState.Modified;
                }



                db.Records.Remove(r);
                db.Entry(r).State = EntityState.Deleted;
            }

            db.Forms.Remove(origForm);
            db.Entry(origForm).State = EntityState.Deleted;

            await db.SaveChangesAsync();

            return RedirectToAction("Index");


        }


        public async Task<IActionResult> GetFormsForProject(Guid? id, string search)
        {
            if (id == null) return NotFound();
            Project p = await db.Projects
                .Include(m => m.ProjectForms)
                    .ThenInclude(fg => fg.Form)
                .Where(p => p.ProjectId == id)
                .FirstOrDefaultAsync();

            List<FormsPoco> returnlist = new List<FormsPoco>();
            foreach (Form f in p.ProjectForms.Select(m => m.Form))
            {
                returnlist.Add(new FormsPoco() { FormId = f.FormId, Title = f.Title });
            }

            string json = JsonConvert.SerializeObject(returnlist);
            return Content(json, "application/json");
        }

        public async Task<IActionResult> GetFormsForProjectTransfer(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mProject = await db.Projects.Include(m => m.ProjectGroups).ThenInclude(pg => pg.Records).ThenInclude(r => r.Form)
                //.Include(m => m.GroupUsers)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (mProject == null)
            {
                return NotFound();
            }

            //DB.Models.Domain.User me = UserHelper.GetCurrentUser(User, _context);

            List<Form> forms = await db.Forms.Include(m => m.FormProjects).ToListAsync();
            List<SelectedFormsPoco> returnList = new List<SelectedFormsPoco>();
            foreach (Form f in forms)
            {
                SelectedFormsPoco sup = new SelectedFormsPoco() { myForm = f, myProject = mProject };
                if (mProject.ProjectGroups.Where(m => m.Records.Any(fo => fo.FormId == f.FormId && fo.StatusId != StatusEnum.deleted) && m.ProjectId == mProject.ProjectId).Count() > 0) sup.disabled = true;

                returnList.Add(sup);
            }

            string json = JsonConvert.SerializeObject(returnList);
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> EditFormsForProject([FromBody] Forms data)
        {
            var mProject = await db.Projects
                .Include(m => m.ProjectForms).ThenInclude(m => m.Form).ThenInclude(fo => fo.FormRecords)
                .FirstOrDefaultAsync(m => m.ProjectId == data.guid);
            if (mProject == null)
            {
                return NotFound();
            }

            List<ProjectForm> oldProjectForms = mProject.ProjectForms.ToList();
            List<Form> newForms = new List<Form>();

            foreach (FormIds formid in data.items)
            {
                Form f = await db.Forms.FindAsync(Int32.Parse(formid.value));

                if (f != null)
                {
                    if (oldProjectForms.Any(m => m.FormId == f.FormId))
                    {
                        newForms.Add(f);
                    }
                    else
                    {
                        ProjectForm pf = new ProjectForm() { Form = f, Project = mProject };
                        db.ProjectsForms.Add(pf);
                    }
                }

            }

            // delete all not used projectforms and only if there is no records or geometrie
            List<ProjectForm> toRemoveForms = oldProjectForms?.Where(m => newForms.All(m2 => m2.FormId != m.FormId)).ToList();
            foreach (ProjectForm pf in toRemoveForms)
            {
                List<Record> rs = await db.Records.Where(m => m.ProjectGroup.ProjectId == mProject.ProjectId && m.FormId == pf.FormId && m.StatusId != StatusEnum.deleted).ToListAsync();

                if (rs.Count == 0)
                {
                    mProject.ProjectForms.Remove(pf);
                    db.ProjectsForms.Remove(pf);
                }
            }

            await db.SaveChangesAsync();


            return Content("OK", "application/json");
        }




        public async Task<IActionResult> Edit(int id)
        {
            if ((!User.IsInRole("DM")) && (!User.IsInRole("PL")) && (!User.IsInRole("PK"))) return RedirectToAction("NotAllowed", "Home");

            List<Form> forms = await db.Forms
                .Include(m => m.FormChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User)
                .ToListAsync();

            List<FormPoco> fps = new List<FormPoco>();

            if (User.IsInRole("DM"))
            {
                foreach (Form fo in forms)
                {
                    FormPoco fp = new FormPoco() { Form = fo, Editable = true, Author = fo.FormChangeLogs?.First().ChangeLog?.User };
                    fps.Add(fp);
                }
            }

            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            foreach (Form fo in forms)
            {
                FormPoco fp = new FormPoco() { Form = fo, Editable = false, Author = fo.FormChangeLogs?.First().ChangeLog?.User };
                if (fo.FormChangeLogs?.First().ChangeLog?.User == user) fp.Editable = true;
                fps.Add(fp);
            }

            Form f = await db.Forms.
                Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(m=>m.FieldChoices)
                .Include(mother => mother.FormFormFields).ThenInclude(mo => mo.FormField).ThenInclude(mo => mo.PublicMotherFormField).Where(m => m.FormId == id).FirstOrDefaultAsync();
            if (f == null) return NotFound();
            if (fps.Where(m => m.Form.FormId == f.FormId && m.Editable == true).Count() == 0) return RedirectToAction("NotAllowed", "Home");


            // create list of public form fields
            List<FormField> ffs = await db.FormFields.Include(m => m.FieldChoices).Where(m => m.Public == true && m.PublicMotherFormField == null).ToListAsync();
            List<FormFieldPoco> pffps = new List<FormFieldPoco>();
            foreach (FormField ff in ffs.OrderBy(m => m.Title))
            {
                await db.Entry(ff).Collection(m => m.HiddenFieldChoices).LoadAsync();

                FormFieldPoco pffp = CreateFormFieldPocosFormField(ff);
                // get first Form -> This is the owner
                FormFormField firstFormFormField = await db.FormsFormFields.
                    Include(m => m.Form).ThenInclude(cl => cl.FormChangeLogs).ThenInclude(fcl => fcl.ChangeLog).ThenInclude(xx => xx.User)
                    .Where(m => m.FormFieldId == ff.FormFieldId).FirstOrDefaultAsync();
                if ((firstFormFormField != null) && (firstFormFormField.Form.FormChangeLogs != null) && (firstFormFormField.Form.FormChangeLogs.Count > 0))
                {
                    //pffp.label = pffp.label + " (" + firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.FirstName + " " + firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.Name + ")";
                    pffp.author = firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.FirstName + " " + firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.Name;
                    if (firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.UserId == user.UserId) pffp.isreadonly = false;
                    else pffp.isreadonly = true;
                }

                pffps.Add(pffp);
            }

            string json = JsonConvert.SerializeObject(pffps);
            ViewData["PublicFormFields"] = json;

            ViewData["HidePublicFieldsJSCode"] = "if ((author == null) || ((author.value != myauthor ) && (isPublic.attributes.checked))) {var addOption = fld.querySelector(\".add-opt\");addOption.style.display = \"none\";for (i = 0; i < optionRemoveFields.length; i++) {optionRemoveFields[i].style.display = \"none\";}}";

            ViewData["MyAuthor"] = user.FirstName + " " + user.Name;

            return View(f);
        }

        private FormFieldPoco CreateFormFieldPocosFormField(FormField ff, FormField origFormField = null)
        {
            if (ff.FieldTypeId == FieldTypeEnum.Text)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "text", label = ff.Title, name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle, ispublic = ff.Public, value = ff.StandardValue };
                return ffp;
            }
            else if (ff.FieldTypeId == FieldTypeEnum.DateTime)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "date", label = ff.Title, name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle, ispublic = ff.Public };
                return ffp;
            }
            else if (ff.FieldTypeId == FieldTypeEnum.Number)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "number", label = ff.Title, name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle,/* ispublic = ff.Public*/ };
                return ffp;
            }
            else if (ff.FieldTypeId == FieldTypeEnum.Binary)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "file", label = ff.Title, name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle,/* ispublic = ff.Public*/ };
                return ffp;
            }
            else if (ff.FieldTypeId == FieldTypeEnum.Choice)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "select", label = ff.Title, name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle, ispublic = ff.Public };

                List<OptionsPoco> values = new List<OptionsPoco>();

                if (ff.FieldChoices.Count < 50)
                {

                    foreach (FieldChoice fc in ff.FieldChoices.OrderBy(m => m.Order))
                    {
                        OptionsPoco op = new OptionsPoco() { label = fc.Text, value = fc.FieldChoiceId.ToString() };
                        if ((origFormField != null))
                        {
                            if (!origFormField.HiddenFieldChoices.Where(m => m.FormField == origFormField && m.FieldChoice == fc).Any()) op.visible = true;
                            else op.visible = false;
                        }
                        else
                        {
                            if (!ff.HiddenFieldChoices.Where(m => m.FormField == origFormField && m.FieldChoice == fc).Any()) op.visible = true;
                            else op.visible = false;
                        }
                        values.Add(op);
                    }

                }
                else
                {
                    OptionsPoco op = new OptionsPoco() { label = "Zu viele Einträge in Liste. Bitte separat editieren", value = "0", visible=true };
                    values.Add(op);
                }

                ffp.values = values;
                return ffp;
            }
            else if (ff.FieldTypeId == FieldTypeEnum.Boolean)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "checkbox-group", name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle, ispublic = ff.Public };
                List<OptionsPoco> values = new List<OptionsPoco>();
                OptionsPoco op = new OptionsPoco() { label = ff.Title, value = ff.FormFieldId.ToString() };
                values.Add(op);

                ffp.values = values;

                return ffp;
            }
            else if (ff.FieldTypeId == FieldTypeEnum.Header)
            {
                FormFieldPoco ffp = new FormFieldPoco() { type = "header", label = ff.Title, name = ff.FormFieldId.ToString(), description = ff.Description, source = ff.Source, mandatory = ff.Mandatory, useinrecordtitle = ff.UseInRecordTitle, ispublic = ff.Public };
                return ffp;
            }
            return null;

        }

        public async Task<IActionResult> CreateFormBuilderJson(int id)
        {
            Form form = await db.Forms.Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField)
                .Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField)
                .Include(m=>m.FormChangeLogs).ThenInclude(m=>m.ChangeLog).ThenInclude(m=>m.User)
                .Include(mother => mother.FormFormFields).ThenInclude(mo => mo.FormField).ThenInclude(mo=>mo.PublicMotherFormField)
                .Where(m => m.FormId == id).FirstOrDefaultAsync();
            if (form == null) return NotFound();

            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            List<FormFieldPoco> ffps = new List<FormFieldPoco>();
            foreach (FormField ff in form.FormFormFields.Select(fff => fff.FormField).OrderBy(m => m.Order))
            {
                if (ff.FieldTypeId == FieldTypeEnum.Choice)
                {
                    await db.Entry(ff).Collection(m => m.HiddenFieldChoices).LoadAsync();
                    await db.Entry(ff).Collection(m => m.FieldChoices).LoadAsync();
                }

                if (ff.PublicMotherFormField != null)
                {
                    if (ff.FieldTypeId == FieldTypeEnum.Choice)
                    {
                        await db.Entry(ff.PublicMotherFormField).Collection(m => m.FieldChoices).LoadAsync();
                    }
                    FormFieldPoco ffp = CreateFormFieldPocosFormField(ff.PublicMotherFormField, ff);
                    ffp.name = ff.FormFieldId.ToString();
                    ffp.mandatory = ff.Mandatory;
                    ffp.useinrecordtitle = ff.UseInRecordTitle;

                    // Check if user is author of public form field
                    FormFormField firstFormFormField = await db.FormsFormFields.
                    Include(m => m.Form).ThenInclude(cl => cl.FormChangeLogs).ThenInclude(fcl => fcl.ChangeLog).ThenInclude(xx => xx.User)
                    .Where(m => m.FormFieldId == ff.PublicMotherFormFieldFormFieldId).FirstOrDefaultAsync();
                    if ((firstFormFormField != null) && (firstFormFormField.Form.FormChangeLogs != null) && (firstFormFormField.Form.FormChangeLogs.Count > 0))
                    {
                        ffp.author = firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.FirstName + " " + firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.Name;
                        if (firstFormFormField.Form.FormChangeLogs[0].ChangeLog.User.UserId == user.UserId) ffp.isreadonly = false;
                        else ffp.isreadonly = true;
                    }


                    ffps.Add(ffp);

                }
                else
                {
                    FormFieldPoco ffp = CreateFormFieldPocosFormField(ff);

                    FormFormField fff = ff.FormFieldForms.First();

                    if ((fff.Form.FormChangeLogs != null) && (fff.Form.FormChangeLogs.Count > 0))
                    {
                        ffp.author = fff.Form.FormChangeLogs[0].ChangeLog.User.FirstName + " " + fff.Form.FormChangeLogs[0].ChangeLog.User.Name;
                        if (fff.Form.FormChangeLogs[0].ChangeLog.User.UserId == user.UserId) ffp.isreadonly = false;
                        else ffp.isreadonly = true;
                    }

                    ffps.Add(ffp);
                }
            }

            string json = JsonConvert.SerializeObject(ffps);
            return Content(json, "application/json");
        }



        [HttpPost]
        public async Task<IActionResult> SaveFormBuilderJson([FromBody] FormBuilderJson data)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            Form form = await db.Forms.
                Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(m => m.FieldChoices)
                .Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(m => m.HiddenFieldChoices)
                .Include(mother => mother.FormFormFields).ThenInclude(mo => mo.FormField).ThenInclude(mo => mo.PublicMotherFormField).ThenInclude(mo => mo.FieldChoices).Where(m => m.FormId == data.id).FirstOrDefaultAsync();
            if (form == null) return NotFound();

            if (form.Title != data.title)
            {
                form.Title = data.title;
                db.Entry(form).State = EntityState.Modified;
            }

            List<FormField> usedFormFields = new List<FormField>();

            int order = 0;
            foreach (FormFieldPoco ffp in data.json)
            {
                int ffNumber = 0;
                FormField ff = new FormField();
                if (Int32.TryParse(ffp.name, out ffNumber))
                {
                    ff = form.FormFormFields.Select(fff => fff.FormField).Where(m => m.FormFieldId == ffNumber).FirstOrDefault();
                    if (ff == null)
                    {
                        // check if it is a public formfield
                        ff = await db.FormFields.Include(m => m.FieldChoices).Where(m => m.FormFieldId == ffNumber).FirstOrDefaultAsync();
                        if (ff != null)
                        {
                            // It is a public form -> make a new Entry with a reference to the public original ff
                            FormField ffPublicCopy = CreateFormFieldFromPoco(ffp, form, order);
                            ffPublicCopy.PublicMotherFormField = ff;
                            usedFormFields.Add(ffPublicCopy);
                            form.FormFormFields.Add(new FormFormField() { FormField = ffPublicCopy, Form = form });
                            db.Entry(ffPublicCopy).State = EntityState.Added;
                        }
                        else
                        {
                            ff = CreateFormFieldFromPoco(ffp, form, order);
                            form.FormFormFields.Add(new FormFormField() { FormField = ff, Form = form });
                            db.Entry(ff).State = EntityState.Added;
                        }
                    }
                    else
                    {
                        if ((ffp.label != null) && (ffp.label != "")) ff.Title = ffp.label;
                        if (ffp.type == "checkbox-group") ff.Title = ffp.values[0].label;
                        ff.Order = order;
                        ff.Description = ffp.description;
                        ff.Source = ffp.source;
                        ff.Mandatory = ffp.mandatory;
                        ff.UseInRecordTitle = ffp.useinrecordtitle;
                        ff.Public = ffp.ispublic;
                        ff.StandardValue = ffp.value;
                        db.Entry(ff).State = EntityState.Modified;
                    }
                }
                else
                {
                    ff = CreateFormFieldFromPoco(ffp, form, order);
                    form.FormFormFields.Add(new FormFormField() { FormField = ff, Form = form });
                    db.Entry(ff).State = EntityState.Added;
                }

                await db.SaveChangesAsync();


                /// FieldChoices
                if (ffp.values != null)
                {
                    int i = 0;

                    List<FieldChoice> usedFieldChoices = new List<FieldChoice>();

                    // change FieldChoices for FormField or, if there is a mother, change it for the mother
                    FormField origFormField = ff;
                    if (ff.PublicMotherFormField != null) origFormField = ff.PublicMotherFormField;

                    if (origFormField.FieldChoices == null) origFormField.FieldChoices = new List<FieldChoice>();

                    if (origFormField.FieldChoices.Count < 50)
                    {
                        foreach (OptionsPoco op in ffp.values)
                        {
                            try
                            {
                                FieldChoice alreadyFieldChoiceExist = origFormField.FieldChoices?.Where(m => m.Text == op.label).FirstOrDefault();
                                if (alreadyFieldChoiceExist != null)
                                {
                                    if (alreadyFieldChoiceExist.Order != i) alreadyFieldChoiceExist.Order = i;
                                    if (alreadyFieldChoiceExist.Text != op.label) alreadyFieldChoiceExist.Text = op.label;

                                    usedFieldChoices.Add(alreadyFieldChoiceExist);
                                    db.Entry(alreadyFieldChoiceExist).State = EntityState.Modified;

                                    // set visibility: visible -> no entry in hidden
                                    if (op.visible && ff.HiddenFieldChoices.Where(m => m.FieldChoice == alreadyFieldChoiceExist && m.FormField == ff).Any())
                                        db.HiddenFieldChoices.RemoveRange(ff.HiddenFieldChoices.Where(m => m.FieldChoice == alreadyFieldChoiceExist && m.FormField == ff));
                                    else if (!op.visible && !ff.HiddenFieldChoices.Where(m => m.FieldChoice == alreadyFieldChoiceExist && m.FormField == ff).Any())
                                    {
                                        HiddenFieldChoice hfc = new HiddenFieldChoice() { FieldChoice = alreadyFieldChoiceExist, FormField = ff };
                                        db.HiddenFieldChoices.Add(hfc);
                                    }
                                }
                                else
                                {
                                    FieldChoice newFieldChoice = new FieldChoice() { Text = op.label, FormField = ff, Order = i };
                                    if (ff.FieldChoices == null) origFormField.FieldChoices = new List<FieldChoice>();
                                    ff.FieldChoices.Add(newFieldChoice);
                                    db.FieldChoices.Add(newFieldChoice);
                                    usedFieldChoices.Add(newFieldChoice);
                                    db.Entry(newFieldChoice).State = EntityState.Added;

                                    // set visibility: visible -> no entry in hidden
                                    
                                    // disable for the first entry
                                    
                                    /*if (!op.visible)
                                    {
                                        HiddenFieldChoice hfc = new HiddenFieldChoice() { FieldChoice = newFieldChoice, FormField = ff };
                                        db.HiddenFieldChoices.Add(hfc);
                                    }*/

                                }
                            }
                            catch (Exception e)
                            {
                                // could not parse value
                            }
                            i++;

                        }

                        await db.SaveChangesAsync();
                        // delete not used FieldChoices
                        List<FieldChoice> toRemoveFieldChoices = origFormField.FieldChoices?.Where(m => usedFieldChoices.All(m2 => m2.FieldChoiceId != m.FieldChoiceId)).ToList();
                        foreach (FieldChoice toRemoveFC in toRemoveFieldChoices)
                        {
                            // delete all references in textdata
                            List<TextData> referenzeTextDatas = await db.TextData.Where(m => m.FieldChoiceId == toRemoveFC.FieldChoiceId).ToListAsync();
                            foreach (TextData td in referenzeTextDatas)
                            {
                                td.FieldChoice = null;
                                db.Entry(td).State = EntityState.Modified;
                            }


                            db.FieldChoices.Remove(toRemoveFC);
                            db.Entry(toRemoveFC).State = EntityState.Deleted;
                        }
                    }

                }


                await db.SaveChangesAsync();

                usedFormFields.Add(ff);
                order++;
            }
            await db.SaveChangesAsync();
            // delete not used FormFields
            List<FormFormField> toRemoveFormFields = form.FormFormFields.Where(m => usedFormFields.All(m2 => m2.FormFieldId != m.FormFieldId)).ToList();
            foreach (FormFormField toRemoveFFF in toRemoveFormFields)
            {
                List<TextData> referenzeTextDatas = await db.TextData.Where(m => m.FormFieldId == toRemoveFFF.FormFieldId).ToListAsync();
                foreach (TextData td in referenzeTextDatas)
                {
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                List<BooleanData> referenzeBooleanDatas = await db.BooleanData.Where(m => m.FormFieldId == toRemoveFFF.FormFieldId).ToListAsync();
                foreach (BooleanData td in referenzeBooleanDatas)
                {
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                List<NumericData> referenzeNumericDatas = await db.NumericData.Where(m => m.FormFieldId == toRemoveFFF.FormFieldId).ToListAsync();
                foreach (NumericData td in referenzeNumericDatas)
                {
                    td.FormField = null;
                    db.Entry(td).State = EntityState.Modified;
                }

                db.FormsFormFields.Remove(toRemoveFFF);
                db.Entry(toRemoveFFF).State = EntityState.Deleted;
            }


            ChangeLog cl = new ChangeLog() { Log = "changed form", User = user };
            ChangeLogForm clf = new ChangeLogForm() { ChangeLog = cl, Form = form };
            db.ChangeLogsForms.Add(clf);
            db.ChangeLogs.Add(cl);
            db.Entry(form).State = EntityState.Modified;

            await db.SaveChangesAsync();
            return Json("OK");
        }


        public static async Task CreateViews(BioDivContext db, List<Form> forms, bool withOgd = true, string prefix = "wfs", bool withNumber = false, int maxLength=255)
        {
            string sqlCreateViews1 = "CREATE OR REPLACE VIEW public.{prefix}_{ogd}{geometry}_view " +
                                        "AS SELECT p.projectid as \"bdcguid_projekt\", p.id_extern as \"projekt_id_extern\", " +
                                        "p.projectname as \"projektname\", " +
                                        "p.description AS \"beschreibung\", " +
                                        "p2.description AS \"status\", " +
                                        "thirdparty.name as \"extprogramme\"," +
                                        "g.geometryid as \"bdcguid_geometrie\", " +
                                        "g.geometryname as \"geometriename\", " +
                                        "g.{geometry}, " +
                                        "case when r2.recordid is null then uuid_generate_v4() else r2.recordid end AS \"bdcguid_beobachtung\", " +
                                        "r2.groupid as \"group_id\", "+
                                        "getrecordchangelogdate(r2.recordid) as changedate, " +
                                        "getrecordchangeloguser(r2.recordid) as changeuser, ";
            sqlCreateViews1 = sqlCreateViews1.Replace("{prefix}", prefix);

            if (prefix == "wfs") prefix = "";
            else prefix += "_";

            string sqlCreateViewsGeneral = "CREATE OR REPLACE VIEW public.{prefix}records_without_geometries " +
                                        "AS SELECT p.projectid as \"bdcguid_projekt\", p.id_extern as \"projekt_id_extern\", " +
                                        "p.projectname as \"projektname\", " +
                                        "p.description AS \"beschreibung\", " +
                                        "p2.description AS \"status\", " +
                                        "thirdparty.name as \"extprogramme\"," +
                                        "r2.geometryid as \"bdcguid_geometrie\", " +
                                        "NULL as \"geometriename\", " +
                                        "'POINT EMPTY'::geometry, " +
                                        "r2.recordid as \"bdcguid_beobachtung\", " +
                                        "r2.groupid as \"group_id\", " +
                                        "getrecordchangelogdate(r2.recordid) as changedate, " +
                                        "getrecordchangeloguser(r2.recordid) as changeuser, ";
                                        
            sqlCreateViewsGeneral = sqlCreateViewsGeneral.Replace("{prefix}", prefix);

            string sqlCreateViews2 = "FROM projects p " +
                                        "INNER JOIN projectstatuses p2 on p2.id = p.projectstatusid " +
                                        "LEFT JOIN geometries g ON g.projectid = p.projectid " +
                                        "LEFT JOIN (select * from records where statusid <> 3) r2 ON r2.geometryid = g.geometryid " +
                                        "LEFT JOIN (select p.projectid, string_agg(name::text, ';') as name from projectsthirdpartytools p inner join thirdpartytools t ON (t.thirdpartytoolid = p.thirdpartytoolid) group by p.projectid) as thirdparty ON thirdparty.projectid = p.projectid " +
                                        "WHERE p.statusid <> 3 AND g.statusid <> 3 AND g.{geometry} IS NOT NULL {ogd_true}" +
                                        "ORDER BY g.geometryname; ";

            string sqlCreateViewsGeneral2 = "FROM projects p " +
                                        "INNER JOIN projectstatuses p2 on p2.id = p.projectstatusid " +
                                        "LEFT JOIN (select * from records where statusid <> 3) r2 ON r2.projectid = p.projectid " +
                                        "LEFT JOIN (select p.projectid, string_agg(name::text, ';') as name from projectsthirdpartytools p inner join thirdpartytools t ON (t.thirdpartytoolid = p.thirdpartytoolid) group by p.projectid) as thirdparty ON thirdparty.projectid = p.projectid " +
                                        "WHERE p.statusid <> 3 and r2.groupid is not null {ogd_true};";

            string sqlGeometrieView = sqlCreateViews1;
            string sqlGeneralView = sqlCreateViewsGeneral;


            List<FormFieldNamesForView> ffnfvs = new List<FormFieldNamesForView>();
            foreach (Form f in forms)
            {
                foreach (FormFormField fff in f.FormFormFields)
                {
                    FormFieldNamesForView ffnfv = new FormFieldNamesForView() { Form = f, FormField = fff.FormField };
                    ffnfv.MaxLength = maxLength;
                    List<FormFieldNamesForView> sameNames = ffnfvs.Where(m => m.FormField.Title == ffnfv.FormField.Title).ToList();
                    if ((sameNames.Count > 0) && (withNumber==false))
                    {
                        foreach (FormFieldNamesForView same in sameNames)
                        {
                            same.isGeneral = true;
                        }

                        ffnfv.isGeneral = true;
                        //int max = sameNames.Select(m => m.number).Max();
                        //ffnfv.number = max + 1;
                    }
                    // Super Short Version - only id's. Handle Allgemein too
                    else if ((sameNames.Count > 0) && (maxLength == -1))
                    {
                        foreach (FormFieldNamesForView same in sameNames)
                        {
                            same.isGeneral = true;
                        }

                        ffnfv.isGeneral = true;
                        //int max = sameNames.Select(m => m.number).Max();
                        //ffnfv.number = max + 1;
                    }
                    else ffnfvs.Add(ffnfv);

                }
            }

            // first all general fields
            foreach (FormFieldNamesForView ffnfv in ffnfvs.Where(m => m.isGeneral == true))
            {

                if ((ffnfv.FormField.FieldTypeId == FieldTypeEnum.Choice) || (ffnfv.FormField.FieldTypeId == FieldTypeEnum.Text) || (ffnfv.FormField.FieldTypeId == FieldTypeEnum.DateTime))
                {
                    sqlGeometrieView += "getrecordtext(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                    sqlGeneralView += "getrecordtext(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                }
                else if (ffnfv.FormField.FieldTypeId == FieldTypeEnum.Boolean)
                {
                    sqlGeometrieView += "getrecordboolean(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                    sqlGeneralView += "getrecordboolean(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                }
                else if (ffnfv.FormField.FieldTypeId == FieldTypeEnum.Number)
                {
                    sqlGeometrieView += "getrecordnumber(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                    sqlGeneralView += "getrecordnumber(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                }
                else if (ffnfv.FormField.FieldTypeId == FieldTypeEnum.Number)
                {
                    sqlGeometrieView += "getrecordbinary(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                    sqlGeneralView += "getrecordbinary(r2.recordid, '" + ffnfv.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                }
            }


            // then form specific fieldd
            foreach (Form f in forms)
            {
                foreach (FormFormField fff in f.FormFormFields)
                {
                    FormFieldNamesForView ffnfv = ffnfvs.Where(m => m.Form == f && m.FormField == fff.FormField).FirstOrDefault();
                    if ((ffnfv != null) && (ffnfv.isGeneral == false))
                    {
                        if ((fff.FormField.FieldTypeId == FieldTypeEnum.Choice) || (fff.FormField.FieldTypeId == FieldTypeEnum.Text) || (fff.FormField.FieldTypeId == FieldTypeEnum.DateTime))
                        {
                            sqlGeometrieView += "getrecordtext(r2.recordid, '" + f.Title.Replace("'","''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                            sqlGeneralView += "getrecordtext(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                        }
                        else if (fff.FormField.FieldTypeId == FieldTypeEnum.Boolean)
                        {
                            sqlGeometrieView += "getrecordboolean(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                            sqlGeneralView += "getrecordboolean(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                        }
                        else if (fff.FormField.FieldTypeId == FieldTypeEnum.Number)
                        {
                            sqlGeometrieView += "getrecordnumber(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                            sqlGeneralView += "getrecordnumber(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                        }
                        else if (fff.FormField.FieldTypeId == FieldTypeEnum.Binary)
                        {
                            sqlGeometrieView += "getrecordbinary(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                            sqlGeneralView += "getrecordbinary(r2.recordid, '" + f.Title.Replace("'", "''") + "'::text, '" + fff.FormField.Title.Replace("'", "''") + "'::text) AS \"" + (withNumber ? ffnfv.ShortNameWithId : ffnfv.ShortName) + "\", ";
                        }
                    }
                }
            }
            // remove last ,
            sqlGeometrieView = sqlGeometrieView.Substring(0, sqlGeometrieView.Length - 2);
            sqlGeneralView = sqlGeneralView.Substring(0, sqlGeneralView.Length - 2);

            string ogdGeneralView = sqlGeometrieView + sqlCreateViews2.Replace("{ogd_true}", " AND p.ogd = true ");
            sqlGeometrieView += sqlCreateViews2.Replace("{ogd_true}", "");
            sqlGeneralView += sqlCreateViewsGeneral2.Replace("{ogd_true}", "");

            string sqlPointView = sqlGeometrieView.Replace("{geometry}", "point").Replace("{ogd}", "");
            string sqlLineView = sqlGeometrieView.Replace("{geometry}", "line").Replace("{ogd}", "");
            string sqlPolygonView = sqlGeometrieView.Replace("{geometry}", "polygon").Replace("{ogd}", "");

            string sqlOGDPointView = ogdGeneralView.Replace("{geometry}", "point").Replace("{ogd}", "ogd_");
            string sqlOGDLineView = ogdGeneralView.Replace("{geometry}", "line").Replace("{ogd}", "ogd_");
            string sqlOGDPolygonView = ogdGeneralView.Replace("{geometry}", "polygon").Replace("{ogd}", "ogd_");

            // first delete views, becaus we may have different columns

            // add the views for each geometry type
            await db.Database.ExecuteSqlRawAsync(sqlPointView);
            await db.Database.ExecuteSqlRawAsync(sqlLineView);
            await db.Database.ExecuteSqlRawAsync(sqlPolygonView);

            if (withOgd)
            {
                await db.Database.ExecuteSqlRawAsync(sqlOGDPointView);
                await db.Database.ExecuteSqlRawAsync(sqlOGDLineView);
                await db.Database.ExecuteSqlRawAsync(sqlOGDPolygonView);
            }
            await db.Database.ExecuteSqlRawAsync(sqlGeneralView);
            if (prefix != "")
            {
                string unionquery = "CREATE OR REPLACE VIEW public.{prefix}union_records " +
                                            "AS select * from {prefix}records_without_geometries where bdcguid_geometrie is null " +
                                            "union all select * from {prefix}point_view " +
                                            "union all select * from {prefix}line_view " +
                                            "union all select * from {prefix}polygon_view ";
                unionquery = unionquery.Replace("{prefix}", prefix);
                await db.Database.ExecuteSqlRawAsync(unionquery);
            }
        }


        // Regenerates the Views for the WFS Service
        public async Task<IActionResult> ResetWFSLayers()
        {
            List<Form> forms = await db.Forms.Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(m => m.FieldChoices).ToListAsync();
            
            // first delete views, becaus we may have different columns
            await db.Database.ExecuteSqlRawAsync("DROP VIEW IF EXISTS wfs_point_view; DROP VIEW IF EXISTS wfs_line_view; DROP VIEW IF EXISTS wfs_polygon_view;");
            await db.Database.ExecuteSqlRawAsync("DROP VIEW IF EXISTS wfs_ogd_point_view; DROP VIEW IF EXISTS wfs_ogd_line_view; DROP VIEW IF EXISTS wfs_ogd_polygon_view;");
            await db.Database.ExecuteSqlRawAsync("DROP VIEW IF EXISTS records_without_geometries;");

            //await CreateViews(db, forms, true);
            await CreateViews(db, forms, true, "wfs", true, -1);

            // refresh the geoserver
            var client = new RestClient(Configuration["Environment:Geoserver"] + "rest/reset");
            client.Timeout = -1;
            var request = new RestRequest(Method.PUT);
            // TODO: Move Geoserver-Auth to config
            request.AddHeader("Authorization", "Basic " + Configuration["Environment:GeoserverAuth"]);
            request.AddParameter("text/plain", "", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return Content("OK");

        }


        private FormField CreateFormFieldFromPoco(FormFieldPoco ffp, Form form, int order)
        {
            FormField ff = new FormField();
            ff.FormFieldForms = new List<FormFormField>();
            if (ffp.type == "checkbox-group") ff.Title = Regex.Replace(ffp.values[0].label, "<.*?>", String.Empty);
            else ff.Title = Regex.Replace(ffp.label, "<.*?>", String.Empty);
            ff.Order = order;
            ff.Source = ffp.source;
            ff.Mandatory = ffp.mandatory;
            ff.UseInRecordTitle = ffp.useinrecordtitle;
            ff.Public = ffp.ispublic;
            ff.Description = ffp.description;
            ff.FormFieldForms.Add(new FormFormField() { Form = form, FormField = ff });
            if (ffp.type == "text")
            {
                ff.FieldTypeId = FieldTypeEnum.Text;
                ff.StandardValue = ffp.value;
            }
            else if (ffp.type == "date")
            {
                ff.FieldTypeId = FieldTypeEnum.DateTime;
            }
            else if (ffp.type == "select")
            {
                ff.FieldTypeId = FieldTypeEnum.Choice;
            }
            else if (ffp.type == "file")
            {
                ff.FieldTypeId = FieldTypeEnum.Binary;
            }
            else if (ffp.type == "checkbox-group")
            {
                ff.FieldTypeId = FieldTypeEnum.Boolean;
            }
            else if (ffp.type == "number")
            {
                ff.FieldTypeId = FieldTypeEnum.Number;
            }
            else if (ffp.type == "header")
            {
                ff.FieldTypeId = FieldTypeEnum.Header;
            }
            return ff;
        }


        public async Task<IActionResult> Export()
        {
            List<Dictionary<int, string>> returnList = new List<Dictionary<int, string>>();

            List<Form> forms = await db.Forms.Include(m => m.FormProjects).ThenInclude(fp => fp.Project)
                .Include(m => m.FormFormFields).ThenInclude(fff => fff.FormField)
                .Include(m => m.FormChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User)
                .ToListAsync();

            List<Form> fps = new List<Form>();

            if (User.IsInRole("DM"))
            {
                foreach (Form f in forms)
                {
                    fps.Add(f);
                }
            }
            else
            {
                User user = Helpers.UserHelper.GetCurrentUser(User, db);
                foreach (Form f in forms)
                {
                    if (f.FormChangeLogs?.First().ChangeLog?.User == user) fps.Add(f);
                }
            }



            List<FormField> ffs = db.FormFields.Include(m=>m.FormFieldForms).ThenInclude(m=>m.Form).Where(m=>m.FieldTypeId == FieldTypeEnum.Choice).ToList();
            foreach (FormField ff in ffs)
            {
                if (ff.FormFieldForms.Where(m => fps.Contains(m.Form)).Any())
                {
                    Dictionary<int, string> keyValuePairs = new Dictionary<int, string>() { { ff.FormFieldId, ff.Title + "(" + ff.FormFieldForms.FirstOrDefault()?.Form.Title + ")" } };
                    returnList.Add(keyValuePairs);
                }
            }

            return View(returnList);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Export(string fieldchoice)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, this.db);
            
            string exportf = Path.GetRandomFileName();
            string[] fname = exportf.Split(".");

            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            if (!Directory.Exists(dataDir + "//Export")) Directory.CreateDirectory(dataDir + "//Export");
            string exportfilename = dataDir + "//Export//" + fname[0] + ".csv";


            ProcessStartInfo psi = new ProcessStartInfo();
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

            string sql_command = @"
select fc2.fieldchoiceid, ff.formfieldid, 'nein' as kanntexteditieren, fc2.\""text\"", fc2.\""order\"" reihenfolge, case when h.hiddenfieldchoiceid is not null then 0 else 1 end sichtbar from formfields ff 
left outer join formfields mother on (ff.publicmotherformfieldformfieldid = mother.formfieldid)
inner join fieldchoices fc2 on (fc2.formfieldid = mother.formfieldid) 
left outer join hiddenfieldchoices h on (h.fieldchoiceid = fc2.fieldchoiceid and h.formfieldid = ff.formfieldid)
where ff.formfieldid = " + fieldchoice + @"
union
select fc.fieldchoiceid, fc.formfieldid, 'ja', text, \""order\"" reihenfolge, case when h.hiddenfieldchoiceid is not null then 0 else 1 end sicthbar from fieldchoices fc 
left outer join hiddenfieldchoices h on (h.fieldchoiceid = fc.fieldchoiceid and h.formfieldid = fc.formfieldid)
where fc.\""formfieldid\"" = " + fieldchoice + @"
order by \""reihenfolge\""
";


            psi.Arguments = "-f CSV " + dataDir + "//Export//" + fname[0] + ".csv " + pgstring + " -sql \""+ sql_command +"\"";

            var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
            if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.First() });
  
            return Json(new ExportProcess() { Filename = fname[0] + ".csv"});

        }

        public IActionResult Import()
        {
            return View();
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
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

            string db_conf = Configuration["Environment:DB"];
            string host = Configuration["Environment:DBHost"];
            string dbuser = Configuration["Environment:DBUser"];
            string dbpassword = Configuration["Environment:DBPassword"];

            string pgstring = " PG:\"dbname = '" + db_conf + "' user = '" + dbuser + "' password = '" + dbpassword + "' host = '" + host + "'\"";

            psi.FileName = @"C:\Program Files\GDAL\ogr2ogr.exe";
            psi.WorkingDirectory = @"C:\Program Files\GDAL";
            psi.EnvironmentVariables["GDAL_DATA"] = @"C:\Program Files\GDAL\gdal-data";
            psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\Program Files\GDAL\gdal-plugins";
            psi.EnvironmentVariables["PATH"] = "C:\\Program Files\\GDAL;" + psi.EnvironmentVariables["PATH"];

            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;

            string format = System.IO.Path.GetExtension(filePath);

            string changes = "";

            
            psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_fieldchoice\" \"" + filePath + "\" --config OGR_XLSX_HEADERS FORCE";
            var OgrOgrResult = ProcessEx.RunAsync(psi).Result;
            if (OgrOgrResult.ExitCode != 0) return Json(new ExportProcess() { Error = OgrOgrResult.StandardError.FirstOrDefault().ToString() + " (" + psi.Arguments + ")" });

            string returnMessage = "";

            // overwrite filechoices or create new
            try
            {
                using (var context = new BioDivContext())
                {
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "select * from " + prefix + "_fieldchoice";
                        command.CommandType = CommandType.Text;

                        context.Database.OpenConnection();


                        using (var result = command.ExecuteReader())
                        {
                            var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();
                            int fieldchoiceid = names.IndexOf("fieldchoiceid");
                            int formfieldid = names.IndexOf("formfieldid");
                            int text = names.IndexOf("text");
                            int order = names.IndexOf("reihenfolge");
                            int visibility = names.IndexOf("sichtbar");

                            FormField? lastFF = null;

                            while (result.Read())
                            {
                                if (result[fieldchoiceid] != null)
                                {
                                    FieldChoice fc = null;
                                    try
                                    {
                                        int newId = Int32.Parse(result.GetString(fieldchoiceid));
                                        fc = db.FieldChoices.Include(m => m.FormField).ThenInclude(m=>m.HiddenFieldChoices).Where(m => m.FieldChoiceId == newId).FirstOrDefault();
                                        string content = result.GetString(text);

                                        if (fc.FormField.FormFieldId == Int32.Parse(result.GetString(formfieldid)))
                                        {
                                            lastFF = fc.FormField;

                                            if (content.ToLower() == "todelete")
                                            {
                                                db.FieldChoices.Remove(fc);
                                                db.Entry(fc).State = EntityState.Deleted;
                                                returnMessage += "<li>Auswahlfeld (" + fc.FieldChoiceId + ") gelöscht</li>";
                                            }
                                            else if ((fc.Text != content) || (fc.Order != Int32.Parse(result.GetString(order))))
                                            {
                                                fc.Text = content;
                                                fc.Order = Int32.Parse(result.GetString(order));

                                                db.Entry(fc).State = EntityState.Modified;
                                                returnMessage += "<li>Auswahlfeld (" + fc.FieldChoiceId + ") angepasst mit neuem Text " + fc.Text + "</li>";
                                            }

                                            int visibilityId = Int32.Parse(result.GetString(visibility));
                                            if (visibilityId == 0)
                                            {
                                                if (!fc.FormField.HiddenFieldChoices.Where(m => m.FieldChoice.FieldChoiceId == fc.FieldChoiceId).Any())
                                                {
                                                    HiddenFieldChoice hfc = new HiddenFieldChoice() { FieldChoice = fc, FormField = fc.FormField };
                                                    db.HiddenFieldChoices.Add(hfc);
                                                    db.Entry(hfc).State = EntityState.Added;

                                                    returnMessage += "<li>Auswahlfeld (" + fc.FieldChoiceId + "): Sichtbarkeit auf nicht sichtbar gestellt</li>";
                                                }
                                            }
                                            else
                                            {
                                                if (fc.FormField.HiddenFieldChoices.Where(m => m.FieldChoice.FieldChoiceId == fc.FieldChoiceId).Any())
                                                {
                                                    HiddenFieldChoice hfc = fc.FormField.HiddenFieldChoices.Where(m => m.FieldChoice.FieldChoiceId == fc.FieldChoiceId).First();
                                                    db.HiddenFieldChoices.Remove(hfc);
                                                    db.Entry(hfc).State = EntityState.Deleted;
                                                    returnMessage += "<li>Auswahlfeld (" + fc.FieldChoiceId + "): Sichtbarkeit auf sichtbar gestellt</li>";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Only changes the visibility --> fc is from publicmotherformfield
                                            FormField ff = db.FormFields.Include(m => m.PublicMotherFormField).ThenInclude(m=>m.FieldChoices).Include(m=>m.HiddenFieldChoices).ThenInclude(m=>m.FieldChoice).Where(m => m.FormFieldId == Int32.Parse(result.GetString(formfieldid))).FirstOrDefault();
                                            if (ff != null)
                                            {
                                                if ((ff.PublicMotherFormField!=null) && (ff.PublicMotherFormField.FieldChoices!=null) && (ff.PublicMotherFormField.FieldChoices.Contains(fc)))
                                                {
                                                    int visibilityId = Int32.Parse(result.GetString(visibility));
                                                    if (visibilityId==0)
                                                    {
                                                        if (!ff.HiddenFieldChoices.Where(m=>m.FieldChoice.FieldChoiceId == fc.FieldChoiceId).Any())
                                                        {
                                                            HiddenFieldChoice hfc = new HiddenFieldChoice() { FieldChoice = fc, FormField = ff };
                                                            db.HiddenFieldChoices.Add(hfc);
                                                            db.Entry(hfc).State = EntityState.Added;
                                                            returnMessage += "<li>Auswahlfeld (" + fc.FieldChoiceId + "): Sichtbarkeit auf nicht sichtbar gestellt</li>";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (ff.HiddenFieldChoices.Where(m => m.FieldChoice.FieldChoiceId == fc.FieldChoiceId).Any())
                                                        {
                                                            HiddenFieldChoice hfc = ff.HiddenFieldChoices.Where(m => m.FieldChoice.FieldChoiceId == fc.FieldChoiceId).First();
                                                            db.HiddenFieldChoices.Remove(hfc);
                                                            db.Entry(hfc).State = EntityState.Deleted;
                                                            returnMessage += "<li>Auswahlfeld (" + fc.FieldChoiceId + "): Sichtbarkeit auf sichtbar gestellt</li>";
                                                        }


                                                    }
                                                }
                                            }
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        int ffid = Int32.Parse(result.GetString(formfieldid));
                                        FormField ff = await db.FormFields.Where(m => m.FormFieldId == ffid).SingleOrDefaultAsync();
                                        if ((fc == null) && (ff != null))
                                        {
                                            fc = new FieldChoice();
                                            fc.FormField = ff;
                                            fc.Text = result.GetString(text);
                                            fc.Order = Int32.Parse(result.GetString(order));

                                            int visibilityId = Int32.Parse(result.GetString(visibility));
                                            if (visibilityId == 0)
                                            {
                                                HiddenFieldChoice hfc = new HiddenFieldChoice() { FieldChoice = fc, FormField = ff };
                                                db.HiddenFieldChoices.Add(hfc);
                                                db.Entry(hfc).State = EntityState.Added;
                                            }


                                            db.Entry(fc).State = EntityState.Added;
                                            returnMessage += "<li>Neue Auswahlmöglichkeit erstellt</li>";
                                        }

                                    }
                                    

                                        
                                    


                                }
                            }

                            await db.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                returnMessage += "<li>Fehler :" + ex.ToString() + "</li>";
            }


            changes += returnMessage;
            await this.db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS " + prefix + "_nogeometry;");
            

            changes = "Folgende Änderungen / Neuerungen wurden erfolgreich übernommen: <ul>" + changes + "</ul>";

            return View("ImportOK", changes.Replace("OK", ""));

        }

    }

    public class FormFieldNamesForView
    {
        public Form Form { get; set; }
        public FormField FormField { get; set; }

        public string FormTitle { get { return Form.Title; } }
        public string FieldTitle { get { return FormField.Title; } }
        public int number { get; set; }

        public bool isGeneral { get; set; }

        public int MaxLength { get; set; }

        public string XMLFieldTitle
        {
            get
            {
                return RemoveSpecialCharacters(FieldTitle.Replace(" ", "").Replace(".", ""));
            }
        }

        public string XMLFormTitle
        {
            get
            {
                return RemoveSpecialCharacters(FormTitle.Replace(" ", "").Replace(".", ""));
            }
        }

        public string ShortName
        {
            get
            {
                string myFormTitle = XMLFormTitle;
                if (isGeneral) myFormTitle = "Allgemein";
                string fot = XmlConvert.EncodeName(myFormTitle).Length > 20 ? XmlConvert.EncodeName(myFormTitle).Substring(0, 20) : XmlConvert.EncodeName(myFormTitle);
                string fit = XmlConvert.EncodeName(XMLFieldTitle).Length > 20 ? XmlConvert.EncodeName(XMLFieldTitle).Substring(0, 20) : XmlConvert.EncodeName(XMLFieldTitle);
                string numbers = number > 0 ? "-" + number.ToString() : "";
                string name = fot + "-" + fit + numbers;
                name = name.ToLower();
                return name.Length <= MaxLength ? name : name.Substring(0, MaxLength);

            }
        }

        public string ShortNameWithId
        {
            get
            {
                string myFormTitle = XMLFormTitle;
                if (isGeneral) myFormTitle = "Allgemein";
                string fot = XmlConvert.EncodeName(myFormTitle).Length > 20 ? XmlConvert.EncodeName(myFormTitle).Substring(0, 20) : XmlConvert.EncodeName(myFormTitle);
                string fit = XmlConvert.EncodeName(XMLFieldTitle).Length > 20 ? XmlConvert.EncodeName(XMLFieldTitle).Substring(0, 20) : XmlConvert.EncodeName(XMLFieldTitle);
                string numbers = number > 0 ? "-" + number.ToString() : "";
                //string name = FormField.FormFieldId + "_" + fot + "_" + fit + numbers;
                // the first f is sponsored by jack dangermond. He doesn't like the numbers in the front...
                string name = "f_"+FormField.FormFieldId + "_" + fot + "_" + fit + numbers; 
                name = name.ToLower();

                // super short version when MaxLength = -1: only f_number
                if (MaxLength == -1)
                {
                    if (isGeneral)
                    {
                        if (FormField.PublicMotherFormFieldFormFieldId != null) return "a_" + FormField.PublicMotherFormFieldFormFieldId;
                        else return "a_" + FormField.FormFieldId;
                    }

                    return "f_" + FormField.FormFieldId;


                }

                return name.Length <= MaxLength ? name : name.Substring(0, MaxLength);

            }
        }

        public string ShortNameWithoutNumber
        {
            get
            {
                string myFormTitle = XMLFormTitle;
                if (isGeneral) myFormTitle = "Allgemein";
                string fot = XmlConvert.EncodeName(myFormTitle).Length > 20 ? XmlConvert.EncodeName(myFormTitle).Substring(0, 20) : XmlConvert.EncodeName(myFormTitle);
                string fit = XmlConvert.EncodeName(XMLFieldTitle).Length > 20 ? XmlConvert.EncodeName(XMLFieldTitle).Substring(0, 20) : XmlConvert.EncodeName(XMLFieldTitle);
                string name =  fot + "-" + fit;
                name = name.ToLower();
                return name.Length <= MaxLength ? name : name.Substring(0, MaxLength);

            }
        }
        public string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }


    }

    public class FormPoco
    {
        public Form Form { get; set; }
        public bool Editable { get; set; }
        public User Author { get; set; }
        public int RecordsCount { get; set; }
    }

    public class FormBuilderJson
    {
        public int id { get; set; }
        public string title { get; set; }
        public List<FormFieldPoco> json { get; set; }
    }

    public class PublicFormFieldPoco
    {
        public string label { get; set; }
        public string name { get; set; }
        public FormFieldPoco attrs { get; set; }
    }

    public class FormFieldPoco
    {
        //bac20201209: label user for FormField.Title, description not mapped.
        public string type { get; set; }
        public string label { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string value { get; set; }
        public string source { get; set; }
        public bool mandatory { get; set; }
        public bool useinrecordtitle { get; set; }
        public bool ispublic { get; set; }
        public bool isreadonly { get; set; }
        public string author { get; set; }
        public string subtype { get; set; }
        public List<OptionsPoco> values { get; set; }
    }


    public class OptionsPoco
    {
        public string label { get; set; }
        public string value { get; set; }
        public bool selected { get; set; }
        public bool visible { get; set; }
    }

    public class FormsPoco
    {
        public string Title { get; set; }
        public int FormId { get; set; }
    }


    public class SelectedFormsPoco
    {
        [Newtonsoft.Json.JsonIgnore]
        public DB.Models.Domain.Form myForm { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public Project myProject { get; set; }
        public string item { get { return myForm.Title; } }
        public string value { get { return myForm.FormId.ToString(); } }
        public bool selected
        {
            get
            {
                foreach (ProjectForm pf in myForm.FormProjects)
                {
                    if (pf.ProjectId == myProject.ProjectId) return true;
                }
                return false;
            }
        }
        public bool disabled { get; set; }
    }


    public class Projects
    {
        public ProjectIds[] items { get; set; }
        public Guid guid { get; set; }

    }


    public class ProjectIds
    {
        public string item { get; set; }
        public string value { get; set; }
    }

    public class Forms
    {
        public FormIds[] items { get; set; }
        public Guid guid { get; set; }

    }


    public class FormIds
    {
        public string item { get; set; }
        public string value { get; set; }
    }

    public class LookupTableViewModel
    {
        public FormField FormField { get; set; }
        public List<Form> UsedInForms { get; set; }
    }

    public class BigFieldChoice
    {
        public FieldChoice FieldChoice { get; set; }
        public bool isHidden { get; set; }
    }

}
