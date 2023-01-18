using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Views;
using BioDivCollectorXamarin.ViewModels;
using BioDivCollectorXamarin.Models.DatabaseModel;
using System.Globalization;
using Xamarin.Essentials;
using NetTopologySuite.Index.HPRtree;

namespace BioDivCollectorXamarin.Views
{
    [QueryProperty("ObjectId","objectId")]
    public partial class RecordsPage : ContentPage
    {
        RecordsPageVM ViewModel;

        /// <summary>
        /// The geometry to which the records list is filtered
        /// </summary>
        private string objectId;
        public string ObjectId
        {
            get { return objectId; }
            set { 
                if (value != null)
                {
                    objectId = Uri.UnescapeDataString(value);
                    ViewModel.Object_pk = Int32.Parse(objectId);
                }
            }
        }

        /// <summary>
        /// Initialise the records list, specifying either a specific geometry, or leaving the list unfiltered
        /// </summary>
        public RecordsPage()
        {
            InitializeComponent();
            try
            {
                if (ObjectId != null && ObjectId != String.Empty)
                {
                    BindingContext = ViewModel = new RecordsPageVM(Int32.Parse(ObjectId), Navigation);
                }
                else
                {
                    BindingContext = ViewModel = new RecordsPageVM();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                BindingContext = ViewModel = new RecordsPageVM();
            }

            MessagingCenter.Subscribe<Application>(Application.Current, "CopiedRecordGuid", async (sender) =>
            {
                await Task.Delay(500);
                await DisplayAlert("BDC GUID kopiert", String.Empty, "OK");
            });

            MessagingCenter.Subscribe<MapPageVM, string>(this, "GenerateNewForm", async (sender, geomId) =>
            {
                var formList = await Form.FetchFormsForProject();
                int i = formList.Count;

                var geom = await ReferenceGeometry.GetGeometry(geomId);

                AddFormToNewGeometry(i, formList, geom, geomId);
                MessagingCenter.Unsubscribe<MapPageVM>(this, "GenerateNewForm");
            });

            MessagingCenter.Unsubscribe<Application>(App.Current, "SetBackSortBy");
            MessagingCenter.Subscribe<Application>(App.Current, "SetBackSortBy", (sender) =>
            {
                FiltrierenButton.Text = "Filtern nach";
                ViewModel.FilterBy = String.Empty;
                ViewModel.UpdateRecords();
            });
        }

        /// <summary>
        /// On appearing, set up state restoration and populate the text in the filter/sort buttons
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            MessagingCenter.Send<Application>(App.Current, "RecordsPageReady");
            if (App.CurrentRoute.Length < 11 || App.CurrentRoute.Substring(0,10) != "///Records" )
            {
                App.CurrentRoute = "//Records";
            }
            ViewModel.OnAppearing();

            if (ViewModel.SortBy == null || ViewModel.SortBy == String.Empty)
            {
                SortierenButton.Text = "Sortiert nach Geometrie";
            }
            else
            {
                SortierenButton.Text = "Sortiert nach " + ViewModel.SortBy;
            }

            if (ViewModel.FilterBy == null || ViewModel.FilterBy == String.Empty)
            {
                FiltrierenButton.Text = "Filtern nach ";
            }
            else
            {
                FiltrierenButton.Text = "Gefiltert nach " + ViewModel.FilterBy;
            }
        }

        /// <summary>
        /// On disappearing, deal with peculiarities in the iOS back button title
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Title = "Beobachtungen";
            NavigationPage.SetBackButtonTitle(this, "Beobachtungen");
        }

        /// <summary>
        /// Navigate to a form when a record entry is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RecordListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var rec = e.Item as FormRec;
            //Navigation.PushAsync(new FormPage(rec.RecId, rec.FormId, rec.GeomId),true);
            await Shell.Current.GoToAsync($"Form?recid={rec.RecId}&formid={rec.FormId}&geomid={rec.GeomId}", true); 
        }

        /// <summary>
        /// Allow the user to define how the list is sorted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SortierenButton_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Sortieren nach", "Abbrechen", null, "Geometrie", "Formulartyp");
            if (action == "Abbrechen")
            { }
            else
            { 
                ViewModel.SortBy = action;
                SortierenButton.Text = "Sortiert nach " + action;
            }
            ViewModel.UpdateRecords();
        }

        /// <summary>
        /// Allow the user to define how the list is filtered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FiltrierenButton_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Filtern nach", "Abbrechen", "kein Filter", "Geometrie", "Formulartyp");
            string filteredBy = ViewModel.FilterBy;
            if (action == "kein Filter")
            {
                ViewModel.Object_pk = null;
                ObjectId = null;
                ViewModel.FilterBy = String.Empty;
                FiltrierenButton.Text = "Filtern nach";
            }
            else if (action == "Abbrechen")
            {
                ViewModel.FilterBy = filteredBy;
            }
            else if (action == "Geometrie")
            {
                FiltrierenButton.Text = "Gefiltert nach Geometrie";
                ViewModel.FilterBy = "Geometrie";
                App.CurrentRoute = "//Records/Geometries";
                await Shell.Current.GoToAsync("//Records/Geometries", true);

            }
            else if (action == "Formulartyp")
            {
                ViewModel.FilterBy = "Formulartyp";
                var formNames = await Form.FetchFormNamesForProject();
                var formNamesArray = formNames.ToArray();
                string formAction = await DisplayActionSheet("Filtern nach", "Abbrechen", null, formNamesArray);
                if (formAction != "Abbrechen")
                {
                    ViewModel.FormName = formAction;
                    FiltrierenButton.Text = "Gefiltert nach Formulartyp";
                }
                else
                {
                    ViewModel.FilterBy = filteredBy;
                }
            }
            else { ViewModel.FilterBy = action; }
            ViewModel.UpdateRecords();
        }

        /// <summary>
        /// Delete a geometry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteButton_Clicked(object sender, EventArgs e)
        {
            string formAction = await DisplayActionSheet("Möchten Sie diese Geometrie vom Gerät entfernen?", "Abbrechen", "Entfernen");
            if (formAction == "Entfernen")
            {
                GroupedFormRec formRec = ((Button)sender).BindingContext as GroupedFormRec;
                if (formRec.GeomId != null)
                {
                    if (ViewModel.Object_pk == formRec.GeomId)
                    { ViewModel.Object_pk = null; }
                    await ReferenceGeometry.DeleteGeometry((int)formRec.GeomId);
                    MessagingCenter.Send<Application>(App.Current, "RefreshGeometries");
                }
            }

        }

        /// <summary>
        /// Show a list of further actions (including copying the guid)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GUIDButton_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                string formAction = await DisplayActionSheet("Optionen", "Abbrechen", null, "Geometrie entfernen", "GUID Kopieren", "Geometriename editieren", "Geometrie editieren");
                if (formAction == "Geometrie entfernen")
                {
                    DeleteButton_Clicked(sender, e);
                }
                else if (formAction == "GUID Kopieren")
                {
                    try
                    {
                        GroupedFormRec formRec = ((Button)sender).BindingContext as GroupedFormRec;
                        var geom = await ReferenceGeometry.GetGeometry((int)formRec.GeomId);
                        var extId = geom.geometryId;
                        ViewModel.CopyGUID(extId);
                        await DisplayAlert("BDC GUID kopiert", "", "OK");
                    }
                    catch
                    {

                    }
                }
                else if (formAction == "Geometriename editieren")
                {
                    try
                    {
                        GroupedFormRec formRec = ((Button)sender).BindingContext as GroupedFormRec;
                        var geom = await ReferenceGeometry.GetGeometry((int)formRec.GeomId);
                        string newName = await DisplayPromptAsync("Geometriename", "Editieren Sie bitte der Geometriename", accept: "OK", cancel: "Abbrechen", initialValue: geom.geometryName, keyboard: Keyboard.Text);
                        if (newName != null) { geom.ChangeGeometryName(newName); }
                        MessagingCenter.Send<Application>(App.Current, "RefreshGeometries");
                    }
                    catch
                    {

                    }
                    
                }
                else if (formAction == "Geometrie editieren")
                {
                    SendGeometryToMapForEditing((Button)sender);
                    App.CurrentRoute = "//Map";
                    await Shell.Current.GoToAsync($"//Map", true);
                }
            });
        }

        private void SendGeometryToMapForEditing(Button sender)
        {
            GroupedFormRec formRec = ((Button)sender).BindingContext as GroupedFormRec;
            if (formRec.GeomId != null)
            {
                MessagingCenter.Send<Application, int>(Application.Current, "EditGeometry", (int)formRec.GeomId);
            }
        }

        /// <summary>
        /// Copy the BDC GUID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GUIDItem_Clicked(object sender, EventArgs e)
        {
            var rec = ((MenuItem)sender).CommandParameter as FormRec;
            var record = await Record.FetchRecord(rec.RecId);
            var bguid = "<<BDC><" + record.recordId + ">>";
            await Clipboard.SetTextAsync(bguid);
            MessagingCenter.Send<Application>(Application.Current, "CopiedRecordGuid");
        }

        /// <summary>
        /// Navigate to the form selection page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddButton_Clicked(object sender, EventArgs e)
        {
            if (ViewModel.SortBy == "Formulartyp")
            {
                var button = sender as Button;
                var formId = button.CommandParameter as int?;


                var geometryList = await ReferenceGeometry.GetAllGeometries();
                int i = geometryList.Count;

                if (i == 1)
                {
                    //Navigation.PushAsync(new FormPage(null, formList.First().formId, (int?)geomId), true);
                    Shell.Current.GoToAsync($"Form?recid=&formid={(int?)formId}&geomid={geometryList.First().Id}", true);
                }
                else
                {
                    if (formId != null)
                    {
                        await Navigation.PushAsync(new GeomSelectionPage((int?)formId), true);
                    }
                    else
                    {
                        await Navigation.PushAsync(new GeomSelectionPage(null), true);
                    }
                }
            }
            else
            {
                var button = sender as Button;
                var geomId = button.CommandParameter as int?;

                var formList = await Form.FetchFormsForProject();
                int i = formList.Count;

                if (i == 1)
                {
                    //Navigation.PushAsync(new FormPage(null, formList.First().formId, (int?)geomId), true);
                    Shell.Current.GoToAsync($"Form?recid=&formid={formList.First().formId}&geomid={(int?)geomId}", true);
                }
                else
                {
                    if (geomId != null)
                    {
                        await Navigation.PushAsync(new FormSelectionPage((int?)geomId), true);
                    }
                    else
                    {
                        await Navigation.PushAsync(new FormSelectionPage(null), true);
                    }
                }
            }
        }

        /// <summary>
        /// Navigate to the form or form selection page if a new geometry is added
        /// </summary>
        /// <param name="i"></param>
        /// <param name="formList"></param>
        /// <param name="geom"></param>
        /// <param name="geomId"></param>
        void AddFormToNewGeometry(int i, List<Form> formList, ReferenceGeometry geom, string geomId)
        {
            int geomId2 = geom.Id;
            if (i == 1)
            {
                var formid = formList.FirstOrDefault().formId;


                if (formid != null)
                {
                    //Navigation.PushAsync(new FormPage(null, formid, geomId2), true);
                    Shell.Current.GoToAsync($"Form?recid=&formid={formid}&geomid={geomId2}", true);
                }
            }
            else
            {
                if (geomId == null)
                {
                    Navigation.PushAsync(new FormSelectionPage(null), true);
                }
                else
                {
                    Navigation.PushAsync(new FormSelectionPage(geomId2), true);
                }
            }
        }
    }
}