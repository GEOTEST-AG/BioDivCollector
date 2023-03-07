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
            set 
            { 
                Device.BeginInvokeOnMainThread(() =>
                {
                    activity = value;
                    OnPropertyChanged("Activity");
                    App.Busy = (activity != String.Empty);
                    MessagingCenter.Send<Application>(App.Current, "RefreshProjectList");
                });
            }
        }

        /// <summary>
        /// A validation to determine if the activity display should be visible
        /// </summary>
        private bool activityVisible;
        public bool ActivityVisible
        {
            get 
            {
                activityVisible = (activity != "");
                return activityVisible; 
            }
            set 
            {
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
        /// Refresh the project list
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Link command for opening the BioDiv URL in the browser
        /// </summary>
        public ICommand UrlCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));

        /// <summary>
        /// Pull to refresh parameters
        /// </summary>
        private bool isRefreshing = false;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        /// <summary>
        /// Initialisation
        /// </summary>
        public ProjectListVM()
        {
            User user = User.RetrieveUser();
            List<ProjectSimple> projectList = user.projects;

            var getProjectListTask = new Task<ObservableCollection<ProjectSimple>>(() =>
            {
                return new ObservableCollection<ProjectSimple>(projectList);
            });
        
            SyncProjectCommand = new SyncListProjectCommand(this);
            DeleteProjectCommand = new DeleteListProjectCommand(this);
            CopyBDCGUIDCommand = new BDCGUIDListCommand(this);
            RefreshCommand = new Command(ExecuteRefreshCommandAsync);


            getProjectListTask.Start();
            var newProjectList = getProjectListTask.Result;

            Projects = newProjectList;                
            //Projects = new ObservableCollection<ProjectSimple>(projectList);


            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            Activity = String.Empty;

            MessagingCenter.Subscribe<Application, string>(App.Current, "SyncMessage", (sender, arg) =>
            {
                    Activity = arg;
            });

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshProjectList", (sender) =>
            {
                RefreshProjectList();
            });
        }

        private void ExecuteRefreshCommandAsync(object obj)
        {
            IsRefreshing = true;

            if (App.IsConnected)
            {
                Task.Run(() =>
                {
                    App.CheckConnection();
                    if (App.IsConnected)
                    {
                        Login.GetUserDetails();
                        RefreshProjectList();
                    }
                });
            }

            // Stop refreshing
            IsRefreshing = false;
        }

        /// <summary>
        /// Check whether the project is available locally
        /// </summary>
        /// <param name="project"></param>
        /// <returns>true/false</returns>
        public async Task<bool> CheckProjectStatusAsync(ProjectSimple project)
        {
            string projectId = project.projectId.ToString();
            bool exists = await Project.LocalProjectExists(projectId);
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

        public void RefreshProjectList()
        {
            Task.Run(() =>
                {
                    User messageUser = User.RetrieveUser();
                    List<ProjectSimple> messageProjectList = messageUser.projects;
                    Projects = new ObservableCollection<ProjectSimple>(new List<ProjectSimple>()); //Force list update by setting it to a cleared list, then setting it back again
                    Projects = new ObservableCollection<ProjectSimple>(messageProjectList);
                });
        }

        /// <summary>
        /// Validate if an item has been selected, then delete it
        /// </summary>
        public void DeleteProject()
        {
            Task.Run(async () =>
            {
                if (SelectedItem != null)
                {
                    await DeleteProject(SelectedItem);
                }
            });
        }

        /// <summary>
        /// Delete a specific project
        /// </summary>
        /// <param name="project"></param>
        /// <returns>success</returns>
        public async Task<bool> DeleteProject(ProjectSimple project)
        {
            bool success = await Project.DeleteProject(project);
            return success;
        }

        /// <summary>
        /// Set a project to be the current project
        /// </summary>
        /// <param name="projectGUID"></param>
        public void SetProject(string projectGUID)
        {
            Task.Run(async () =>
            {
                await App.SetProject(projectGUID);
                App.ZoomMapOut = true;
            });
        }

        /// <summary>
        /// Refresh list on changes in connectivity to reflect what actions are possible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            MessagingCenter.Send<Application>(App.Current, "RefreshProjectList");
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
            var task = Task.Run(async () =>
            {
                bool existsAlready = await Project.LocalProjectExists(proj.projectId);
                return existsAlready;
            });
            return task.Result;
        }

        public void Execute(object parameter)
        {
            ProjectSimple proj = parameter as ProjectSimple;

            Device.BeginInvokeOnMainThread(async () =>
            {
                var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie dieses Projekt vom Gerät entfernen?", "Abbrechen", "Entfernen");
                if (response == "Entfernen")
                {
                    Debug.WriteLine("Deleting ");
                    bool success = await Project.DeleteProject(proj.projectId);
                    
                    if (App.CurrentProjectId == proj.projectId)
                    {
                        await App.SetProject(String.Empty);
                        Preferences.Set("FilterGeometry", String.Empty);
                        App.CurrentProject = null;
                    }

                    MessagingCenter.Send<Application>(App.Current, "RefreshProjectList");
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
            //Task.Run(async () =>
            //{
            //    if (parameter == null) { return false; }
            //    ProjectSimple proj = parameter as ProjectSimple;
            //    bool existsAlready = await Project.LocalProjectExists(proj.projectId);
            //    return !App.Busy && (existsAlready || App.IsConnected);
            //});

            if (parameter == null) { return false; }
            ProjectSimple proj = parameter as ProjectSimple;
            var task = Task.Run(async () =>
            {
                bool existsAlready = await Project.LocalProjectExists(proj.projectId);
                return existsAlready;
            });
            return !App.Busy && (App.IsConnected || task.Result);
        }

        public void Execute(object parameter)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                ProjectSimple proj = parameter as ProjectSimple;
                bool existsAlready = await Project.LocalProjectExists(proj.projectId);
                if (existsAlready == true)
                {
                    await App.SetProject(proj.projectId);
                    MessagingCenter.Send<SyncListProjectCommand, ProjectSimple>(this, "ProjectSelected", proj);
                }
                else
                {
                    Debug.WriteLine("Syncing: ");
                    await Project.DownloadProjectData(proj.projectId);
                }
                App.ZoomMapOut = true;
            });
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