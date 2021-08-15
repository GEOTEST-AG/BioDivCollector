using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Views;
using SQLite;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class RecordsPageVM : BaseViewModel
    {
        /// <summary>
        /// The selected record
        /// </summary>
        private FormRec _selectedItem;

        /// <summary>
        /// A list of records, organised into groups
        /// </summary>
        public ObservableCollection<GroupedFormRec> Records { get; set; }

        /// <summary>
        /// Button command for adding a new record
        /// </summary>
        public Command AddItemCommand { get; }

        /// <summary>
        /// Command for selecting a record from the list
        /// </summary>
        public Command<FormRec> ItemTapped { get; }

        /// <summary>
        /// A button command for copying the BDC GUID
        /// </summary>
        public BDCGUIDRecordCommand CopyBDCGUIDCommand { get; set; }

        /// <summary>
        /// A command for deleting the selected record
        /// </summary>
        public RecordDeleteCommand RecordDeleteCommand { get; set; }

        /// <summary>
        /// A command for deleting the selected geometry
        /// </summary>
        public GeometryDeleteCommand GeometryDeleteCommand { get; set; }

        /// <summary>
        /// The selected/filtered object
        /// </summary>
        private int? object_pk;
        public int? Object_pk
        {
            get
            {
                return object_pk;
            }
            set
            {
                object_pk = value;
                Preferences.Set("FilterGeometry", value.ToString());
                FilterBy = "Geometrie";
                UpdateRecords();
            }
        }

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
        /// The method by which to sort the list
        /// </summary>
        private string sortBy;
        public string SortBy
        {
            get
            {
                return sortBy;
            }
            set
            {
                sortBy = value;
            }
        }

        /// <summary>
        /// The selected/filtered geometry
        /// </summary>
        public ReferenceGeometry CurrentGeometry { get; set; }

        /// <summary>
        /// Initialisation without a selected geometry
        /// </summary>
        public RecordsPageVM()
        {
            Title = "Beobachtungen";
            if (SortBy == String.Empty) SortBy = "Geometrie";

            string filterGeom = Preferences.Get("FilterGeometry", String.Empty);
            int filterGeomVal;
            if (filterGeom != String.Empty)
            {
                int.TryParse(filterGeom, out filterGeomVal);
                Object_pk = filterGeomVal;
            }

            if (App.CurrentRoute.Contains("///Records?objectId="))
            {
                var objArr = App.CurrentRoute.Split('=');
                var objId = objArr[1];
                Object_pk = Int32.Parse(objId);
            }

            ItemTapped = new Command<FormRec>(OnItemSelected);

            AddItemCommand = new Command(OnAddItem);
            CopyBDCGUIDCommand = new BDCGUIDRecordCommand(this);
            RecordDeleteCommand = new RecordDeleteCommand(this);
            GeometryDeleteCommand = new GeometryDeleteCommand(this);

            UpdateRecords();

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshRecords", (sender) =>
            {
                UpdateRecords();
            });
            MessagingCenter.Subscribe<Application>(App.Current, "RefreshGeometries", (sender) =>
            {
                UpdateRecords();
            });
            MessagingCenter.Subscribe<Application>(App.Current, "SetProject", (sender) =>
            {
                FilterBy = String.Empty;
            });
        }

        /// <summary>
        /// Initialisation with a selected geometry
        /// </summary>
        /// <param name="objectId"></param>
        public RecordsPageVM(int objectId)
        {
            Title = "Beobachtungen";
            if (SortBy == String.Empty) SortBy = "Geometrie";
            App.CurrentRoute = "//Records?objectId=" + objectId.ToString();

            ItemTapped = new Command<FormRec>(OnItemSelected);

            AddItemCommand = new Command(OnAddItem);
            CopyBDCGUIDCommand = new BDCGUIDRecordCommand(this);
            RecordDeleteCommand = new RecordDeleteCommand(this);
            GeometryDeleteCommand = new GeometryDeleteCommand(this);

            Object_pk = objectId;

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshRecords", (sender) =>
            {
                UpdateRecords();
            });
            MessagingCenter.Subscribe<Application>(App.Current, "RefreshGeometries", (sender) =>
            {
                UpdateRecords();
            });
            MessagingCenter.Subscribe<Application>(App.Current, "SetProject", (sender) =>
            {
                FilterBy = String.Empty;
            });
        }

        /// <summary>
        /// Update the records whenever we return to the page
        /// </summary>
        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
            App.RecordLists.CreateRecordLists();
            UpdateRecords();

        }

        /// <summary>
        /// Based on the current sorting and filtering methods, read the groups into which the records should be sorted, then read the records associated with each of these groups. When reading the records, we also need to know their associated forms and geometries
        /// </summary>
        public void UpdateRecords()
        {
            var recs = new List<GroupedFormRec>();
            if (recs != null && SortBy != "Formulartyp")
            {
                if (FilterBy == "Formulartyp")
                {
                    var frm = Form.FetchForm((int)Form_pk);
                    foreach (var group in App.RecordLists.RecordsByGeometry)
                    {
                        var newGroup = new GroupedFormRec();
                        newGroup.LongGeomName = group.LongGeomName;
                        newGroup.ShortGeomName = group.ShortGeomName;
                        newGroup.GeomId = group.GeomId;
                        newGroup.ShowButton = group.ShowButton;
                        newGroup.Geom = group.Geom;

                        foreach (FormRec form in group)
                        {
                            if (form.FormId == frm.formId)
                            {
                                newGroup.Add(form);
                            }
                        }
                        recs.Add(newGroup);
                    }
                }

                else if (FilterBy == "Geometrie" && object_pk != null)
                {
                    var obj = ReferenceGeometry.GetGeometry((int)Object_pk);
                    foreach (var group in App.RecordLists.RecordsByGeometry)
                    {
                        try
                        {
                            if (group.FirstOrDefault().GeomId == obj.Id)
                            {
                                var newGroup = new GroupedFormRec();
                                newGroup.LongGeomName = group.LongGeomName;
                                newGroup.ShortGeomName = group.ShortGeomName;
                                newGroup.GeomId = group.GeomId;
                                newGroup.ShowButton = group.ShowButton;
                                newGroup.Geom = group.Geom;

                                foreach (FormRec form in group)
                                {
                                    newGroup.Add(form);
                                }
                                recs.Add(newGroup);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

                else
                {
                    recs = App.RecordLists.RecordsByGeometry;
                }
                Records = new ObservableCollection<GroupedFormRec>(recs);
            }
            else if (recs != null && SortBy == "Formulartyp")
            {
                if (FilterBy == "Formulartyp")
                {
                    var frm = Form.FetchForm((int)Form_pk);
                    foreach (var group in App.RecordLists.RecordsByForm)
                    {
                        var newGroup = new GroupedFormRec();
                        newGroup.LongGeomName = group.LongGeomName;
                        newGroup.ShortGeomName = group.ShortGeomName;
                        newGroup.GeomId = group.GeomId;
                        newGroup.ShowButton = group.ShowButton;
                        newGroup.Geom = group.Geom;

                        foreach (FormRec form in group)
                        {
                            if (form.FormId == frm.formId)
                            {
                                newGroup.Add(form);
                            }
                        }
                        recs.Add(newGroup);
                    }
                }

                else if (FilterBy == "Geometrie" && Object_pk != null)
                {
                    var obj = ReferenceGeometry.GetGeometry((int)Object_pk);
                    foreach (var group in App.RecordLists.RecordsByForm)
                    {
                        if (group.GeomId.ToString() == obj.geometryId)
                        {
                            var newGroup = new GroupedFormRec();
                            newGroup.LongGeomName = group.LongGeomName;
                            newGroup.ShortGeomName = group.ShortGeomName;
                            newGroup.GeomId = group.GeomId;
                            newGroup.ShowButton = group.ShowButton;
                            newGroup.Geom = group.Geom;

                            foreach (FormRec form in group)
                            {
                                newGroup.Add(form);
                            }
                            recs.Add(newGroup);
                        }
                    }
                }

                else
                {
                    recs = App.RecordLists.RecordsByForm;
                }
                Records = new ObservableCollection<GroupedFormRec>(recs);
            }
            else
            {
                Records = new ObservableCollection<GroupedFormRec>();
            }

            


            OnPropertyChanged("Records");
            /*Records = new ObservableCollection<GroupedFormRec>();
            Xamarin.Forms.BindingBase.EnableCollectionSynchronization(Records, null, ObservableCollectionCallback);
            var project = Project.FetchCurrentProject();
            Task.Run(async () =>
            {
                try
                {
                    var longName = string.Empty;
                    var shortName = string.Empty;
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {
                        if (SortBy == null || SortBy == string.Empty || SortBy == "Geometrie")
                        {
                            //SORT BY GEOMETRY CASE

                            //No Geometry

                            if (FilterBy == null || FilterBy != "Geometrie")
                            {
                                var nogroup = new GroupedFormRec() { LongGeomName = "Allgemeine Beobachtungen", ShortGeomName = "Allgemein", ShowButton = false };
                                var norecList = new List<FormRec>();
                                if (FilterBy == "Formulartyp")
                                {

                                    norecList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == null).Where(Record => Record.project_fk == project.Id).Where(Record => Record.status < 3).ToList()
                                                 join form in conn.Table<Form>().Where(Form => Form.title == FormName).ToList()
                                                              on record.formId equals form.formId
                                                 select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = record.formId, RecId = record.Id, User = record.fullUserName }).ToList();

                                    foreach (var rec in norecList)
                                    {
                                        var title = CreateTitleStringForRecord(rec);
                                        if (title != String.Empty && title != " ") { rec.Title = title; } else { rec.Title = rec.FormType; }
                                        nogroup.Add(rec);
                                    }

                                }
                                else
                                {
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
                                }
                                if (nogroup != null) { Records.Add(nogroup); }
                            }


                            //For each geometry
                            var geoms = new List<ReferenceGeometry>();
                            var geomsTemp = new List<ReferenceGeometry>();
                            if (FilterBy != "Geometrie")
                            {

                                geomsTemp = conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.status < 3).ToList();

                            }
                            else
                            {

                                geomsTemp = conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.Id == Object_pk).Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList();

                            }
                            geoms = geomsTemp.OrderBy(o => o.geometryName).ToList();

                            foreach (var geom in geoms)
                            {
                                var geomName = geom.geometryName ?? String.Empty;
                                var group = new GroupedFormRec() { LongGeomName = geomName, ShortGeomName = geomName, ShowButton = true, Geom = geom };
                                var recList = new List<FormRec>();
                                if (FilterBy == null || FilterBy == String.Empty || FilterBy == "Geometrie")
                                {

                                    recList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == geom.Id).Where(Record => Record.status < 3).ToList()
                                               join form in conn.Table<Form>().Where(f => f.project_fk == project.Id).ToList()
                                                            on record.formId equals form.formId
                                               join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.Id == geom.Id).Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                            on record.geometry_fk equals referenceGeometry.Id
                                               select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = record.formId, RecId = record.Id, User = record.fullUserName, GeometryName = geomName, GeomId = referenceGeometry.Id }).ToList();

                                }
                                else if (FilterBy == "Formulartyp")
                                {


                                    recList = (from record in conn.Table<Record>().Where(ReferenceGeometry => ReferenceGeometry.geometry_fk == geom.Id).Where(Record => Record.status < 3).ToList()
                                               join form in conn.Table<Form>().Where(Form => Form.title == FormName).ToList()
                                                            on record.formId equals form.formId
                                               join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.Id == geom.Id).Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                            on record.geometry_fk equals referenceGeometry.Id
                                               select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = record.formId, RecId = record.Id, User = record.fullUserName, GeometryName = geomName }).ToList();


                                }
                                foreach (var rec in recList)
                                {
                                    var title = CreateTitleStringForRecord(rec);
                                    if (title != String.Empty && title != " ") { rec.Title = title; } else { rec.Title = rec.FormType; }
                                    if (rec != null) { group.Add(rec); }
                                }
                                if (group != null) { Records.Add(group); }
                            }
                        }


                        else if (SortBy == "Formulartyp")
                        {
                            //SORT BY FORM TYPE CASE
                            var forms = new List<Form>();
                            if (FilterBy != "Formulartyp")
                            {
                                var formsTemp = conn.Table<Form>().Where(Form => Form.project_fk == project.Id).ToList();
                                forms = formsTemp.OrderBy(o => o.title).ToList();
                            }
                            else
                            {
                                var formsTemp = conn.Table<Form>().Where(Form => Form.project_fk == project.Id).Where(Form => Form.Id == Form_pk).ToList();
                                forms = formsTemp.OrderBy(o => o.title).ToList();
                            }


                            //For each form

                            foreach (var formgr in forms)
                            {
                                var group = new GroupedFormRec() { LongGeomName = formgr.title ?? "", ShortGeomName = formgr.title ?? "", ShowButton = false };
                                var recList = new List<FormRec>();
                                var recListNoGeom = new List<FormRec>();

                                if (FilterBy == null || FilterBy == String.Empty)
                                {
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

                                }
                                else if (FilterBy == "Formulartyp")
                                {
                                    recList = (from record in conn.Table<Record>().Where(Record => Record.formId == formgr.formId).Where(Record => Record.status < 3).Where(Record => Record.project_fk == project.Id).ToList()
                                               join form in conn.Table<Form>().Where(Form => Form.project_fk == project.Id).Where(Form => Form.title == FormName).ToList()
                                                            on record.formId equals form.formId
                                               join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).ToList()
                                                            on record.geometry_fk equals referenceGeometry.Id
                                               select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = form.formId, RecId = record.Id, User = record.fullUserName, GeometryName = referenceGeometry.geometryName }).ToList();
                                    recListNoGeom = (from record in conn.Table<Record>().Where(Record => Record.formId == formgr.formId).Where(Record => Record.status < 3).Where(Record => Record.geometry_fk == null).Where(Record => Record.project_fk == project.Id).ToList()
                                                     join form in conn.Table<Form>().Where(Form => Form.project_fk == project.Id).Where(Form => Form.title == FormName).ToList()
                                                                  on record.formId equals form.formId
                                                     select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = form.formId, RecId = record.Id, User = record.fullUserName, GeometryName = String.Empty }).ToList();

                                }
                                else if (FilterBy == "Geometrie")
                                {
                                    recList = (from record in conn.Table<Record>().Where(Record => Record.formId == formgr.formId).Where(Record => Record.geometry_fk == Object_pk).Where(Record => Record.status < 3).Where(Record => Record.project_fk == project.Id).ToList()
                                               join form in conn.Table<Form>().Where(Form => Form.project_fk == project.Id).Where(Form => Form.title == formgr.title).ToList()
                                                            on record.formId equals form.formId
                                               join referenceGeometry in conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.Id == Object_pk).ToList()
                                                            on record.geometry_fk equals referenceGeometry.Id
                                               select new FormRec { Timestamp = record.timestamp.ToString("g", CultureInfo.CreateSpecificCulture("de-DE")), Title = form.title, FormType = form.title, FormId = form.formId, RecId = record.Id, User = record.fullUserName, GeometryName = referenceGeometry.geometryName }).ToList();
                                }

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
                                if (group != null) { Records.Add(group); }
                            }

                        }
                        OnPropertyChanged("Records");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


            });
            */
        }


        /*
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
        private string CreateTitleStringForRecord (FormRec rec)
        {
            var title = "";
            var formFields = Form.FetchFormFields(rec.FormId);

            if (formFields != null)
            {
                foreach (var formField in formFields)
                {
                    if ( formField.typeId == 11 || formField.typeId == 51 || formField.typeId == 61 )
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
        */

        /// <summary>
        /// The record selected from the list
        /// </summary>
        public FormRec SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        /// <summary>
        /// Go to the form selection page when the add item button is pressed
        /// </summary>
        /// <param name="obj"></param>
        private async void OnAddItem(object obj)
        {
            await Shell.Current.GoToAsync(nameof(FormPage));
        }

        /// <summary>
        /// Action conducted when an item in the record list is selected
        /// </summary>
        /// <param name="item"></param>
        async void OnItemSelected(FormRec item)
        {
            if (item == null)
                return;

            // This will push the ItemDetailPage onto the navigation stack
            await Shell.Current.GoToAsync($"{nameof(FormPage)}?{nameof(FormPageVM.RecId)}={item.RecId}");
        }

        /// <summary>
        /// Copy the BDC GUID
        /// </summary>
        /// <param name="guid"></param>
        public void CopyGUID(string guid)
        {
            var bguid = "<<BDC><" + guid + ">>";
            Clipboard.SetTextAsync(bguid);
        }
    }

    /// <summary>
    /// The object defining the geometry grouping
    /// </summary>
    public class GroupedFormRec : ObservableCollection<FormRec>
    {
        public string LongGeomName { get; set; }
        public string ShortGeomName { get; set; }
        public int? GeomId { get; set; }
        public bool ShowButton { get; set; }
        public ReferenceGeometry Geom { get; set; }
    }

    /// <summary>
    /// A command for saving the BDC GUID
    /// </summary>
    public class BDCGUIDRecordCommand : ICommand
    {

        public RecordsPageVM RecordsPageViewModel { get; set; }

        public BDCGUIDRecordCommand(RecordsPageVM recordsPageVM)
        {
            RecordsPageViewModel = recordsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var rec = parameter as Record;
            var extId = rec.recordId;
            var bguid = "<<BDC><" + extId + ">>";
            Clipboard.SetTextAsync(bguid);
            MessagingCenter.Send<Application>(Application.Current, "CopiedRecordGuid");
        }
    }

    /// <summary>
    /// A command for deleting the selected record
    /// </summary>
    public class RecordDeleteCommand : ICommand
    {

        public RecordsPageVM RecordsPageViewModel { get; set; }

        public RecordDeleteCommand(RecordsPageVM recordsPageVM)
        {
            RecordsPageViewModel = recordsPageVM;
            MessagingCenter.Subscribe<RecordDeleteCommand,FormRec>(this,"DeleteRecord", async (sender,rec) =>
            {
                await Task.Delay(500);
                Record.DeleteRecord(rec.RecId);
            });
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var rec = parameter as FormRec;
            MessagingCenter.Send<RecordDeleteCommand, FormRec>(this, "DeleteRecord", rec);

        }
    }

    /// <summary>
    /// A command for creating a new record
    /// </summary>
    public class NewRecordCommand : ICommand
    {

        public RecordsPageVM RecordsPageViewModel { get; set; }

        public NewRecordCommand(RecordsPageVM recordsPageVM)
        {
            RecordsPageViewModel = recordsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var rec = parameter as FormRec;
        }
    }
}