using System;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class MainPageVM : BaseViewModel
    {


        public bool DidStart;
        private bool activity;
        public bool Activity
        {
            get
            {
                return activity;
            }
            set
            {
                activity = value;
            }
        }

        public Command LoginCommand { get; }
        public Command RegisterCommand { get; }
        public Command PasswordCommand { get; }

        private string username;
        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }

        private string password;
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        public bool SaveLogin;

        public MainPageVM()
        {
            //If we show the login page, set the chain up for a login to be performed
            DidStart = App.AppDidStart;
            LoginCommand = new Command(Login, ValidateLogin);
            RegisterCommand = new Command(Register, ValidateTrue);
        }

        public void OnAppearing()
        {
            Username = String.Empty;
            Password = String.Empty;
            Activity = false;

            var oldUser = Preferences.Get("Username", String.Empty);
            var oldPassword = Preferences.Get("Password", String.Empty);
            if (oldUser != null && oldUser != String.Empty && oldPassword != null && oldPassword != String.Empty)
            {
                Activity = true;
                Task.Run(async () => {
                    await Authentication.RequestAuthentication(oldUser, oldPassword);
                });
            }
        }
        
        private async void Login()
        {
            if (SaveLogin)
            {
                Preferences.Set("Username", Username);
                Preferences.Set("Password", Password);
            }
            else
            {
                Preferences.Set("Username", String.Empty);
                Preferences.Set("Password", String.Empty);
            }

            try
            {
                Activity = true;
                await Authentication.RequestAuthentication(Username, Password);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private bool ValidateLogin()
        {
            return true;
        }

        private bool ValidateTrue()
        {
            return true;
        }

        public void Register()
        {
            App.Current.MainPage =  new RegistrationPage();
         }

    }
}