using System.Threading.Tasks;
using BioDivCollectorXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Auth;
using System;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {

        MainPageVM ViewModel;

        public MainPage()
        {
            InitializeComponent();

            ViewModel = new MainPageVM();
            BindingContext = ViewModel;

            MessagingCenter.Subscribe<MainPage>(this, "LoginUnsuccessful", (sender) => {
                DisplayAlert("Login Unsuccessful", "Username or password not recognised", "OK");
            });

        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.OnAppearing();
        }


    }
}
