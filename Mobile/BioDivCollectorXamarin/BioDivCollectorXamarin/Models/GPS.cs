using BioDivCollectorXamarin.Helpers;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models
{
    public class GPS : ObservableClass
    {

        private bool hasLocationPermission;
        public bool HasLocationPermission
        {
            get { return hasLocationPermission; }
            set
            {
                hasLocationPermission = value;
                OnPropertyChanged("HasLocationPermission");
            }
        }

        public Mapsui.UI.Forms.Position CurrentPosition;

        public bool GetPermissionsInProgress { get; set; }

        public KalmanLatLong Filter { get; set; } = new KalmanLatLong(3);
        public Queue<double> AccuracyQueue { get; set; }


        /// <summary>
        /// Retrieve and request permissions for GPS use
        /// </summary>
        public async Task GetPermissions()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != Xamarin.Essentials.PermissionStatus.Granted && GetPermissionsInProgress == false)
                {
                    GetPermissionsInProgress = true;
                    Device.BeginInvokeOnMainThread(async() => {
                        if (Device.RuntimePlatform == "Android")
                        {
                            await App.Current.MainPage.DisplayAlert("GPS-Zugriff", "Wenn Sie Ihren Standort auf der Karte anzeigen möchten, benötigt diese App Zugriff auf die GPS-Funktion. Wenn Sie dies zulassen möchten, akzeptieren Sie bitte die GPS-Anfrage. Ist dies nicht der Fall, können Sie die Anfrage ablehnen und die Einstellungen der App zu einem späteren Zeitpunkt ändern, wenn Sie sie benötigen.", "Zeige mir die Anfrage", "Später");
                            Task.Run(async () => {
                                Task.Delay(1000).Wait();
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                                    GetPermissionsInProgress = false;
                                });
                            });
                        }
                        else
                        {
                            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                            GetPermissionsInProgress = false;
                        }
                    });
                }
                if (status == Xamarin.Essentials.PermissionStatus.Granted)
                {
                    HasLocationPermission = true;
                    MessagingCenter.Send<Application>(App.Current, "PermissionsChanged");
                }
                else
                {
                    HasLocationPermission = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                HasLocationPermission = false;
            }
        }

        /// <summary>
        /// Start acquisition using the GPS
        /// </summary>
        public void StartGPSAsync()
        {
            if (!App.GpsIsRunning)
            {
                App.GpsIsRunning = true;
                Task.Run(async () =>
                {
                    var tryStartGPS = Preferences.Get("GPS", false);
                    while (tryStartGPS)
                    {
                        Task.Delay(3000).Wait();
                        if (HasLocationPermission)
                        {
                            try
                            {
                                var locator = CrossGeolocator.Current;
                                locator.PositionChanged += Locator_PositionChanged;
                                locator.DesiredAccuracy = 1;
                                if (!locator.IsListening)
                                {

                                    await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(1), 1, true,
                                        new ListenerSettings
                                        {
                                            ActivityType = ActivityType.Fitness,
                                            AllowBackgroundUpdates = false,
                                            DeferLocationUpdates = false,
                                            DeferralDistanceMeters = 1,
                                            DeferralTime = TimeSpan.FromSeconds(0.5),
                                            ListenForSignificantChanges = false,
                                            PauseLocationUpdatesAutomatically = false
                                        });
                                    AccuracyQueue = new Queue<double>();
                                }

                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    if (!Compass.IsMonitoring && Device.RuntimePlatform == "iOS")
                                    {
                                        Compass.ReadingChanged += Compass_ReadingChanged;
                                        Compass.Start(SensorSpeed.UI, true);
                                    }
                                });
                                break;
                            }
                            catch (FeatureNotSupportedException fnsEx)
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    await App.Current.MainPage.DisplayAlert("GPS wird nicht unterstützt", "Dieses Gerät unterstützt GPS nicht", "OK");
                                });
                                tryStartGPS = false;
                                MessagingCenter.Send<Application>(App.Current, "StopGPSandRemoveLayer");
                            }
                            catch (FeatureNotEnabledException fneEx)
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    await App.Current.MainPage.DisplayAlert("GPS nicht aktiviert", "GPS ist für diese App nicht aktiviert", "OK");
                                });
                                tryStartGPS = false;
                                MessagingCenter.Send<Application>(App.Current, "StopGPSandRemoveLayer");
                            }
                            catch (PermissionException pEx)
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    await App.Current.MainPage.DisplayAlert("GPS nicht zugelassen", "Bitte erlauben Sie die GPS-Aktivierung in den Einstellungen der App", "OK");
                                });
                                tryStartGPS = false;
                                MessagingCenter.Send<Application>(App.Current, "StopGPSandRemoveLayer");
                            }
                            catch (Exception ex)
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    await App.Current.MainPage.DisplayAlert("GPS nicht erreichbar", "Die App konnte nicht auf das GPS zugreifen", "OK");
                                });
                                tryStartGPS = false;
                                MessagingCenter.Send<Application>(App.Current, "StopGPSandRemoveLayer");
                            }
                        }
                        else
                        {
                            Task.Delay(5000).Wait();
                            await GetPermissions();
                        }
                    }
                });
            }

        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var lastHeading = Preferences.Get("LastPositionHeading", 0);
            if (Math.Abs(lastHeading - e.Reading.HeadingMagneticNorth) > 0) 
            {
                Preferences.Set("LastPositionHeading", (int)e.Reading.HeadingMagneticNorth);
                MessagingCenter.Send<GPS>(this, "BearingUpdate");
            }
        }

        /// <summary>
        /// React to an update in the GPS position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Locator_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            var location = e.Position;
            if (location != null)
            {
                Filter.Process(location.Latitude, location.Longitude, (float)location.Accuracy, DateTimeOffset.Now.ToUnixTimeMilliseconds());
                var lat = Filter.get_lat();
                var lon = Filter.get_lng();

                Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
                Console.WriteLine($"Latitude: {lat}, Longitude: {lon}");

                try
                {
                    if (location.Accuracy != null && location.Accuracy < 10000)
                    {
                        AccuracyQueue.Enqueue(location.Accuracy);
                        if (AccuracyQueue.Count > 10)
                        {
                            AccuracyQueue.Dequeue();
                        }
                        //get the median
                        var accuracy = AccuracyQueue.Min();
                        Preferences.Set("LastPositionLatitude", lat);
                        Preferences.Set("LastPositionLongitude", lon);
                        Preferences.Set("LastPositionAccuracy", (int)accuracy);

                        MessagingCenter.Send<GPS>(this, "GPSPositionUpdate");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Stop acquisition using the GPS
        /// </summary>
        public static void StopGPSAsync()
        {
            if (Compass.IsMonitoring && Device.RuntimePlatform == "iOS")
            {
                Compass.Stop();
            }
            App.GpsIsRunning = false;
            CrossGeolocator.Current.StopListeningAsync();
        }

    }
}
