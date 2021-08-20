using Xamarin.Forms;
using BioDivCollectorXamarin.Models.LoginModel;
using System;
using Xamarin.Essentials;
using System.Threading.Tasks;

namespace BioDivCollectorXamarin.ViewModels
{
    public class MainPageVM : BaseViewModel
    {


        public bool DidStart;

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
            PasswordCommand = new Command(ChangePassword, ValidateTrue);
        }

        public void OnAppearing()
        {
            Username = String.Empty;
            Password = String.Empty;

            var oldUser = Preferences.Get("Username", String.Empty);
            var oldPassword = Preferences.Get("Password", String.Empty);
            if (oldUser != String.Empty && oldPassword != String.Empty)
            {
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
            Device.OpenUri(new Uri("https://id.biodivcollector.ch/auth/realms/BioDivCollector/login-actions/registration?client_id=BioDivCollector&tab_id=PPOcNlpFwpI"));
        }

        public void ChangePassword()
        {
            Device.OpenUri(new Uri("https://id.biodivcollector.ch/auth/realms/BioDivCollector/login-actions/authenticate?client_id=BioDivCollector&tab_id=1uPJiY4Fhyw"));
        }
    }
}