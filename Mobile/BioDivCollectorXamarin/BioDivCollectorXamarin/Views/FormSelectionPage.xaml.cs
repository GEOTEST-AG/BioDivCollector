using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FormSelectionPage : ContentPage
    {

        public FormSelectionPageVM ViewModel { get; set; }

        /// <summary>
        /// Initialise the form selection page for a specific geometry
        /// </summary>
        /// <param name="object_pk"></param>
        public FormSelectionPage(int? object_pk)
        {
            InitializeComponent();
            ViewModel = new FormSelectionPageVM();
            if (object_pk != null)
            { ViewModel.Object_pk = object_pk; }
            BindingContext = ViewModel;
        }

        /// <summary>
        /// Deal with routes and titles on appearing
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Title = ""; //Clear title on appearing, otherwise for some reason, the back button title is "back" in iOS
            App.CurrentRoute = "//Records/FormSelection";
        }

        /// <summary>
        /// Reinstate the title on disappearing (iOS issue)
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Title = "Formulare"; //Set the title on disappearing, so that the back button shows correctly in the next page
        }

        /// <summary>
        /// Action for when the required form is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FormListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var form = e.Item as Form;
            var rec = await Record.CreateRecord(form.formId, (int?)ViewModel.Object_pk);
            //var rec = Record.CreateRecord(form.formId, (int?)ViewModel.Object_pk);
            //if (rec != null)
            //{
            //Navigation.PushAsync(new FormPage(null, form.formId, (int?)ViewModel.Object_pk),true);
            Shell.Current.GoToAsync($"Form?formid={form.formId}&geomid={(int?)ViewModel.Object_pk}&recid={rec.recordId}", true);

            //}
        }
    }
}