using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Xamarin.Auth;
using Xamarin.Essentials;

namespace BioDivCollectorXamarin.Models.LoginModel
{
    public class Authentication
    {

        private static MyOAuth2Authenticator authParams;
        public static MyOAuth2Authenticator AuthParams
        {
            get
            {
                //Parse the authorisation xml file and return the MyOAuth2Authenticator object
                var auths = LoadXMLData();
                var auth = auths[0];
                var authenticator = new MyOAuth2Authenticator(
                    clientId: auth.ClientId,
                    clientSecret: auth.ClientSecret,
                    scope: auth.Scope,
                    authorizeUrl: auth.AuthorizeUrl,
                    redirectUrl: auth.RedirectUrl,
                    accessTokenUrl: auth.AccessTokenUrl
                );
                return authenticator;


                // Project must contain an xml file with the structure:
                //<? xml version = "1.0" encoding = "UTF-8" ?>
                //< Servers >
                //< AuthenticationServer clientId = "xxx" clientSecret = "xxx" scope = "xxx" authorizeUrl = "xxx" redirectUrl = "xxx" accessTokenUrl = "xxx" />
                //</ Servers >
            }
            set
            {
                authParams = value;
            }
        }

        /// <summary>
        /// Parse the Auth.xml file
        /// </summary>
        /// <returns></returns>
        public static List<Auth> LoadXMLData()
        {

            List<Auth> rawData = null;
            var assembly = typeof(Authentication).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream(App.AuthParams);

            XDocument doc = XDocument.Load(stream);
            IEnumerable<Auth> auths = from s in doc.Descendants("AuthenticationServer")
                                      select new Auth
                                      {
                                          ClientId = s.Attribute("clientId").Value,
                                          ClientSecret = s.Attribute("clientSecret").Value,
                                          Scope = s.Attribute("scope").Value,
                                          AuthorizeUrl = new Uri(s.Attribute("authorizeUrl").Value),
                                          RedirectUrl = new Uri(s.Attribute("redirectUrl").Value),
                                          AccessTokenUrl = new Uri(s.Attribute("accessTokenUrl").Value)
                                      };
            rawData = auths.ToList();

            return rawData;

        }

        /// <summary>
        /// Get a refresh token to keep the user logged in
        /// </summary>
        public static async Task RequestRefreshTokenAsync()
        {
            
            string localRefreshToken = Preferences.Get("RefreshToken", "");

            var auth = Authentication.AuthParams;
           
            auth.Completed += (sender, eventArgs) =>
            {
                if (eventArgs.IsAuthenticated == true)
                {
                    Console.WriteLine("Refreshed token");
                    try
                    {
                        Dictionary<String, String> props = eventArgs.Account.Properties;

                        Authentication.SaveTokens(props);

                        Xamarin.Forms.MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginSuccessful");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Authentication.loginErrorHandler();
                    }

                }
                else
                {
                    Authentication.loginErrorHandler();
                }
            };

            auth.Error += (sender, eventArgs) =>
            {
                Authentication.loginErrorHandler();
            };

            try
            {
                var valid = await auth.RequestRefreshTokenAsync(localRefreshToken);
                Console.WriteLine("New token valid for: " + valid);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Authentication.loginErrorHandler();
            }
            
        }

        private static void loginErrorHandler()
        {
            Login.Logout();
            Xamarin.Forms.MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnuccessful");
        }

        /// <summary>
        /// Once authentication is complete, save the returned parameters to preferences
        /// </summary>
        /// <param name="props"></param>
        public static void SaveTokens(Dictionary<string,string>props)
        {

            //Access Token
            Preferences.Set("AccessToken", props["access_token"]);

            //Access Token Expiry
            string accessExpiryPeriodString = props["expires_in"];
            int accessExpiryPeriod = Convert.ToInt32(accessExpiryPeriodString);
            var accessExpiry = DateTime.UtcNow.AddSeconds(accessExpiryPeriod);
            Preferences.Set("AccessTokenExpiry", accessExpiry.ToString());

            //Refresh Token
            Preferences.Set("RefreshToken", props["refresh_token"]);

            //Refresh Token Expiry
            string refreshExpiryPeriodString = props["refresh_expires_in"];
            int refreshExpiryPeriod = Convert.ToInt32(refreshExpiryPeriodString);
            var refreshExpiry = DateTime.UtcNow.AddSeconds(refreshExpiryPeriod);
            Preferences.Set("RefreshTokenExpiry", refreshExpiry.ToString());

            //User
            var serUser = JsonConvert.SerializeObject(App.CurrentUser);
            Preferences.Set("User", serUser);
        }
    }

    /// <summary>
    /// Subclass to add the nonce parameter
    /// </summary>
    public class MyOAuth2Authenticator : OAuth2Authenticator
    {
        public MyOAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl, GetUsernameAsyncFunc getUsernameAsync = null, bool isUsingNativeUI = false) : base(clientId, scope, authorizeUrl, redirectUrl, getUsernameAsync, isUsingNativeUI)
        {
        }

        public MyOAuth2Authenticator(string clientId, string clientSecret, string scope, Uri authorizeUrl, Uri redirectUrl, Uri accessTokenUrl, GetUsernameAsyncFunc getUsernameAsync = null, bool isUsingNativeUI = false) : base(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, getUsernameAsync, isUsingNativeUI)
        {
        }

        protected override void OnCreatingInitialUrl(IDictionary<string, string> query)
        {
            query.Add("nonce", Guid.NewGuid().ToString("N"));
        }
    }

}
