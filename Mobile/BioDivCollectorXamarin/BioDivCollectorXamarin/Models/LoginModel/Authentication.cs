using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Xamarin.Auth;
using Xamarin.Essentials;
using Xamarin.Forms;

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

        public static async Task RequestAuthentication(string username, string password)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {

                    var urlString = "https://id.biodivcollector.ch/auth/realms/BioDivCollector/protocol/openid-connect/token";
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(urlString),
                        Method = HttpMethod.Post
                    };

                    FormUrlEncodedContent postData = new FormUrlEncodedContent(
                        new []
                        {
                            new KeyValuePair<string, string>("client_id", Authentication.AuthParams.ClientId),
                            new KeyValuePair<string, string>("grant_type", "password"),
                            new KeyValuePair<string, string>("client_secret", Authentication.AuthParams.ClientSecret),
                            new KeyValuePair<string, string>("scope", Authentication.AuthParams.Scope),
                            new KeyValuePair<string, string>("username", username),
                            new KeyValuePair<string, string>("password", password),
                        }
                    );
                    request.Content = postData;
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    
                    var task = client.SendAsync(request).ContinueWith((taskwithmsg) =>
                    {
                        var response = taskwithmsg.Result;
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var jsonTask = response.Content.ReadAsStringAsync();
                            jsonTask.Wait();
                            var jsonObject = jsonTask.Result;
                            var returnedObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonObject);  //Deserialise response
                            SaveTokens(returnedObject);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            Preferences.Set("Username", String.Empty);
                            Preferences.Set("Password", String.Empty);
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                bool ok = await App.Current.MainPage.DisplayAlert("Anmeldung fehlgeschlagen", "Benutzername oder Passwort ungültig. Versuchen Sie bitte nochmals", null, "OK");
                            });
                        }
                        else
                        {
                            Preferences.Set("Username", String.Empty);
                            Preferences.Set("Password", String.Empty);
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                bool ok = await App.Current.MainPage.DisplayAlert("Anmeldung fehlgeschlagen", "Versuchen Sie bitte nochmals" , null, "OK");
                            });
                        }
                    });
                    task.Wait();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //Delete username and password if login did not work, so that we don't get into an endless loop
                Preferences.Set("Username", String.Empty);
                Preferences.Set("Password", String.Empty);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    bool ok = await App.Current.MainPage.DisplayAlert("Anmeldung Fehlgeschlagen", "Versuchen Sie bitte nochmals", null, "OK");
                });
            }
            finally
            {
                Application.Current.MainPage = BioDivCollectorXamarin.Models.LoginModel.Login.GetPageToView();
            }
        }

        /// <summary>
        /// Get a refresh token to keep the user logged in
        /// </summary>
        public static async Task RequestRefreshTokenAsync()
        {
            
            string localRefreshToken = Preferences.Get("RefreshToken", String.Empty);

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

        /// <summary>
        /// This logs the user out again completely if there is an error
        /// </summary>
        private static void loginErrorHandler()
        {
            Login.Logout();
            Preferences.Set("Username", String.Empty);
            Preferences.Set("Password", String.Empty);
            Xamarin.Forms.MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnsuccessful");
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
