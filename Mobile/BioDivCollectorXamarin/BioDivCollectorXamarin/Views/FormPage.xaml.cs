using System.ComponentModel;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FormPage : ContentPage
    {
        FormPageVM ViewModel;
        public int? RecId;
        public int FormId;
        public int? GeomId;

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
            App.CurrentRoute = "//Records/Form";
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
            MessagingCenter.Subscribe<FormPageVM>(this, "NavigateBack", (sender) =>
            {
                NavigateBack();
            });
            App.CurrentRoute = "//Records/Form";
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