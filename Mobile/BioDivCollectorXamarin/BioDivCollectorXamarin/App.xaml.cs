using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.IEssentials;
using BioDivCollectorXamarin.Models.LoginModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin
{
    public partial class App : Application
    {
        /// <summary>
        /// Quickly configure the app to the test server or production server
        /// </summary>
        public static bool IsTest = false;

        /// <summary>
        /// Keeps track of the current user
        /// </summary>
        public static Models.LoginModel.User CurrentUser;

        /// <summary>
        /// Additional parameters on top of Xamarin Essential preferences
        /// </summary>
        public static BioDivPreferences BioDivPrefs;

        /// <summary>
        /// OS dependent database location
        /// </summary>
        public static string DatabaseLocation = string.Empty;

        /// <summary>
        /// OS dependent user files location
        /// </summary>
        public static string TileLocation = string.Empty;

        /// <summary>
        /// The currently selected project
        /// </summary>
        public static string CurrentProjectId;

        /// <summary>
        /// Whether the app should show the login page
        /// </summary>
        public static Boolean ShowLogin = true;

        /// <summary>
        /// Whether the app is up and running
        /// </summary>
        public static Boolean AppDidStart = true;

        /// <summary>
        /// Zoom the map out to show an overview
        /// </summary>
        public static Boolean ZoomMapOut;

        /// <summary>
        /// Whether the app has a data connection
        /// </summary>
        public static bool IsConnected = true;

        /// <summary>
        /// Configure the app to the test or prod connector
        /// </summary>
        private static string serverURL;
        public static string ServerURL
        {
            get
            {
                if (IsTest)
                {
                    return serverURL = "https://testconnector.biodivcollector.ch";
                }
                else
                {
                    return serverURL = "https://connector.biodivcollector.ch";
                }
            }
        }

        /// <summary>
        /// Configure the app to the test or prod logout url
        /// </summary>
        private static string logoutURL;
        public static string LogoutURL
        {
            get
            {
                if (IsTest)
                {
                    return logoutURL = "https://test.biodivcollector.ch/Home/Logout";
                }
                else
                {
                    return logoutURL = "https://biodivcollector.ch/Home/Logout";
                }
            }
        }

        /// <summary>
        /// Configure the app to the test or prod authentication server
        /// </summary>
        private static string authParams;
        public static string AuthParams
        {
            get
            {
                if (IsTest)
                {
                    return authParams = "BioDivCollectorXamarin.Models.LoginModel.AuthTest.xml";
                }
                else
                {
                    return authParams = "BioDivCollectorXamarin.Models.LoginModel.Auth.xml";
                }
            }
        }

        /// <summary>
        /// State restoration
        /// </summary>
        private static string currentRoute;
        public static string CurrentRoute
        {
            get
            {
                return currentRoute = Preferences.Get("CurrentRoute", String.Empty); 
            }
            set
            {
                currentRoute = value;
                Preferences.Set("CurrentRoute", "/"+ value);
            }
        }

        /// <summary>
        /// Disable buttons when app is busy
        /// </summary>
        private static bool busy;
        public static bool Busy
        {
            get
            {
                return busy;
            }
            set
            {
                busy = value;
            }
        }
        
        /// <summary>
        /// Initialisation without furhter parameters
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialisation with os dependent parameter inputs. Start listening for a login return
        /// </summary>
        /// <param name="databaseLocation"></param>
        /// <param name="tileLocation"></param>
        public App(string databaseLocation, string tileLocation)
        {
            InitializeComponent();
            Device.SetFlags(new string[] { "RadioButton_Experimental", "Shapes_Experimental", "Expander_Experimental" });

            CurrentUser = User.RetrieveUser();
            BioDivPrefs = new BioDivPreferences();
            DatabaseLocation = databaseLocation;
            TileLocation = tileLocation;
            CurrentProjectId = Preferences.Get("currentProject", "");
            Busy = false;
            CheckConnection();

            VersionTracking.Track();

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginSuccessful", (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    BioDivCollectorXamarin.Models.LoginModel.Login.GetUserDetails();

                    MainPage = new AppShell();
                    if (CurrentRoute != null && CurrentRoute != String.Empty)
                    {
                        try { AppShell.Current.GoToAsync(CurrentRoute); }
                        catch (Exception e)
                        {
                            Console.WriteLine("Didn't manage to go to route, " + e);
                        }
                    }
                    
                });
            });

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnuccessful", (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {

                    MainPage = Login.GetPageToView();

                });
            });

        }

        /// <summary>
        /// Start listening for updates in connectivity and check if the app should show the login screen
        /// </summary>
        protected override void OnStart()
        {
            this.StartListening();
            if (ShowLogin)
            {
                MainPage = Login.GetPageToView();
            }
            ShowLogin = false;

        }

        /// <summary>
        /// Stop listening to whether the device is online or offline when the app goes into the background
        /// </summary>
        protected override void OnSleep()
        {
            // Handle when your app sleeps
            this.StopListening();
            ShowLogin = true;
        }

        /// <summary>
        /// Check the login again on returning, and start listening to whether the device is online or offline
        /// </summary>
        protected override void OnResume()
        {
            Login.CheckLogin();
            this.StartListening();
        }

        /// <summary>
        /// Register for connectivity changes
        /// </summary>
        public void StartListening()
        {
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        /// <summary>
        /// Un-register listener for connectivity changes
        /// </summary>
        public void StopListening()
        {
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        /// <summary>
        /// Check the network access status when we are informed that there is a change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            CheckConnection();
        }

        /// <summary>
        /// Check the network access status
        /// </summary>
        public static void CheckConnection()
        {
            if (Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet)
            {
                IsConnected = true;
            }
            else
            {
                IsConnected = false;
            }
        }

        /// <summary>
        /// Register the current project
        /// </summary>
        /// <param name="projectGUID"></param>
        public static void SetProject(string projectGUID)
        {
            Task.Run(() =>
            {
                var projectId = projectGUID.ToString();
                App.CurrentProjectId = projectId;
                Preferences.Set("currentProject", projectId);
                AppShell.ClearNavigationStacks();
                MessagingCenter.Send(App.Current, "SetProject");
            });
        }
    }
}

