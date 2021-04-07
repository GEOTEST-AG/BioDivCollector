using Android.App;
using Android.Content;
using BioDivCollectorXamarin;
using BioDivCollectorXamarin.Droid;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(MainPage), typeof(LoginPageRenderer))]
namespace BioDivCollectorXamarin.Droid
{
    public class LoginPageRenderer : PageRenderer
    {
        public LoginPageRenderer(Context context)
            : base(context)
        {

        }

        /// <summary>
        /// Perform OAuth authorisation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            base.OnElementPropertyChanged(sender, e);
            
            if (App.ShowLogin)
            {
                App.ShowLogin = false;


                //Configure authentication
                var activity = this.Context as Activity;
                var auth = Authentication.AuthParams;
                auth.ShowErrors = true;
                auth.AllowCancel = false;

                //Start authenticaton
                activity.StartActivity(auth.GetUI(activity));

                //On authentication completion
                auth.Completed += async (authsender, eventArgs) =>
                {

                    if (eventArgs.IsAuthenticated == true)
                    {
                        Console.WriteLine("Halleluja!");

                        Dictionary<String, String> props = eventArgs.Account.Properties;

                        Authentication.SaveTokens(props);

                        MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginSuccessful");
                    }
                    else
                    {
                        Console.WriteLine("LOGIN FAILED!");
                        App.ShowLogin = true;
                        MessagingCenter.Send<MainPage>(new MainPage(), "LoginUnsuccessful");
                        MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnuccessful");
                    }

                };

                auth.Error += async (authsender, eventArgs) =>
                {
                    Console.WriteLine("LOGIN FAILED!");
                    App.ShowLogin = true;
                    MessagingCenter.Send<MainPage>(new MainPage(), "LoginUnsuccessful");
                    MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnuccessful");
                };

            }
        }
    }
}