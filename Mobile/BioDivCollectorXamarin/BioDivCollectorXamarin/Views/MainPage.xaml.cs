using BioDivCollectorXamarin.ViewModels;
using Xamarin.Forms;
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

        void CheckBox_CheckedChanged(System.Object sender, Xamarin.Forms.CheckedChangedEventArgs e)
        {
            //Have to use an event handler as binding directly is not an option
            ViewModel.SaveLogin = e.Value;
        }

    }

}
