using BioDivCollectorXamarin.ViewModels;
using FeldAppX.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
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
            //Wait to get permission before starting GPS
            if (!App.GpsIsRunning)
            {
                App.GpsIsRunning = true;
                Task.Run(async () =>
                {
                    while (Preferences.Get("GPS", false))
                    {
                        //Task.Delay(500).Wait();
                        if (HasLocationPermission)
                        {
                            try
                            {

                                App.GPSCancellationToken = new CancellationTokenSource();
                                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                                var location = await Geolocation.GetLocationAsync(request, App.GPSCancellationToken.Token);

                                if (location != null)
                                {
                                    Filter.Process(location.Latitude, location.Longitude, (float)location.Accuracy, DateTimeOffset.Now.ToUnixTimeMilliseconds());
                                    var lat = Filter.get_lat();
                                    var lon = Filter.get_lng();
                                    var accuracy = (int)Filter.get_accuracy();

                                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");

                                    try
                                    {
                                        if (location.Accuracy != null && location.Accuracy < 10000)
                                        {
                                            Preferences.Set("LastPositionLatitude", lat);
                                            Preferences.Set("LastPositionLongitude", lon);
                                            Dictionary<string, double> dic = new Dictionary<string, double>();
                                            dic.Add("latitude", location.Latitude);
                                            dic.Add("longitude", location.Longitude);
                                            Console.WriteLine(location.Latitude.ToString() + ", " + location.Longitude.ToString() + " +/- " + location.Accuracy);
                                            dic.Add("accuracy", (int)location.Accuracy);
                                            Preferences.Set("LastPositionAccuracy", (int)location.Accuracy);

                                            MessagingCenter.Send<GPS>(this, "GPSPositionUpdate");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                    }
                                }
                            }
                            catch (FeatureNotSupportedException fnsEx)
                            {
                                Device.BeginInvokeOnMainThread(async() => {
                                    await App.Current.MainPage.DisplayAlert("GPS wird nicht unterstützt", "Dieses Gerät unterstützt GPS nicht", "OK");
                                });
                                MessagingCenter.Send<Application>(App.Current, "StopGPS");
                            }
                            catch (FeatureNotEnabledException fneEx)
                            {
                                Device.BeginInvokeOnMainThread(async () => {
                                    await App.Current.MainPage.DisplayAlert("GPS nicht aktiviert", "GPS ist für diese App nicht aktiviert", "OK");
                                });
                                MessagingCenter.Send<Application>(App.Current, "StopGPS");
                                break;
                            }
                            catch (PermissionException pEx)
                            {
                                Device.BeginInvokeOnMainThread(async () => {
                                    await App.Current.MainPage.DisplayAlert("GPS nicht zugelassen", "Bitte erlauben Sie die GPS-Aktivierung in den Einstellungen der App", "OK");
                                });
                                MessagingCenter.Send<Application>(App.Current, "StopGPS");
                                break;
                            }
                            catch (Exception ex)
                            {
                                Device.BeginInvokeOnMainThread(async () => {
                                    await App.Current.MainPage.DisplayAlert("GPS nicht erreichbar", "Die App konnte nicht auf das GPS zugreifen", "OK");
                                });
                                MessagingCenter.Send<Application>(App.Current, "StopGPS");
                                break;
                            }

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                if (!Compass.IsMonitoring && Device.RuntimePlatform == "iOS")
                                {
                                    Compass.ReadingChanged += Compass_ReadingChanged;
                                    Compass.Start(SensorSpeed.UI, true);
                                }
                            });

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
        /// Stop acquisition using the GPS
        /// </summary>
        public static void StopGPSAsync()
        {
            if (Compass.IsMonitoring && Device.RuntimePlatform == "iOS")
            {
                Compass.Stop();
            }
            App.GpsIsRunning = false;
            if (App.GPSCancellationToken != null && !App.GPSCancellationToken.IsCancellationRequested)
                App.GPSCancellationToken.Cancel();
        }

    }
}
