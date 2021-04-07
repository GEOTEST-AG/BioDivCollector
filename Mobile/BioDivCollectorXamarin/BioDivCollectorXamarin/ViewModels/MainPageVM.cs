using BioDivCollectorXamarin.Models.LoginModel;
using Xamarin.Essentials;
using System;
using Xamarin.Forms;
using Xamarin.Auth;
using System.Threading.Tasks;

namespace BioDivCollectorXamarin.ViewModels
{
    public class MainPageVM : BaseViewModel
    {


        public bool DidStart;

        public MainPageVM()
        {
            //If we show the login page, set the chain up for a login to be performed
            DidStart = App.AppDidStart;

        }


    }
}