﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class ProjectsPageVM:BaseViewModel
    {
        /// <summary>
        /// Button command for logging out
        /// </summary>
        public LogoutCommand LogoutCommand { get; set; }

        /// <summary>
        /// Button command for deleting the local copy of the app
        /// </summary>
        public DeleteProjectCommand DeleteProjectCommand { get; set; }

        /// <summary>
        /// Button command for copying the BDC GUID
        /// </summary>
        public BDCGUIDCommand CopyBDCGUIDCommand { get; set; }

        /// <summary>
        /// Button command for synchronising the project
        /// </summary>
        public SyncCommand SyncCommand { get; set; }

        /// <summary>
        /// Version number of the app
        /// </summary>
        public string CurrentAppVersion;

        /// <summary>
        /// The currently selected project
        /// </summary>
        private Project currentProject;
        public Project CurrentProject
        {
            get { return currentProject; }
            set
            {
                currentProject = value;
                OnPropertyChanged("CurrentProject");
            }
        }

        /// <summary>
        /// A string used as an activity indicator
        /// </summary>
        private string activity;
        public string Activity
        {
            get { return activity; }
            set
            {
                activity = value;
                OnPropertyChanged("Activity");
            }
        }

        /// <summary>
        /// Shows whether changes have been made to the app since the last sync
        /// </summary>
        public bool ChangesMessageVisible { get; set; }

        /// <summary>
        /// Initialise the project page
        /// </summary>
        public ProjectsPageVM()
        {
            Title = "Projekt";
            //Create commands
            SyncCommand = new SyncCommand(this);
            DeleteProjectCommand = new DeleteProjectCommand(this);
            CopyBDCGUIDCommand = new BDCGUIDCommand(this);
            LogoutCommand = new LogoutCommand(this);
            CurrentAppVersion = VersionTracking.CurrentVersion.ToString() + "(" + VersionTracking.CurrentBuild.ToString() + ")";

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshRecords", (sender) =>
            {
                ChangesMessageVisible = Project.ProjectHasUnsavedChanges(App.CurrentProjectId);
            });

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshGeometries", (sender) =>
            {
                ChangesMessageVisible = Project.ProjectHasUnsavedChanges(App.CurrentProjectId);
            });

            //Get user or log new one in
            if (App.CurrentUser != null)
            {
                CheckProjectAvailability();
                Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            }
            else
            {
                App.Current.MainPage.Navigation.PushAsync(new MainPage(),true);
            }

            //Subscribe to messages
            MessagingCenter.Subscribe<Application>(App.Current, "SetProject", (sender) =>
            {
                var newId = Preferences.Get("currentProject", "");
                this.SetProject(newId);
            });

            Activity = "";
            MessagingCenter.Subscribe<Application, string>(App.Current, "SyncMessage", (sender, arg) =>
            {
                Activity = arg;
            });
        }


        /// <summary>
        /// Set the current project on appearing
        /// </summary>
        public void OnAppearing()
        {
            string projId = Preferences.Get("currentProject", @"");
            SyncCommand.RaiseCanExecuteChanged();
            App.CurrentProjectId = projId;
            try
            {
                this.SetProject(projId);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Couldn't set project " + exp);
            }
            
        }

        /// <summary>
        /// Check if the current project (perhaps selected in a previous session) is available to this user
        /// </summary>
        void CheckProjectAvailability()
        {
            bool found = false;
            var projectId = Preferences.Get("currentProject", "");
            User user = User.RetrieveUser();
            List<ProjectSimple> projectList = user.projects;
            foreach (var proj in projectList)
            {
                if (proj.projectId == projectId)
                {
                    App.CurrentProjectId = projectId;
                    found = true;
                    break;
                }
            }
            if (found == false)
            {
                App.CurrentProjectId = "";
                Preferences.Set("currentProject", "");
            }
        }

        /// <summary>
        /// React to changes in internet connectivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            IsNotConnected = e.NetworkAccess != NetworkAccess.Internet;
            LogoutCommand.RaiseCanExecuteChanged();
            SyncCommand.RaiseCanExecuteChanged();
            DeleteProjectCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Set the current project and reset any filter settings
        /// </summary>
        /// <param name="projectGUID"></param>
        public void SetProject(string projectGUID)
        {
            CurrentProject = Project.FetchProject(projectGUID);
            ChangesMessageVisible = Project.ProjectHasUnsavedChanges(projectGUID);
            Preferences.Set("FilterGeometry", String.Empty);
        }

        /// <summary>
        /// Log the user out
        /// </summary>
        public void Logout ()
        {
            try
            {
                SetProject("");
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(App.LogoutURL),
                        Method = HttpMethod.Get,
                    };

                    var token = Preferences.Get("AccessToken", "");


                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var task = client.SendAsync(request).ContinueWith((taskwithmsg) =>
                    {
                        var response = taskwithmsg.Result;

                        var jsonTask = response.Content.ReadAsStringAsync();
                        jsonTask.Wait();
                    });

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

            Xamarin.Forms.MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnuccessful");
        }
    }

    /// <summary>
    /// Button command for synchronising the project
    /// </summary>
    public class SyncCommand : ICommand
    {

        public ProjectsPageVM ProjectsPageViewModel { get; set; }

        public SyncCommand(ProjectsPageVM projectsPageVM)
        {
            ProjectsPageViewModel = projectsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            //Sync only if there is an internet connection
            var projId = App.CurrentProjectId;
            if (Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet && projId != null && projId != String.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Execute(object parameter)
        {
            if (App.CurrentProjectId != null)
            {
                Project.SynchroniseProjectData(App.CurrentProjectId);
            }
        }
    }

    /// <summary>
    /// Button command for deleting the local copy of the project
    /// </summary>
    public class DeleteProjectCommand : ICommand
    {

        public ProjectsPageVM ProjectsPageViewModel { get; set; }

        public DeleteProjectCommand(ProjectsPageVM projectsPageVM)
        {
            ProjectsPageViewModel = projectsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            //Sync only if there is an internet connection
            var projId = Preferences.Get("currentProject", "");
            if ( projId != null && projId != String.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async void Execute(object parameter)
        {
            var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie dieses Projekt vom Gerät entfernen?", "Abbrechen", "Entfernen");
            if (response == "Entfernen")
            {
                if (App.CurrentProjectId != null)
                {
                    Debug.WriteLine("Deleting ");
                    var proj = Project.FetchProjectWithChildren(App.CurrentProjectId);
                    bool success = Project.DeleteProject(proj);
                    if (success)
                    {
                        App.SetProject(String.Empty);
                    }
                }
                Preferences.Set("FilterGeometry", String.Empty);
            }
        }
    }

    /// <summary>
    /// Button command for copying the BDC GUID
    /// </summary>
    public class BDCGUIDCommand : ICommand
    {

        public ProjectsPageVM ProjectsPageViewModel { get; set; }

        public BDCGUIDCommand(ProjectsPageVM projectsPageVM)
        {
            ProjectsPageViewModel = projectsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            var projId = Preferences.Get("currentProject", "");
            if (projId != null && projId != String.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Execute(object parameter)
        {
            var proj = parameter as Project;
            var extId = proj.projectId;
            var bguid = "<<BDC><" + extId + ">>";
            Clipboard.SetTextAsync(bguid);
            MessagingCenter.Send<Application>(App.Current, "GuidCopied");
        }
    }

    /// <summary>
    /// Button command for logging the user out
    /// </summary>
    public class LogoutCommand : ICommand
    {

        public ProjectsPageVM ProjectsPageViewModel { get; set; }

        public LogoutCommand(ProjectsPageVM projectsPageVM)
        {
            ProjectsPageViewModel = projectsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            if (Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Execute(object parameter)
        {
            MessagingCenter.Send<LogoutCommand>(this, "ShowUserChoice");
        }


    }
}