﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using BioDivCollectorXamarin.Helpers;

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
                Device.BeginInvokeOnMainThread(() =>
                {
                    activity = value;
                    OnPropertyChanged("Activity");
                    App.Busy = (activity != String.Empty);
                });
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
            Activity = String.Empty;

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshRecords", async (sender) =>
            {
                ChangesMessageVisible = await Project.ProjectHasUnsavedChanges(App.CurrentProjectId);
            });

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshGeometries", async (sender) =>
            {
                ChangesMessageVisible = await Project.ProjectHasUnsavedChanges(App.CurrentProjectId);
            });

            //Subscribe to messages
            MessagingCenter.Subscribe<Application>(App.Current, "SetProject", async (sender) =>
            {
                var newId = Preferences.Get("currentProject", "");
                await this.SetProject(newId);
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
            Task.Run(async() =>
            {
                string projId = Preferences.Get("currentProject", @"");
                App.CurrentProjectId = projId;
                try
                {
                    await this.SetProject(projId);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Couldn't set project " + exp);
                }
                //SyncCommand.RaiseCanExecuteChanged();
            });
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
        public async Task SetProject(string projectGUID)
        {
            CurrentProject = await Project.FetchProject(projectGUID);
            ChangesMessageVisible = await Project.ProjectHasUnsavedChanges(projectGUID);
            Preferences.Set("FilterGeometry", String.Empty);
            try
            {
                //SyncCommand.RaiseCanExecuteChanged();
            }
            catch
            {
                //SyncCommand.CanExecute(null);
            }
        }

        /// <summary>
        /// Log the user out
        /// </summary>
        public void Logout()
        {
            SetProject("");
            Login.Logout();
        }
        
        /// <summary>
        /// Create a copy of the database as a backup
        /// </summary>
        /// <param name="obj"></param>
        public async void CreateBackup(object obj)
        {
            string fileloc;
            if (Device.RuntimePlatform == Device.iOS)
            {
                fileloc = DependencyService.Get<Interfaces.FileInterface>().GetBackupPath() + "/biodivcollector_database.sqlite";
            }
            else
            {
                fileloc = Path.Combine(DependencyService.Get<Interfaces.FileInterface>().GetPathToDownloads() + "/biodivcollector_database.sqlite");
            }

            if (File.Exists(Constants.DatabasePath))
            {
                if (File.Exists(fileloc))
                {
                    await App.Current.MainPage.DisplayAlert("Backup-Datei schon vorhanden", "Ein Backup befindet sich unter: " + fileloc + ". Bitte verschieben oder löschen Sie dieses Dokument, um ein neues zu erstellen.", "OK");
                }
                else
                {
                    File.Copy(Constants.DatabasePath, fileloc);
                    await App.Current.MainPage.DisplayAlert("Backup erstellt", "Ein Backup befindet sich unter: " + fileloc, "OK");
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Fehler", "Datenbank nicht vorhanden", "OK");
            }

        }
        
        /// <summary>
        /// Restore data from an existing backup
        /// </summary>
        /// <param name="obj"></param>
        public async void RestoreData(object obj)
        {
            var restore = await App.Current.MainPage.DisplayAlert("Daten wiederherstellen?", "Möchten Sie die Daten aus der vorhandenen Datensicherung wiederherstellen?", "Ja", "Nein");
            if (restore == true)
            {
                string backupFile;
                if (Device.RuntimePlatform == Device.iOS)
                {
                    backupFile = DependencyService.Get<Interfaces.FileInterface>().GetBackupPath() + "/biodivcollector_database.sqlite";
                }
                else
                {
                    backupFile = Path.Combine(DependencyService.Get<Interfaces.FileInterface>().GetPathToDownloads() + "/biodivcollector_database.sqlite");
                }

                if (File.Exists(backupFile))
                {
                    await App.ActiveDatabaseConnection.CloseAsync();
                    if (File.Exists(Constants.DatabasePath))
                    {
                        File.Delete(Constants.DatabasePath);
                    }
                    File.Copy(backupFile, Constants.DatabasePath);
                    App.ActiveDatabaseConnection = await DatabaseConnection.Instance;
                    await App.Current.MainPage.DisplayAlert("Daten wiederhergestellt", String.Empty, "OK");
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Fehler", "Die Daten könnten nicht wiederhergestellt werden", "OK");
                }
            }
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
            Task.Run(async () =>
            {
                if (App.CurrentProjectId != null)
                {
                    await Project.SynchroniseProjectData(App.CurrentProjectId);
                }
            });
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

        public void Execute(object parameter)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie dieses Projekt vom Gerät entfernen?", "Abbrechen", "Entfernen");
                if (response == "Entfernen")
                {
                    if (App.CurrentProjectId != null)
                    {
                        Debug.WriteLine("Deleting ");
                        bool success = await Project.DeleteProject(App.CurrentProjectId);
                        if (success)
                        {
                            await App.SetProject(String.Empty);
                        }
                    }
                    Preferences.Set("FilterGeometry", String.Empty);
                }
            });
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
