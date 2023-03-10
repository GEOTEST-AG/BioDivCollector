using System;
using System.Threading.Tasks;
using BioDivCollectorXamarin.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProjectsPage : ContentPage
    {

        ProjectsPageVM ViewModel;

        /// <summary>
        /// Initialise the project page and listen for actions
        /// </summary>
        public ProjectsPage()
        {
            InitializeComponent();
            ViewModel = new ProjectsPageVM();
            BindingContext = ViewModel;

            MessagingCenter.Subscribe<LogoutCommand>(this, "ShowUserChoice", (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ShowUserChoice();
                });
                
            });


            MessagingCenter.Subscribe<Xamarin.Forms.Application>(App.Current, "RemoveNavBar", (sender) =>
            {
                Task.Delay(1000).Wait();
                Device.BeginInvokeOnMainThread(() =>
                {
                    Xamarin.Forms.NavigationPage.SetHasNavigationBar(this, false);
                });
            });

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(App.Current, "GuidCopied", (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    GuidCopied();
                });
            });
        }

        /// <summary>
        /// On appearing, deal with iOS safe areas
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.CurrentRoute = "//Projects";
            Device.BeginInvokeOnMainThread(async() =>
                {
                    var safeInsets = On<iOS>().SafeAreaInsets();
                    Padding = safeInsets;
                    ViewModel.OnAppearing();
                    await DisplayProjectWarningAsync();
                });
        }

        /// <summary>
        /// Perform last minute UI settings if required
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height); 
        }
        
        /// <summary>
        /// Show user account options
        /// </summary>
        private async void ShowUserChoice()
        {
            string action = await DisplayActionSheet("Willkommen " + App.CurrentUser.firstName, "Cancel", "Abmelden");
            if (action == "Abmelden")
            {
                ViewModel.Logout();
            }
        }

        /// <summary>
        /// Show a notification if no project is selected
        /// </summary>
        /// <returns></returns>
        private async Task DisplayProjectWarningAsync()
        {
            var projId = Preferences.Get("currentProject", "");
            if (projId == null || projId == String.Empty)
            {
                await DisplayAlert("Projekt synchronisieren", "Synchronisieren Sie bitte ein Projekt aus der Liste", "OK");
            }
        }

        /// <summary>
        /// Navigate to the project list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProjectSelectionButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ProjectListPage(),true);
        }

        /// <summary>
        /// Show a confirmation that the GUID has been copied
        /// </summary>
        private void GuidCopied()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("BDC GUID kopiert", "", "OK");
            });
        }
    }
}