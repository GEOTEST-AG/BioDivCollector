using System.ComponentModel;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [QueryProperty(nameof(RecIdString), "recid")]
    [QueryProperty(nameof(GeomIdString), "geomid")]
    [QueryProperty(nameof(FormIdString), "formid")]
    public partial class FormPage : ContentPage
    {
        FormPageVM ViewModel;
        public string RecIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                RecId = parsedValue;
                if (ViewModel != null) { ViewModel.RecId = parsedValue; }
            }
        }
        public string FormIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                FormId = parsedValue;
                if (ViewModel != null) { ViewModel.FormId = parsedValue; }
            }
        }
        public string GeomIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                GeomId = parsedValue;
                if (ViewModel != null) { ViewModel.GeomId = parsedValue; }
            }
        }
        public int? RecId { get; set; }
        public int FormId { get; set; }
        public int? GeomId { get; set; }

        /// <summary>
        /// Try to initialise the page with a default constructor
        /// </summary>
        public FormPage()
        {
            InitializeComponent();
            ViewModel = new FormPageVM(RecId, FormId, GeomId, Navigation);
            BindingContext = ViewModel;
            RecId = ViewModel.RecId;
            MessagingCenter.Subscribe<FormPageVM>(this, "NavigateBack", (sender) =>
            {
                NavigateBack();
            });
        }

        /// <summary>
        /// Initialise the form for a specific record
        /// </summary>
        /// <param name="recId"></param>
        public FormPage(int? recId, int formId, int? geomId)
        {
            InitializeComponent();
            RecId = recId;
            FormId = formId;
            GeomId = geomId;
            ViewModel = new FormPageVM(recId, formId, geomId, Navigation);
            BindingContext = ViewModel;
            RecId = recId = ViewModel.RecId;
            MessagingCenter.Subscribe<FormPageVM>(ViewModel, "NavigateBack", (sender) =>
            {
                NavigateBack();
            });
            MessagingCenter.Subscribe<FormPageVM>(ViewModel, "PhotoDeleted", (sender) =>
            {
                OnAppearing();
            });
        }

        /// <summary>
        /// Add the form entry UI elements on appearing
        /// </summary>
        protected override void OnAppearing()
        {
            FormElementStack.Children.Clear();
            ViewModel.OnAppearing();
            foreach (var view in ViewModel.Assets)
            {
                FormElementStack.Children.Add(view);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }

        /// <summary>
        /// Return to the bottom of the tab stack
        /// </summary>
        private void NavigateBack()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Navigation.PopToRootAsync();
            });
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }
    }
}