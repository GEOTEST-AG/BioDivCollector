using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.IEssentials;
using BioDivCollectorXamarin.Models.LoginModel;
using FeldAppX.Models.DatabaseModel;
using SQLite;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: ExportFont("MaterialIcons-Regular.ttf", Alias = "Material")]
namespace BioDivCollectorXamarin
{
    public partial class App : Application
    {
        /// <summary>
        /// Quickly configure the app to the test server or production server
        /// </summary>
        public static bool IsTest = true;

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
        /// Single running Databaseconnection
        /// </summary>
        public static SQLiteAsyncConnection ActiveDatabaseConnection;

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
                    return logoutURL = "https://id.biodivcollector.ch/auth/realms/BioDivCollector/protocol/openid-connect/logout";
                }
                else
                {
                    return logoutURL = "https://id.biodivcollector.ch/auth/realms/BioDivCollector/protocol/openid-connect/logout";
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
        /// Initialisation without further parameters
        /// </summary>
        public App()
        {
            LoadXMLLicenceData();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Preferences.Get("sflicence", ""));
            InitializeComponent();
        }

        /// <summary>
        /// Initialisation with os dependent parameter inputs. Start listening for a login return
        /// </summary>
        /// <param name="databaseLocation"></param>
        /// <param name="tileLocation"></param>
        public App(string databaseLocation, string tileLocation)
        {
            LoadXMLLicenceData();
            var licence = Preferences.Get("sflicence", "");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Preferences.Get("sflicence", ""));

            InitializeComponent();
            Device.SetFlags(new string[] { "RadioButton_Experimental", "Shapes_Experimental", "Expander_Experimental" });
            BioDivPrefs = new BioDivPreferences();
            Preferences.Set("databaseLocation", databaseLocation);

            Task.Run(async () =>
            {
                ActiveDatabaseConnection = await DatabaseConnection.Instance;
            });

            CurrentUser = User.RetrieveUser();

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
                        try {
                            AppShell.Current.GoToAsync(CurrentRoute);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Didn't manage to go to route, " + e);
                        }
                    }
                    
                });
            });

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnsuccessful", (sender) =>
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

        /// <summary>
        /// Load up the licence key from an xml file
        /// </summary>
        public static void LoadXMLLicenceData()
        { 
            var assembly = typeof(SfLicence).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream("BioDivCollectorXamarin.SfLicence.xml");

            XDocument doc = XDocument.Load(stream);
            IEnumerable<string> licences = from s in doc.Descendants("SfLicenceKey")
                          select s.Attribute("sflicence").Value.ToString();
            Preferences.Set("sflicence", licences.FirstOrDefault());
        }
    }
}

