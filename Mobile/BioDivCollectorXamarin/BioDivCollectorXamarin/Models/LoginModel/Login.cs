using BioDivCollectorXamarin.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models.LoginModel
{
    class Login
    {
        /// <summary>
        /// Get details of the current user from the connector
        /// </summary>
        public static void GetUserDetails()
        {
            var jsonObject = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var urlString = App.ServerURL + "/api/User";
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(urlString),
                        Method = HttpMethod.Get,
                    };

                    var token = Preferences.Get("AccessToken", "");


                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var task = client.SendAsync(request).ContinueWith((taskwithmsg) =>
                    {
                        var response = taskwithmsg.Result;
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var jsonTask = response.Content.ReadAsStringAsync();
                            jsonTask.Wait();
                            jsonObject = jsonTask.Result;

                            App.BioDivPrefs.Set("User", jsonObject);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                bool ok = await App.Current.MainPage.DisplayAlert("Anmeldung fehlgeschlagen", "Benutzername oder Passwort ungültig. Versuchen Sie bitte nochmals", null, "OK");
                            });
                        }
                        else
                        {
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                bool ok = await App.Current.MainPage.DisplayAlert("Anmeldung fehlgeschlagen", "Versuchen Sie bitte nochmals", null, "OK");
                            });
                        }

                    });
                    task.Wait();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Device.BeginInvokeOnMainThread(async () =>
                {
                    bool ok = await App.Current.MainPage.DisplayAlert("Anmeldung Fehlgeschlagen", "Versuchen Sie bitte nochmals", null, "OK");
                });
            }
            finally
            {
                
                try
                {
                    User user = JsonConvert.DeserializeObject<User>(jsonObject);
                    App.BioDivPrefs.Set("User", jsonObject);
                    App.CurrentUser = User.RetrieveUser();
                }
                catch
                {
                    App.CurrentUser = new User();
                }
                
                App.CurrentUser.LoggedIn = true;

                App.ShowLogin = false;
            }
        }

        /// <summary>
        /// Check whether to display the login page or the rest of the app
        /// </summary>
        /// <returns></returns>
        public static Xamarin.Forms.Page GetPageToView()
        {


            //Get the last user and when the last login was
            if (App.CurrentUser == null)
            {
                App.CurrentUser = new Models.LoginModel.User();
                User.RetrieveUser();
            }

            //Offline Access
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                if (App.CurrentUser == null)
                {
                    //Tell the user that they need to go online
                    return new Offline();
                }
                else
                {
                    //Log straight in if we have a user
                    return new AppShell();
                }
            }
            else
            {
                string currentTime = DateTime.UtcNow.ToString();
                string refreshExpiry = Preferences.Get("RefreshTokenExpiry", currentTime);
                try
                {
                    DateTime refreshTokenExpiry = DateTime.Parse(refreshExpiry);

                    if (refreshTokenExpiry <= DateTime.UtcNow)
                    {

                        //Return login page
                        App.ShowLogin = true;

                        var oldUser = Preferences.Get("Username", String.Empty);
                        var oldPassword = Preferences.Get("Password", String.Empty);
                        if (oldUser != String.Empty && oldPassword != String.Empty)
                        {
                            Task.Run(async () => {
                                await Authentication.RequestAuthentication(oldUser, oldPassword);
                            });
                            return new LoginPage();
                        }
                        else
                        {
                            return new MainPage();
                        }
                        

                    }
                    else
                    {
                        //Use refresh token and show a page whilst we are waiting for the token to return

                        Authentication.RequestRefreshTokenAsync();
                        return new LoginPage();
                    }
                }
                catch (Exception ex)
                {
                    //Use refresh token and show a page whilst we are waiting for the token to return

                    Authentication.RequestRefreshTokenAsync();
                    return new LoginPage();
                }

            }
        }

        /// <summary>
        /// Update the login token
        /// </summary>
        public static async void CheckLogin()
        {
            //Get the last user and when the last login was
            if (App.CurrentUser == null)
            {
                App.CurrentUser = new Models.LoginModel.User();
                User.RetrieveUser();
            }

            //OFfline Access
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                string currentTime = DateTime.UtcNow.ToString();
                string refreshExpiry = Preferences.Get("RefreshTokenExpiry", currentTime);
                DateTime refreshTokenExpiry = DateTime.Parse(refreshExpiry);

                if (refreshTokenExpiry <= DateTime.UtcNow)
                {
                    //Log out if timed out
                    MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnsuccessful");
                }
                else
                {
                    //request new token if not timed out
                    await Authentication.RequestRefreshTokenAsync();
                }
            }
        }

        /// <summary>
        /// Log the user out (delete the session on the auth server) and delete the authentication cookies
        /// </summary>
        public static async void Logout()
        {
            try
            {
                
                var auth = Authentication.AuthParams;

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10); // 10 seconds
                    var token = Preferences.Get("AccessToken", "");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    Preferences.Set("Username", String.Empty);
                    Preferences.Set("Password", String.Empty);

                    var values = new Dictionary<string, string>
                    {
                        { "client_id", auth.ClientId },
                        { "client_secret", auth.ClientSecret },
                        { "refresh_token", Preferences.Get("RefreshToken", "") }
                    };
                    var content = new FormUrlEncodedContent(values);

                    var response = await client.PostAsync(new Uri(App.LogoutURL), content);  //UPLOAD
                    var jsonbytes = await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Preferences.Set("AccessToken", "");
            Preferences.Set("AccessTokenExpiry", DateTime.UtcNow.ToString());
            Preferences.Set("RefreshToken", "");
            Preferences.Set("RefreshTokenExpiry", DateTime.UtcNow.ToString());

            //Delete cookies
#if __IOS__
            // iOS-specific code
            //Delete Cookies
            var cookieJar = NSHttpCookieStorage.SharedStorage;
            cookieJar.AcceptPolicy = NSHttpCookieAcceptPolicy.Always;
            foreach (var aCookie in cookieJar.Cookies)
            {
                cookieJar.DeleteCookie(aCookie);
            }
#endif
#if __ANDROID__
            // Android-specific code

                //Delete cookies
                Android.Webkit.CookieManager.Instance.RemoveSessionCookie();
                Android.Webkit.CookieManager.Instance.RemoveAllCookie();
                Android.Webkit.CookieManager.Instance.Flush();
#endif

            Xamarin.Forms.MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnsuccessful");
        }
    }
}
