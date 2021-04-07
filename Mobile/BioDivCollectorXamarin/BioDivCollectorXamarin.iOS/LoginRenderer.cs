using System;
using System.Collections.Generic;
using BioDivCollectorXamarin;
using BioDivCollectorXamarin.iOS;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using BioDivCollectorXamarin.Models.LoginModel;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Foundation;
using UIKit;

[assembly: ExportRenderer(typeof(MainPage), typeof(LoginPageRenderer))]
namespace BioDivCollectorXamarin.iOS
{
    public class LoginPageRenderer : PageRenderer
    {
        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (true)
            {
                this.ShowLogin();
            }
        }

        private void ShowLogin()
        {
            App.ShowLogin = false;
            //Delete Cookies
            var cookieJar = NSHttpCookieStorage.SharedStorage;
            cookieJar.AcceptPolicy = NSHttpCookieAcceptPolicy.Always;
            foreach (var aCookie in cookieJar.Cookies)
            {
                cookieJar.DeleteCookie(aCookie);
            }

            //Start Authentication
            var auth = Authentication.AuthParams;
            auth.ShowErrors = false;
            auth.AllowCancel = false;

            //On authentication completion
            auth.Completed += (sender, eventArgs) =>
            {
                if (eventArgs.IsAuthenticated == true)
                {
                    Dictionary<String, String> props = eventArgs.Account.Properties;
                    Authentication.SaveTokens(props);

                    App.ShowLogin = false;

                    MessagingCenter.Send< Xamarin.Forms.Application> (Xamarin.Forms.Application.Current, "LoginSuccessful");
                }
                else
                {
                    //Re-present
                    App.ShowLogin = true;
                    this.ShowLogin();
                }
            };

            auth.Error += (sender, eventArgs) =>
            {
                //Careful! This triggers on the iPhone even when login is successful
            };



            
            var vc = auth.GetUI();
            vc.ModalPresentationStyle = UIKit.UIModalPresentationStyle.OverFullScreen;

            PresentViewController(vc, true, null);
        }
    }
}