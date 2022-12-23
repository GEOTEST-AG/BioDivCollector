using System;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.ViewModels;
using Xamarin.Essentials;
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
                //int.TryParse(value, out int parsedValue);
                RecId = value;
                Initialise();
                if (ViewModel != null) { ViewModel.RecId = value; }
            }
        }
        public string FormIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                FormId = parsedValue;
                Initialise();
                if (ViewModel != null) { ViewModel.FormId = parsedValue; }
            }
        }
        public string GeomIdString
        {
            set
            {
                int.TryParse(value, out int parsedValue);
                GeomId = parsedValue;
                Initialise();
                if (ViewModel != null) { ViewModel.GeomId = parsedValue; }
            }
        }
        public string RecId { get; set; }
        public int FormId { get; set; }
        public int? GeomId { get; set; }

        /// <summary>
        /// Try to initialise the page with a default constructor
        /// </summary>
        public FormPage()
        {
            InitializeComponent();
            //ViewModel = new FormPageVM(RecId, FormId, GeomId, Navigation);
            //BindingContext = ViewModel;
            //RecId = ViewModel.RecId;
            MessagingCenter.Subscribe<Application>(App.Current, "NavigateBack", (sender) =>
            {
                NavigateBack();
            });
            MessagingCenter.Unsubscribe<Application>(App.Current, "UpdateDataForm");
            MessagingCenter.Subscribe<Application>(App.Current, "UpdateDataForm", (sender) =>
            {
                UpdateFormView();
            });
        }



        /// <summary>
        /// Initialise the form for a specific record
        /// </summary>
        /// <param name="recId"></param>
        public FormPage(string recId, int formId, int? geomId)
        {
            InitializeComponent();
            RecId = recId;
            FormId = formId;
            GeomId = geomId;
            //ViewModel = new FormPageVM(recId, formId, geomId, Navigation);
            //BindingContext = ViewModel;
            //RecId = recId = ViewModel.RecId;
            //UpdateFormView();

            MessagingCenter.Subscribe<Application>(App.Current, "NavigateBack", (sender) =>
            {
                NavigateBack();
            });
            MessagingCenter.Subscribe<Application>(App.Current, "PhotoDeleted", (sender) =>
            {
                OnAppearing();
            });
        }

        private async void Initialise()
        {
            if (FormId != 0 && RecId != null && GeomId != null && FormId != null)
            {
                //var form = await Form.FetchFormOfType(FormId);
                //if (form != null)
                //{
                    var makeFormTask = Task.Run(() =>
                    {
                        ViewModel = new FormPageVM(RecId, FormId, GeomId, Navigation);
                        BindingContext = ViewModel;
                    });

                    await makeFormTask;


                //}

                if (RecId == String.Empty)
                {
                    RecId = ViewModel.RecId; //Take RecId from viewmodel - this is then created in viewmodel if it started off empty.
                }
            }
        }

        /// <summary>
        /// Add the form entry UI elements on appearing
        /// </summary>
        protected override void OnAppearing()
        {
            //ViewModel.OnAppearing();
        }

        private void UpdateFormView()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (ViewModel != null)
                {
                    FormElementStack.Children.Clear();

                    foreach (var view in ViewModel.Assets)
                    {
                        FormElementStack.Children.Add(view);
                    }
                }
            });
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