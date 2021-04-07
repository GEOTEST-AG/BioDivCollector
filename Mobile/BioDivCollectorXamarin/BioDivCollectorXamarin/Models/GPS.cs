using BioDivCollectorXamarin.ViewModels;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
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
            set { 
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
        public async void StartGPSAsync()
        {
            //Wait to get permission before starting GPS
            await Task.Run(async () =>
             {
                 while (true)
                 {

                     if (HasLocationPermission)
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
                         }
                         break;
                     }
                     else
                     {
                         Task.Delay(5000).Wait();
                         GetPermissions();
                     }
                 }
             });

        }

        /// <summary>
        /// React to an update in the GPS position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Locator_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (e.Position.Accuracy < 10000)
                    {
                        Preferences.Set("LastPositionLatitude", e.Position.Latitude);
                        Preferences.Set("LastPositionLongitude", e.Position.Longitude);
                        Dictionary<string, double> dic = new Dictionary<string, double>();
                        dic.Add("latitude", e.Position.Latitude);
                        dic.Add("longitude", e.Position.Longitude);
                        Console.WriteLine(e.Position.Latitude.ToString() + ", " + e.Position.Longitude.ToString() + " +/- " + e.Position.Accuracy);
                        dic.Add("accuracy", e.Position.Accuracy);
                        Preferences.Set("LastPositionAccuracy", e.Position.Accuracy);

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

                        var actualHeading = Math.Abs((e.Position.Heading + deviceRotation) % 360);

                        dic.Add("heading", actualHeading);
                        Preferences.Set("LastPositionHeading", e.Position.Heading);
                        dic.Add("speed", e.Position.Speed);
                        Preferences.Set("LastPositionSpeed", e.Position.Speed);
                        MessagingCenter.Send<GPS, Dictionary<string, double>>(this, "GPSPositionUpdate", dic);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        /// <summary>
        /// Stop acquisition using the GPS
        /// </summary>
        public async void StopGPSAsync()
        {
            var locator = CrossGeolocator.Current;
            locator.PositionChanged -= Locator_PositionChanged;
            await locator.StopListeningAsync();
        }

    }
}
