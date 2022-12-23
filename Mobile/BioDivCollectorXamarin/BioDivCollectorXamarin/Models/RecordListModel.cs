using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;
using SQLite;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models
{
    public class RecordListModel
    {

        /// <summary>
        /// The form name for filtering the records
        /// </summary>
        private string formName;
        public string FormName
        {
            get { return formName; }
            set
            {
                formName = value;
                var form = Form.FetchFormWithFormName(formName);
                if (form != null)
                {
                    Form_pk = form.Id;
                }
            }
        }

        /// <summary>
        /// The form pk for filtering the records
        /// </summary>
        public int? Form_pk { get; set; }

        private string filterBy;
        public string FilterBy
        {
            get
            {
                return filterBy;
            }
            set
            {
                filterBy = value;
                if (filterBy != "Geometrie")
                {
                    Preferences.Set("FilterGeometry", "");
                }
            }
        }

        /// <summary>
        /// Instantiate the record list model
        /// </summary>
        public RecordListModel()
        {
        }

        /// <summary>
        /// Create the record list required for the records page
        /// </summary>
        /// <param name="project"></param>
        /// <param name="sortby"></param>
        /// <param name="filter"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<List<GroupedFormRec>> ListRecords(Project project, string sortby, string filter, int? id)
        {
            try
            {
                var longName = string.Empty;
                var shortName = string.Empty;
                var conn = App.ActiveDatabaseConnection;
                //SORT BY GEOMETRY CASE

                //No Geometry

                var nogroup = new GroupedFormRec() { LongGeomName = "Allgemeine Beobachtungen", ShowButton = false };
                var norecList = new List<FormRec>();

                if (filter == "Formulartyp")
                {
                    norecList = (from record in await conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == null).Where(Record => Record.project_fk == project.Id).Where(Record => Record.status < 3).Where(Record => Record.formId == id).ToListAsync()
                                 join form in await conn.Table<Form>().Where(f => f.project_fk == project.Id).ToListAsync()
                                              on record.formId equals form.formId
                                 select new FormRec
                                 {
                                     String1 = CreateTitleStringForRecordFromForm(form.Id, record.recordId).ToString(),
                                     String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName,
                                     RecId = record.recordId,
                                     FormId = record.formId
                                 }).ToList();

                    foreach (var rec in norecList)
                    {
                        var prev = nogroup.Select(p => p.RecId == rec.RecId).ToList();
                        if (!prev.Contains(true))
                        {
                            if (rec != null) { nogroup.Add(rec); }
                        }
                    }
                }
                else if (filter != "Geometrie")
                {
                    norecList = (from record in await conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == null).Where(Record => Record.project_fk == project.Id).Where(Record => Record.status < 3).ToListAsync()
                                 join form in await conn.Table<Form>().Where(f => f.project_fk == project.Id).ToListAsync()
                                              on record.formId equals form.formId
                                 select new FormRec
                                 {
                                     String1 = CreateTitleStringForRecordFromForm(form.Id, record.recordId).ToString(),
                                     String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName,
                                     RecId = record.recordId,
                                     FormId = record.formId
                                 }).ToList();

                    foreach (var rec in norecList)
                    {
                        var prev = nogroup.Select(p => p.RecId == rec.RecId).ToList();
                        if (!prev.Contains(true))
                        {
                            if (rec != null) { nogroup.Add(rec); }
                        }
                    }

                }


                var recList = new List<FormRec>();

                if (filter == "Geometrie")
                {
                    recList = (from record in await conn.Table<Record>().Where(Record => Record.status < 3).Where(Record => Record.geometry_fk == id).ToListAsync()
                               join form in await conn.Table<Form>().Where(f => f.project_fk == project.Id).ToListAsync()
                                            on record.formId equals form.formId
                               join referenceGeometry in await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToListAsync()
                                            on record.geometry_fk equals referenceGeometry.Id
                               select new FormRec 
                               {
                                   String1 = CreateTitleStringForRecordFromForm(form.Id, record.recordId).ToString(),
                                   String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, 
                                   GeomId = referenceGeometry.Id, RecId = record.recordId, FormId = record.formId 
                               }).ToList();
                }
                else if (filter == "Formulartyp")
                {
                    recList = (from record in await conn.Table<Record>().Where(Record => Record.status < 3).Where(Record => Record.formId == id).ToListAsync()
                               join form in await conn.Table<Form>().Where(f => f.project_fk == project.Id).ToListAsync()
                                            on record.formId equals form.formId
                               join referenceGeometry in await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToListAsync()
                                            on record.geometry_fk equals referenceGeometry.Id
                               select new FormRec 
                               {
                                   String1 = CreateTitleStringForRecordFromForm(form.Id, record.recordId).ToString(),
                                   String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, 
                                   GeomId = referenceGeometry.Id, RecId = record.recordId, FormId = record.formId 
                               }).ToList();
                }
                else
                {
                    recList = (from record in await conn.Table<Record>().Where(Record => Record.status < 3).ToListAsync()
                               join form in await conn.Table<Form>().Where(f => f.project_fk == project.Id).ToListAsync()
                                            on record.formId equals form.formId
                               join referenceGeometry in await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToListAsync()
                                            on record.geometry_fk equals referenceGeometry.Id
                               select new FormRec 
                               {
                                   String1 = CreateTitleStringForRecordFromForm(form.Id, record.recordId).ToString(),
                                   String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, 
                                   GeomId = referenceGeometry.Id, RecId = record.recordId, FormId = record.formId }).ToList();
                }

                if (sortby == "Formulartyp")
                {
                    recList.AddRange(norecList);
                    var formResults = recList.GroupBy(r => r.FormId, r => r, (key, r) => new { Form = key, Rec = r.ToList() });
                    var forms = await conn.Table<Form>().Where(Form => Form.project_fk == project.Id).ToListAsync();
                    var recordsByForm = formResults.Join(forms, rid => rid.Form, fid => fid.formId, (formrec, form) => new { LongGeomName = form.title, GeomId = form.Id, ShowButton = false, RecList = formrec.Rec }).Select(g => new GroupedFormRec(g.RecList as List<FormRec>) { LongGeomName = g.LongGeomName, GeomId = g.GeomId, ShowButton = false }).ToList();
                    //Sort groups by name
                    var recordsByFormOrdered = recordsByForm.OrderBy(rec => rec.LongGeomName).Select(rec => rec).ToList();
                    return recordsByFormOrdered;
                }
                else
                {
                    var recResults = recList.GroupBy(r => r.GeomId, r => r, (key, r) => new { Geom = key, Rec = r.ToList() });
                    var geoms = await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToListAsync();
                    var recordsByGeometry = recResults.Join(geoms, rid => rid.Geom, gid => gid.Id, (formrec, geom) => new { LongGeomName = geom.geometryName, GeomId = geom.Id, ShowButton = true, RecList = formrec.Rec }).Select(g => new GroupedFormRec(g.RecList as List<FormRec>) { LongGeomName = g.LongGeomName, GeomId = g.GeomId, ShowButton = true }).ToList();

                    //Add in geometries with no records
                    var geomlist = new List<ReferenceGeometry>();
                    if (id == null || filter == "Formulartyp")
                    {
                        geomlist = await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.status != 3).ToListAsync();
                    }
                    else
                    {
                        geomlist = await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.Id == id).Where(ReferenceGeometry => ReferenceGeometry.status != 3).ToListAsync();
                    }

                    foreach (var geom in geomlist)
                    {
                        if (recResults.Where(r => r.Geom == geom.Id).Count() == 0)
                        {
                            recordsByGeometry.Add(new GroupedFormRec { LongGeomName = geom.geometryName, GeomId = geom.Id, ShowButton = true });
                        }
                    }

                    //Sort groups by name
                    var recordsByGeometryOrdered = recordsByGeometry.OrderBy(rec => rec.LongGeomName).Select(rec => rec).ToList();
                    if (nogroup != null && (id == null || filter == "Formulartyp")) { recordsByGeometryOrdered.Insert(0, nogroup); }
                    return recordsByGeometryOrdered;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

        }



        /// <summary>
        /// Compile the title string for the record, based on the parameters selected to be used in the title in the form definition
        /// </summary>
        /// <param name="rec"></param>
        /// <returns>The record title</returns>
        private static string CreateTitleStringForRecordFromForm(int formId, string recId)
        {
            var createTitleStringTask = new Task<string>(() =>
            {
                try
                {
                    bool hasRecords;
                    var textRecords = TextData.FetchTextDataByRecordId(recId).Result;
                    var noOfTextRecords = textRecords.Where(txt => txt.value != string.Empty).Where(txt => txt.value != null).Count();
                    var boolRecords = BooleanData.FetchBooleanDataByRecordId(recId).Result;
                    var noOfBoolRecords = boolRecords.Where(b => b.value != null).Count();
                    var numericRecords = NumericData.FetchNumericDataByRecordId(recId).Result;
                    var noOfNumericRecords = numericRecords.Count();
                    var binaryRecords = BinaryData.FetchBinaryDataByRecordId(recId).Result;
                    var noOfBinaryRecords = binaryRecords.Count();
                    hasRecords = (noOfTextRecords + noOfBoolRecords + noOfNumericRecords + noOfBinaryRecords) > 0;
                    var title = "";
                    var form = Form.FetchForm(formId).Result;
                    var formFields = Form.FetchFormFields(formId).Result;

                    if (formFields != null)
                    {
                        foreach (var formField in formFields)
                        {
                            if (formField.typeId == 11 || formField.typeId == 51 || formField.typeId == 61)
                            {
                                try
                                {
                                    var txtList = TextData.FetchTextDataByFormFieldId(formField.fieldId).Result;
                                    TextData txt = txtList.Where(Txt => Txt.record_fk == recId).FirstOrDefault();
                                    title = title + txt.value + ", ";
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                        if (title.Length >= 2)
                        {
                            title = title.Substring(0, title.Length - 2);
                        }
                    }
                    if (title == String.Empty || title == " ") { title = form.title; }
                    if (!hasRecords)
                    { title = "⚠️ " + title; }
                    return title;
                }
                catch
                {
                    return String.Empty;
                }
            });

            createTitleStringTask.Start();
            var titleString = createTitleStringTask.Result;
            return titleString;
        }
    }
}
