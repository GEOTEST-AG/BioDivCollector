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


        public void CreateRecordLists()
        {
            RecordsByGeometry = new List<GroupedFormRec>();
            RecordsByForm = new List<GroupedFormRec>();
            var project = Project.FetchCurrentProject();

            ListRecordsByGeometry(project);
            ListRecordsByForm(project);
            MessagingCenter.Send<Application>(App.Current, "RefreshRecords");
        }

        public void ListRecordsByGeometry(Project project)
        {
                try
                {
                    var longName = string.Empty;
                    var shortName = string.Empty;
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {
                        //SORT BY GEOMETRY CASE

                        //No Geometry

                            var nogroup = new GroupedFormRec() { LongGeomName = "Allgemeine Beobachtungen", ShortGeomName = "Allgemein", ShowButton = false };
                            var norecList = new List<FormRec>();

                                norecList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == null).Where(Record => Record.project_fk == project.Id).Where(Record => Record.status < 3).ToList()
                                             join form in conn.Table<Form>().ToList()
                                                          on record.formId equals form.formId
                                             select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = record.formId, RecId = record.Id, User = record.fullUserName }).ToList();

                                foreach (var rec in norecList)
                                {
                                    var title = CreateTitleStringForRecord(rec);
                                    if (title != String.Empty && title != " ") { rec.Title = title; } else { rec.Title = rec.FormType; }
                                    var prev = nogroup.Select(p => p.RecId == rec.RecId).ToList();
                                    if (!prev.Contains(true))
                                    {
                                        if (rec != null) { nogroup.Add(rec); }
                                    }
                                }
                            if (nogroup != null) { RecordsByGeometry.Add(nogroup); }


                        //For each geometry
                        var geoms = new List<ReferenceGeometry>();
                        var geomsTemp = new List<ReferenceGeometry>();
                        geomsTemp = conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.status < 3).ToList();
                        geoms = geomsTemp.OrderBy(o => o.geometryName).ToList();

                        foreach (var geom in geoms)
                        {
                            var geomName = geom.geometryName ?? String.Empty;
                            var group = new GroupedFormRec() { LongGeomName = geomName, ShortGeomName = geomName, ShowButton = true, Geom = geom };
                            var recList = new List<FormRec>();

                                recList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == geom.Id).Where(Record => Record.status < 3).ToList()
                                           join form in conn.Table<Form>().Where(f => f.project_fk == project.Id).ToList()
                                                        on record.formId equals form.formId
                                           join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.Id == geom.Id).Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                        on record.geometry_fk equals referenceGeometry.Id
                                           select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = record.formId, RecId = record.Id, User = record.fullUserName, GeometryName = geomName, GeomId = referenceGeometry.Id }).ToList();

                            foreach (var rec in recList)
                            {
                                var title = CreateTitleStringForRecord(rec);
                                if (title != String.Empty && title != " ") { rec.Title = title; } else { rec.Title = rec.FormType; }
                                if (rec != null) { group.Add(rec); }
                            }
                            if (group != null) { RecordsByGeometry.Add(group); }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

        }

        public void ListRecordsByForm(Project project)
        {
                try
                {
                    var longName = string.Empty;
                    var shortName = string.Empty;
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {

                        //SORT BY FORM TYPE CASE
                        var forms = new List<Form>();

                            var formsTemp = conn.Table<Form>().Where(Form => Form.project_fk == project.Id).ToList();
                            forms = formsTemp.OrderBy(o => o.title).ToList();


                        //For each form

                        foreach (var formgr in forms)
                        {
                            var group = new GroupedFormRec() { LongGeomName = formgr.title ?? "", ShortGeomName = formgr.title ?? "", ShowButton = false };
                            var recList = new List<FormRec>();
                            var recListNoGeom = new List<FormRec>();

                                recList = (from record in conn.Table<Record>().Where(Record => Record.formId == formgr.formId).Where(Record => Record.status < 3).Where(Record => Record.project_fk == project.Id).ToList()
                                           join form in conn.Table<Form>().Where(Form => Form.project_fk == project.Id).Where(Form => Form.title == formgr.title).ToList()
                                                        on record.formId equals form.formId
                                           join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                        on record.geometry_fk equals referenceGeometry.Id
                                           select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = form.formId, RecId = record.Id, User = record.fullUserName, GeometryName = referenceGeometry.geometryName }).ToList();
                                recListNoGeom = (from record in conn.Table<Record>().Where(Record => Record.formId == formgr.formId).Where(Record => Record.status < 3).Where(Record => Record.geometry_fk == null).Where(Record => Record.project_fk == project.Id).ToList()
                                                 join form in conn.Table<Form>().Where(Form => Form.project_fk == project.Id).Where(Form => Form.title == formgr.title).ToList()
                                                              on record.formId equals form.formId
                                                 select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = form.formId, RecId = record.Id, User = record.fullUserName, GeometryName = String.Empty }).ToList();


                            foreach (var rec in recListNoGeom)
                            {
                                var title = CreateTitleStringForRecord(rec);
                                if (title != String.Empty && title != " ") { rec.Title = title; } else { rec.Title = rec.FormType; }
                                if (rec != null) { group.Add(rec); }
                            }
                            foreach (var rec in recList)
                            {
                                var title = CreateTitleStringForRecord(rec);
                                if (title != String.Empty && title != " ") { rec.Title = title; } else { rec.Title = rec.FormType; }
                                if (rec != null) { group.Add(rec); }
                            }
                            if (group != null) { RecordsByForm.Add(group); }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        }


        void ObservableCollectionCallback(IEnumerable collection, object context, Action accessMethod, bool writeAccess)
        {
            // `lock` ensures that only one thread access the collection at a time
            lock (collection)
            {
                accessMethod?.Invoke();
            }
        }



        /// <summary>
        /// Compile the title string for the record, based on the parameters selected to be used in the title in the form definition
        /// </summary>
        /// <param name="rec"></param>
        /// <returns>The record title</returns>
        private string CreateTitleStringForRecord(FormRec rec)
        {
            var title = "";
            var formFields = Form.FetchFormFields(rec.FormId);

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
                                TextData txt = txtconn.Table<TextData>().Where(Txt => Txt.formFieldId == formField.fieldId).Where(Txt => Txt.record_fk == rec.RecId).FirstOrDefault();
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
                    rec.Title = title;
                }
            }
            return title;
        }

    }
}
