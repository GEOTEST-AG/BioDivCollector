using BioDivCollector.DB.Models.Domain;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BioDivCollector.WebApp.Helpers
{
    public class UserHelper
    {
        public static User GetCurrentUser(ClaimsPrincipal userClaims, BioDivContext db)
        {
            ClaimsIdentity identity = (ClaimsIdentity)userClaims.Identity;
            string username = identity.FindFirst("preferred_username").Value;
            User u = db.Users.Find(username);
            if (u != null) return u;


            return null;

        }

        private static string GetAdminAccessToken(string url, string adminkey, string adminuser, string adminpassword)
        {
            var client = new RestClient(url);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", "admin-cli");
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_secret", adminkey);
            request.AddParameter("scope", "openid");
            request.AddParameter("username", adminuser);
            request.AddParameter("password", adminpassword);
            IRestResponse response = client.Execute(request);
            dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
            try
            {
                string newAccessToken = json.access_token.Value;

                return newAccessToken;
            }
            catch (Exception e)
            {
                return "Error";

            }
        }


        public static List<string> GetAllUsersByRole(string Role, string keycloakurl, string clientid, string adminurl, string adminkey, string adminuser, string adminpassword)
        {
            string access_token = UserHelper.GetAdminAccessToken(adminurl, adminkey, adminuser, adminpassword);
            if (access_token != "Error")
            {
                var client = new RestClient(keycloakurl + "/clients/" + clientid + "/roles/" + Role + "/users?max=1000");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);

                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);
                JArray jArray = JArray.Parse(response.Content);
                List<string> returnList = new List<string>();

                try
                {
                    foreach (dynamic user in jArray)
                    {
                        returnList.Add(user.username.Value);
                    }

                    return returnList;
                }
                catch (Exception e)
                {
                    return null;

                }
            }
            return null;

        }

        public static List<string> GetAllUsers(string url, string keycloakurl, string adminkey, string adminuser, string adminpassword)
        {
            string access_token = UserHelper.GetAdminAccessToken(url, adminkey, adminuser, adminpassword);
            if (access_token != "Error")
            {
                var client = new RestClient(keycloakurl + "/users?max=1000");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);
                JArray jArray = JArray.Parse(response.Content);
                List<string> returnList = new List<string>();

                try
                {
                    foreach (dynamic user in jArray)
                    {
                        returnList.Add(user.username.Value);
                    }

                    return returnList;
                }
                catch (Exception e)
                {
                    return null;

                }
            }
            return null;

        }

    }

    public class Users
    {
        public UserIds[] items { get; set; }
        public Guid guid { get; set; }

    }


    public class UserIds
    {
        public string item { get; set; }
        public string value { get; set; }
    }
}
