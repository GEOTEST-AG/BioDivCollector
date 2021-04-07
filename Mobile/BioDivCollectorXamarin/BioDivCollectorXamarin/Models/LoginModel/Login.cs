using BioDivCollectorXamarin.Views;
using Newtonsoft.Json;
using System;
using System.Net.Http;
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

                        var jsonTask = response.Content.ReadAsStringAsync();
                        jsonTask.Wait();
                        jsonObject = jsonTask.Result;
                        
                        App.BioDivPrefs.Set("User", jsonObject);
                    });
                    task.Wait();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

            //OFfline Access
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
                DateTime refreshTokenExpiry = DateTime.Parse (refreshExpiry);

                if (refreshTokenExpiry <= DateTime.UtcNow)
                {

                    //Return login page
                    App.ShowLogin = true;
                    return new MainPage();
                    
                }
                else
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
                    MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnuccessful");
                }
                else
                {
                    //request new token if not timed out
                    await Authentication.RequestRefreshTokenAsync();
                }
            }
        }
    }
}
