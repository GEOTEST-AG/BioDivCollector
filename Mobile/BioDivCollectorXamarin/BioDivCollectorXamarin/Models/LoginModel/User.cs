using System;
using System.Collections.Generic;
using BioDivCollectorXamarin.Models.IEssentials;
using Newtonsoft.Json;

namespace BioDivCollectorXamarin.Models.LoginModel
{
    public class User
    {
        private bool loggedIn;

        public bool LoggedIn
        {
            get { return loggedIn; }
            set
            {
                loggedIn = value;
            }
        }

        private bool success;

        public bool Success
        {
            //Success is the parameter returned by the login system. If login returns, also update LoggedIn.
            get { return success; }
            set
            {
                success = value;
                LoggedIn = value;
            }
        }

        public string error { get; set; }

        public string userId { get; set; }
        public string name { get; set; }
        public string firstName { get; set; }

        public List<string> roles { get; set; } = new List<string>();
        public string activeRole { get; set; }
        public List<ProjectSimple> projects { get; set; } = new List<ProjectSimple>();

        public User()
        {

        }

        /// <summary>
        /// Get the last user from the preferences
        /// </summary>
        /// <returns></returns>
        public static User RetrieveUser()
        {
            if (App.BioDivPrefs == null)
            { App.BioDivPrefs = new BioDivPreferences(); }
            string json = App.BioDivPrefs.Get("User", "");
            var currentUser = new User();
            if (json != "")
            {
                try
                {
                    currentUser = JsonConvert.DeserializeObject<User>(json);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    currentUser = new User();
                    currentUser.firstName = "";
                    currentUser.name = "";
                }
                App.CurrentUser = currentUser;
                return currentUser;
            }  
            else
            {
                currentUser = new User();
                currentUser.firstName = "";
                currentUser.name = "";
                App.CurrentUser = currentUser;
                return currentUser;
            }
        }
    }

    /// <summary>
    /// Project definition returned by the connector
    /// </summary>
    public class ProjectSimple
    {
        public string projectId { get; set; }
        public string projectName { get; set; }
        public string description { get; set; }
        public string projectNumber { get; set; }
        public string id_Extern { get; set; }
        public int projectStatusId { get; set; }

        public string projectManager { get; set; }
        public string projectConfigurator { get; set; }

        public ProjectSimple()
        {

        }
    }
}
