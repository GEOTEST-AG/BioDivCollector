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
                    BindingContext = ViewModel = new RecordsPageVM(Int32.Parse(ObjectId));
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

        }

        /// <summary>
        /// On appearing, set up state restoration and populate the text in the filter/sort buttons
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
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
        private void RecordListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var rec = e.Item as FormRec;
            Navigation.PushAsync(new FormPage(rec.RecId),true);
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
                string formAction = await DisplayActionSheet("Filtern nach", "Abbrechen", null, Form.FetchFormNamesForProject().ToArray());
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
                var geom = formRec.Geom;
                if (ViewModel.Object_pk == geom.Id)
                { ViewModel.Object_pk = null; }
                ReferenceGeometry.DeleteGeometry(geom.Id);
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
                string formAction = await DisplayActionSheet("Optionen", "Abbrechen", null, "Geometrie entfernen", "GUID Kopieren", "Geometriename editieren");
                if (formAction == "Geometrie entfernen")
                {
                    DeleteButton_Clicked(sender, e);
                }
                else if (formAction == "GUID Kopieren")
                {
                    GroupedFormRec formRec = ((Button)sender).BindingContext as GroupedFormRec;
                    var geom = formRec.Geom;
                    var extId = geom.geometryId;
                    ViewModel.CopyGUID(extId);
                    await DisplayAlert("BDC GUID kopiert", "", "OK");
                }
                else if (formAction == "Geometriename editieren")
                {
                    GroupedFormRec formRec = ((Button)sender).BindingContext as GroupedFormRec;
                    var geom = formRec.Geom;
                    string newName = await DisplayPromptAsync("Geometriename", "Editieren Sie bitte der Geometriename", accept: "OK", cancel: "Abbrechen",  initialValue: geom.geometryName, keyboard: Keyboard.Text);
                    geom.geometryName = newName;
                    geom.timestamp = DateTime.Now;
                    if (geom.status != -1)
                    {
                        geom.status = 2;
                    }
                    ReferenceGeometry.SaveGeometry(geom);
                    ViewModel.UpdateRecords();
                }
            });
        }

        /// <summary>
        /// Copy the BDC GUID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GUIDItem_Clicked(object sender, EventArgs e)
        {
            var rec = ((MenuItem)sender).CommandParameter as FormRec;
            var record = Record.FetchRecord(rec.RecId);
            var bguid = "<<BDC><" + record.recordId + ">>";
            Clipboard.SetTextAsync(bguid);
            MessagingCenter.Send<Application>(Application.Current, "CopiedRecordGuid");
        }

        /// <summary>
        /// Navigate to the form selection page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Clicked(object sender, EventArgs e)
        {

            var button = sender as Button;
            var geom = button.CommandParameter as ReferenceGeometry;
            if (geom != null)
            {
                Navigation.PushAsync(new FormSelectionPage((int?)geom.Id),true);
            }
            else
            {
                Navigation.PushAsync(new FormSelectionPage(null),true);
            }
            
        }

    }
}