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
        /// A list of records, organised into groups
        /// </summary>
        public List<GroupedFormRec> RecordsByGeometry { get; set; }

        /// <summary>
        /// A list of records, organised into groups
        /// </summary>
        public List<GroupedFormRec> RecordsByForm { get; set; }

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

        public RecordListModel()
        {
            RecordsByGeometry = new List<GroupedFormRec>();
            RecordsByForm = new List<GroupedFormRec>();
        }


        public static IOrderedEnumerable<GroupedFormRec> ListRecords(Project project, string sortby, string filter, int? id)
        {
            try
            {
                var longName = string.Empty;
                var shortName = string.Empty;
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    //SORT BY GEOMETRY CASE

                    //No Geometry

                    var nogroup = new GroupedFormRec() { LongGeomName = "Allgemeine Beobachtungen", ShowButton = false };
                    var norecList = new List<FormRec>();

                    if (filter == "Formulartyp")
                    {
                        norecList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == null).Where(Record => Record.project_fk == project.Id).Where(Record => Record.status < 3).Where(Record => Record.formId == id).ToList()
                                     join form in conn.Table<Form>().Where(f => f.formId == id).ToList()
                                                  on record.formId equals form.formId
                                     select new FormRec { String1 = CreateTitleStringForRecordFromForm(form.Id, record.Id), String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, RecId = record.Id }).ToList();

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
                        norecList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == null).Where(Record => Record.project_fk == project.Id).Where(Record => Record.status < 3).ToList()
                                     join form in conn.Table<Form>().ToList()
                                                  on record.formId equals form.formId
                                     select new FormRec { String1 = CreateTitleStringForRecordFromForm(form.Id, record.Id), String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, RecId = record.Id }).ToList();

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
                        recList = (from record in conn.Table<Record>().Where(Record => Record.status < 3).Where(Record => Record.geometry_fk == id).ToList()
                                   join form in conn.Table<Form>().Where(f => f.project_fk == project.Id).ToList()
                                                on record.formId equals form.formId
                                   join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                on record.geometry_fk equals referenceGeometry.Id
                                   select new FormRec { String1 = CreateTitleStringForRecordFromForm(form.Id, record.Id), String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, GeomId = referenceGeometry.Id, RecId = record.Id, FormId = record.formId }).ToList();
                    } else if (filter == "Formulartyp")
                    {
                        recList = (from record in conn.Table<Record>().Where(Record => Record.status < 3).Where(Record => Record.formId == id).ToList()
                                   join form in conn.Table<Form>().Where(f => f.project_fk == project.Id).Where(f => f.formId == id).ToList()
                                                on record.formId equals form.formId
                                   join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                on record.geometry_fk equals referenceGeometry.Id
                                   select new FormRec { String1 = CreateTitleStringForRecordFromForm(form.Id, record.Id), String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, GeomId = referenceGeometry.Id, RecId = record.Id, FormId = record.formId }).ToList();
                    }
                    else
                    {
                        recList = (from record in conn.Table<Record>().Where(Record => Record.status < 3).ToList()
                                       join form in conn.Table<Form>().Where(f => f.project_fk == project.Id).ToList()
                                                    on record.formId equals form.formId
                                       join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                    on record.geometry_fk equals referenceGeometry.Id
                                       select new FormRec { String1 = CreateTitleStringForRecordFromForm(form.Id, record.Id), String2 = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")) + ", " + record.fullUserName, GeomId = referenceGeometry.Id, RecId = record.Id, FormId = record.formId }).ToList();
                    }

                    if (sortby == "Formulartyp")
                    {
                        recList.AddRange(norecList);
                        var formResults = recList.GroupBy(r => r.FormId, r => r, (key, r) => new { Form = key, Rec = r.ToList() });
                        var forms = conn.Table<Form>().Where(Form => Form.project_fk == project.Id).ToList();
                        var recordsByForm = formResults.Join(forms, rid => rid.Form, fid => fid.formId, (formrec, form) => new { LongGeomName = form.title, GeomId = form.Id, ShowButton = false, RecList = formrec.Rec }).Select(g => new GroupedFormRec(g.RecList as List<FormRec>) { LongGeomName = g.LongGeomName, GeomId = g.GeomId, ShowButton = false }).ToList();
                        //Sort groups by name
                        var recordsByFormOrdered = recordsByForm.OrderBy(rec => rec.LongGeomName);
                        return recordsByFormOrdered;
                    }
                    else
                    {
                        var recResults = recList.GroupBy(r => r.GeomId, r => r, (key, r) => new { Geom = key, Rec = r.ToList() });
                        var geoms = conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList();
                        var recordsByGeometry = recResults.Join(geoms, rid => rid.Geom, gid => gid.Id, (formrec, geom) => new { LongGeomName = geom.geometryName, GeomId = geom.Id, ShowButton = true, RecList = formrec.Rec }).Select(g => new GroupedFormRec(g.RecList as List<FormRec>) { LongGeomName = g.LongGeomName, GeomId = g.GeomId, ShowButton = true }).ToList();

                        //Add in geometries with no records
                        var geomlist = new List<ReferenceGeometry>();
                        if (id == null)
                        {
                            geomlist = conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.status != 3).ToList();
                        }
                        else
                        {
                            geomlist = conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.Id == id).Where(ReferenceGeometry => ReferenceGeometry.status != 3).ToList();
                        }
                           
                        foreach (var geom in geomlist)
                        {
                            if (recResults.Where(r => r.Geom == geom.Id).Count() == 0)
                            {
                                recordsByGeometry.Add(new GroupedFormRec { LongGeomName = geom.geometryName, GeomId = geom.Id, ShowButton = true });
                            }
                        }

                        //Sort groups by name
                        var recordsByGeometryOrdered = recordsByGeometry.OrderBy(rec => rec.LongGeomName);
                        if (nogroup != null && id == null) { recordsByGeometry.Insert(0, nogroup); }
                        return recordsByGeometryOrdered;
                    }
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
        private static string CreateTitleStringForRecordFromForm(int formId, int recId)
        {
            try
            {
                var title = "";
                var form = Form.FetchForm(formId);
                var formFields = Form.FetchFormFields(formId);

                if (formFields != null)
                {
                    foreach (var formField in formFields)
                    {
                        if (formField.typeId == 11 || formField.typeId == 51 || formField.typeId == 61)
                        {
                            using (SQLiteConnection txtconn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                            {
                                try
                                {
                                    TextData txt = txtconn.Table<TextData>().Where(Txt => Txt.formFieldId == formField.fieldId).Where(Txt => Txt.record_fk == recId).FirstOrDefault();
                                    title = title + txt.value + ", ";
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                    }
                    if (title.Length >= 2)
                    {
                        title = title.Substring(0, title.Length - 2);
                    }
                }
                if (title == String.Empty || title == " ") { title = form.title; }
                return title;
            }
            catch
            {
                return String.Empty;
            }

        }

    }
}
