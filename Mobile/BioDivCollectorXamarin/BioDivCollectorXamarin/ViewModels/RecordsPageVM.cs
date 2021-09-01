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
                bool isnew = (value != object_pk);
                object_pk = value;
                Preferences.Set("FilterGeometry", value.ToString());
                FilterBy = "Geometrie";
                if (isnew) { UpdateRecords(); }
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
        /// A string used as an activity indicator
        /// </summary>
        private bool activity;
        public bool Activity
        {
            get { return activity; }
            set
            {
                activity = value;
                OnPropertyChanged("Activity");
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
            Activity = false;
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
            Activity = false;
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
            Activity = false;
            IsBusy = true;
            SelectedItem = null;

            Task.Run(async() =>
            {
                Activity = true;
                App.RecordLists.CreateRecordLists();
            });
        }

        /// <summary>
        /// Based on the current sorting and filtering methods, read the groups into which the records should be sorted, then read the records associated with each of these groups. When reading the records, we also need to know their associated forms and geometries
        /// </summary>
        public void UpdateRecords()
        {
            Activity = true;
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

                else if (FilterBy == "Geometrie" && Object_pk != null)
                {
                    var obj = ReferenceGeometry.GetGeometry((int)Object_pk);
                    foreach (var group in App.RecordLists.RecordsByGeometry)
                    {
                        try
                        {
                                if (group.GeomId == obj.Id)
                                {
                                    recs.Add(group);
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
                        var newGroup = new GroupedFormRec();
                        newGroup.LongGeomName = group.LongGeomName;
                        newGroup.ShortGeomName = group.ShortGeomName;
                        newGroup.GeomId = group.GeomId;
                        newGroup.ShowButton = group.ShowButton;
                        newGroup.Geom = group.Geom;

                        foreach (FormRec form in group)
                        {
                            if (form.GeomId == obj.Id)
                            {
                                newGroup.Add(form);
                            }
                        }
                        recs.Add(newGroup);
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
            Activity = false;
        }


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