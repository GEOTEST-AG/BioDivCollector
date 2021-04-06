using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class ProjectListVM:BaseViewModel
    {
        /// <summary>
        /// A list of projects
        /// </summary>
        private ObservableCollection<ProjectSimple> projects;
        public ObservableCollection<ProjectSimple> Projects
        {
            get { return projects; }
            set
            {
                projects = value;
                OnPropertyChanged("Projects");
            }
        }

        /// <summary>
        /// The selected project
        /// </summary>
        private ProjectSimple selectedItem;
        public ProjectSimple SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        /// <summary>
        /// An indicator of activity (a string telling you what is happening)
        /// </summary>
        private string activity;
        public string Activity
        {
            get { return activity; }
            set { 
                Device.BeginInvokeOnMainThread(() =>
                {
                    activity = value;
                    OnPropertyChanged("Activity");
                });
            }
        }

        /// <summary>
        /// A validation to determine if the activity display should be visible
        /// </summary>
        private bool activityVisible;
        public bool ActivityVisible
        {
            get {
                activityVisible = (activity != "");
                return activityVisible; 
            }
            set {
                Device.BeginInvokeOnMainThread(() =>
                {
                    activityVisible = value;
                    OnPropertyChanged("ActivityVisible");
                });
            }
        }

        /// <summary>
        /// A button command to delete the local copy of a project
        /// </summary>
        public DeleteListProjectCommand DeleteProjectCommand { get; set; }

        /// <summary>
        /// A button command to sync/download the project
        /// </summary>
        public SyncListProjectCommand SyncProjectCommand { get; set; }

        /// <summary>
        /// A button command to copy the BDC GUID
        /// </summary>
        public BDCGUIDListCommand CopyBDCGUIDCommand { get; set; }

        /// <summary>
        /// Link command for opening the BioDiv URL in the browser
        /// </summary>
        public ICommand UrlCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));


        /// <summary>
        /// Initialisation
        /// </summary>
        public ProjectListVM()
        {
            User user = User.RetrieveUser();
            List<ProjectSimple> projectList = user.projects;

            Projects = new ObservableCollection<ProjectSimple>(projectList);

            SyncProjectCommand = new SyncListProjectCommand(this);
            DeleteProjectCommand = new DeleteListProjectCommand(this);
            CopyBDCGUIDCommand = new BDCGUIDListCommand(this);

            Activity = "";
            MessagingCenter.Subscribe<Application, string>(App.Current, "SyncMessage", (sender, arg) =>
            {
                Activity = arg;
            });

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshProjectList", (sender) =>
            {
                User messageUser = User.RetrieveUser();
                List<ProjectSimple> messageProjectList = messageUser.projects;
                Projects = new ObservableCollection<ProjectSimple>(new List<ProjectSimple>()); //Force list update by setting it to a cleared list, then setting it back again
                Projects = new ObservableCollection<ProjectSimple>(messageProjectList);
            });
        }

        /// <summary>
        /// Check whether the project is available locally
        /// </summary>
        /// <param name="project"></param>
        /// <returns>true/false</returns>
        public async Task<bool> CheckProjectStatusAsync(ProjectSimple project)
        {
            string projectId = project.projectId.ToString();
            bool exists = Project.LocalProjectExists(projectId);
            if (!exists)
            {
                await Project.DownloadProjectData(project.projectId);
            }
            else
            {
                if (projectId == App.CurrentProjectId)
                {
                    this.SetProject(project.projectId);
                }
            }
            return true;
        }

        /// <summary>
        /// Validate if an item has been selected, then delete it
        /// </summary>
        public void DeleteProject()
        {
            if (SelectedItem != null)
            {
                DeleteProject(SelectedItem);
            }
        }

        /// <summary>
        /// Delete a specific project
        /// </summary>
        /// <param name="project"></param>
        /// <returns>success</returns>
        public bool DeleteProject(ProjectSimple project)
        {
            bool success = Project.DeleteProject(project);
            return success;
        }

        /// <summary>
        /// Set a project to be the current project
        /// </summary>
        /// <param name="projectGUID"></param>
        public void SetProject(string projectGUID)
        {
            App.SetProject(projectGUID);
            App.ZoomMapOut = true;
        }
    }

    /// <summary>
    /// A button command for deleting the project
    /// </summary>
    public class DeleteListProjectCommand : ICommand
    {

        public ProjectListVM ProjectListViewModel { get; set; }

        public DeleteListProjectCommand(ProjectListVM projectListVM)
        {
            ProjectListViewModel = projectListVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {

            if (parameter == null) { return false; }
            ProjectSimple proj = parameter as ProjectSimple;
            bool existsAlready = Project.LocalProjectExists(proj.projectId);
            return existsAlready;

        }

        public void Execute(object parameter)
        {
            ProjectSimple proj = parameter as ProjectSimple;
            var project = Project.FetchProject(proj.projectId);

            Device.BeginInvokeOnMainThread(async () =>
            {
                var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie dieses Projekt vom Gerät entfernen?", "Abbrechen", "Entfernen");
                if (response == "Entfernen")
                {
                    Debug.WriteLine("Deleting ");
                    bool success = Project.DeleteProject(project);
                    MessagingCenter.Send<Application>(App.Current, "RefreshProjectList");
                    if (App.CurrentProjectId == project.projectId)
                    {
                        App.SetProject(String.Empty);
                        Preferences.Set("FilterGeometry", String.Empty);
                    }
                }
            });
        }
    }

    /// <summary>
    /// A button command for synchronising/downloading the project
    /// </summary>
    public class SyncListProjectCommand : ICommand
    {

        public ProjectListVM ProjectListViewModel { get; set; }

        public SyncListProjectCommand(ProjectListVM projectListVM)
        {
            ProjectListViewModel = projectListVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            ProjectSimple proj = parameter as ProjectSimple;
            bool existsAlready = Project.LocalProjectExists(proj.projectId);
            if (existsAlready == true)
            {
                App.SetProject(proj.projectId);
                MessagingCenter.Send<SyncListProjectCommand, ProjectSimple>(this, "ProjectSelected", proj);
            }
            else
            {
                Debug.WriteLine("Syncing: ");

                
                MessagingCenter.Subscribe<DataDAO, string>(new DataDAO(), "DataDownloadSuccess", (sender, arg) =>
                {
                    if (arg != "Error downloading data" && arg != "DataDownloadError")
                    {
                        App.SetProject(proj.projectId);
                        MessagingCenter.Send<SyncListProjectCommand, ProjectSimple>(this, "ProjectSelected", proj);
                    }
                    MessagingCenter.Unsubscribe<DataDAO, string>(new DataDAO(), "DataDownloadSuccess");
                });

                MessagingCenter.Subscribe<DataDAO, string>(new DataDAO(), "DataDownloadError", (sender, arg) =>
                {
                    MessagingCenter.Unsubscribe<DataDAO, string>(new DataDAO(), "DataDownloadError");
                });

                await Project.DownloadProjectData(proj.projectId);
            }
            App.ZoomMapOut = true;
        }
    }

    /// <summary>
    /// A button command for copying the BDC GUID
    /// </summary>
    public class BDCGUIDListCommand : ICommand
    {

        public ProjectListVM ProjectListViewModel { get; set; }

        public BDCGUIDListCommand(ProjectListVM projectListVM)
        {
            ProjectListViewModel = projectListVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var proj = parameter as ProjectSimple;
            var extId = proj.projectId;
            var bguid = "<<BDC><" + extId + ">>";
            Clipboard.SetTextAsync(bguid);
            MessagingCenter.Send<Application>(App.Current, "GuidCopied");
        }
    }
}