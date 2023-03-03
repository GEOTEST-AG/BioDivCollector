using BioDivCollectorXamarin.ViewModels;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
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

        /// <summary>
        /// Retrieve and request permissions for GPS use
        /// </summary>
        public async void GetPermissions()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != Xamarin.Essentials.PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
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
            Task.Run(async() =>
             {
                 while (Preferences.Get("GPS", false))
                 {
                     Thread.Sleep(2000);
                     if (HasLocationPermission)
                     {
                         try {
                             
                             App.GPSCancellationToken = new CancellationTokenSource();
                             var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                             var location = await Geolocation.GetLocationAsync(request,App.GPSCancellationToken.Token);

                             if (location != null)
                              {
                                  Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");

                                      try
                                      {
                                          if (location.Accuracy != null && location.Accuracy < 10000)
                                          {
                                              Preferences.Set("LastPositionLatitude", location.Latitude);
                                              Preferences.Set("LastPositionLongitude", location.Longitude);
                                              Dictionary<string, double> dic = new Dictionary<string, double>();
                                              dic.Add("latitude", location.Latitude);
                                              dic.Add("longitude", location.Longitude);
                                              Console.WriteLine(location.Latitude.ToString() + ", " + location.Longitude.ToString() + " +/- " + location.Accuracy);
                                              dic.Add("accuracy", (double)location.Accuracy);
                                              Preferences.Set("LastPositionAccuracy", (double)location.Accuracy);

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
                             await App.Current.MainPage.DisplayAlert("GPS wird nicht unterstützt", "Dieses Gerät unterstützt GPS nicht", "OK");
                          }
                          catch (FeatureNotEnabledException fneEx)
                          {
                             await App.Current.MainPage.DisplayAlert("GPS nicht aktiviert", "GPS ist für diese App nicht aktiviert", "OK");
                         }
                          catch (PermissionException pEx)
                          {
                             await App.Current.MainPage.DisplayAlert("GPS nicht zugelassen", "Bitte erlauben Sie die GPS-Aktivierung in den Einstellungen der App", "OK");
                         }
                          catch (Exception ex)
                          {
                             await App.Current.MainPage.DisplayAlert("GPS nicht erreichbar", "Die App konnte nicht auf das GPS zugreifen", "OK");
                         }

                         if (!Compass.IsMonitoring)
                         {
                             Compass.ReadingChanged += Compass_ReadingChanged;
                             Compass.Start(SensorSpeed.UI, true);
                         }
                     }
                     else
                     {
                         Task.Delay(5000).Wait();
                         GetPermissions();
                     }
                 }
             });

        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var deviceRotation = 0;

            switch (DeviceDisplay.MainDisplayInfo.Rotation.ToString())
            {
                case "Rotation0":
                    deviceRotation = 0;
                    break;
                case "Rotation90":
                    deviceRotation = 90;
                    break;
                case "Rotation180":
                    deviceRotation = 180;
                    break;
                case "Rotation270":
                    deviceRotation = 270;
                    break;
                default:
                    break;
            }

            var lastHeadingString = Preferences.Get("LastPositionHeading", "0");
            double.TryParse(lastHeadingString, out var lastHeading);
            if (Math.Abs(lastHeading - e.Reading.HeadingMagneticNorth) > 1) 
            {
                Preferences.Set("LastPositionHeading", e.Reading.HeadingMagneticNorth);
                MessagingCenter.Send<GPS>(this, "BearingUpdate");
            }
        }

        /// <summary>
        /// Stop acquisition using the GPS
        /// </summary>
        public static void StopGPSAsync()
        {
            if (App.GPSCancellationToken != null && !App.GPSCancellationToken.IsCancellationRequested)
                App.GPSCancellationToken.Cancel();
        }

    }
}
