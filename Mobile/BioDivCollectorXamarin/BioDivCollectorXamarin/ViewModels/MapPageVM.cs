using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.Wms;
using BioDivCollectorXamarin.Views;
using BruTile;
using Mapsui;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.UI;
using Mapsui.Utilities;
using Xamarin.Essentials;
using Xamarin.Forms;
using Exception = System.Exception;

namespace BioDivCollectorXamarin.ViewModels
{
    public class MapPageVM : BaseViewModel
    {
        /// <summary>
        /// The main map
        /// </summary>
        public Mapsui.Map Map;

        /// <summary>
        /// THe main map view
        /// </summary>
        public Mapsui.UI.Forms.MapView VMMapView;

        /// <summary>
        /// The text detailing how many tiles have been saved out of the full amount
        /// </summary>
        private string saveCountText;
        public string SaveCountText
        {
            get { return saveCountText; }
            set
            {
                saveCountText = value;
                OnPropertyChanged("SaveCountText");
            }
        }

        /// <summary>
        /// The GPS Button command
        /// </summary>
        public Command GPSButtonCommand { get; set; }

        /// <summary>
        /// The button command to cancel the creation of a geometry
        /// </summary>
        public Command CancelGeomCommand { get; set; }

        /// <summary>
        /// The button command to save the newly defined geometry
        /// </summary>
        public Command SaveGeomCommand { get; set; }

        /// <summary>
        /// The button command to trigger the creation of a new geometry
        /// </summary>
        public Command AddMapGeometryCommand { get; set; }

        /// <summary>
        /// The button command to cancel saving map tiles
        /// </summary>
        public Command CancelSaveCommand { get; set; }

        /// <summary>
        /// The button command to trigger the maps to be saved
        /// </summary>
        public Command SaveMapCommand { get; set; }

        /// <summary>
        /// Background colour of the add map geometry button (green for active, grey for inactive)
        /// </summary>
        public Color AddMapGeometryButtonBackgroundColour { get; set; }

        /// <summary>
        /// Validation of the add map geometry button
        /// </summary>
        private bool canAddMapGeometry;
        public bool CanAddMapGeometry
        {
            get { return canAddMapGeometry; }
            set { 
                canAddMapGeometry = value;
                OnPropertyChanged("CanAddMapGeometry");
            }
        }

        /// <summary>
        /// The button command for showing the page of map layers
        /// </summary>
        public LayersButtonCommand LayersButtonCommand { get; set; }

        /// <summary>
        /// The GPS button
        /// </summary>
        private Button VMGPSButton;

        /// <summary>
        /// The geometry creation button
        /// </summary>
        private Button VMGeomEditButton;

        /// <summary>
        /// Current GPS position
        /// </summary>
        public Mapsui.UI.Forms.Position CurrentPosition;

        /// <summary>
        /// The page navigation
        /// </summary>
        public INavigation Navigation { get; set; }

        /// <summary>
        /// The GPS object
        /// </summary>
        public GPS gps { get; set; } = new GPS();

        /// <summary>
        /// Timestamp of last object selection
        /// </summary>
        public DateTime LastObjectSelection = DateTime.Now;

        /// <summary>
        /// Validation of the polygon being created
        /// </summary>
        private bool CurrentPolygonSelfIntersecting;

        /// <summary>
        /// The temporary shape creation layer
        /// </summary>
        private ILayer TempLayer;

        /// <summary>
        /// The current geometry type to be created
        /// </summary>
        private string geometryType;

        public string GeometryType
        {
            get { return geometryType; }
            set
            {
                geometryType = value;
                (SaveGeomCommand as Command).ChangeCanExecute();
            }
        }

        /// <summary>
        /// A temporary list of coordinates used during geometry creation
        /// </summary>
        private List<Mapsui.Geometries.Point> tempCoordinates;

        public List<Mapsui.Geometries.Point> TempCoordinates
        {
            get { return tempCoordinates; }
            set
            {
                tempCoordinates = value;
                (SaveGeomCommand as Command).ChangeCanExecute();
            }
        }

        /// <summary>
        /// An observable collection of map layers available within the current project.
        /// </summary>
        public ObservableCollection<MapLayer> MapLayers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public MapPageVM()
        {

        }

        /// <summary>
        /// Initialisation
        /// </summary>
        /// <param name="mapView"></param>
        /// <param name="GPSButton"></param>
        /// <param name="AddMapGeometryButton"></param>
        /// <param name="navigation"></param>
        public MapPageVM(Mapsui.UI.Forms.MapView mapView, Button GPSButton, Button AddMapGeometryButton, INavigation navigation)
        {
            GPSButtonCommand = new Command(GPSButtonPressed, GPSActivated);
            LayersButtonCommand = new LayersButtonCommand(this);
            CancelGeomCommand = new Command(CancelNewGeom, CanCancelNewGeom);
            SaveGeomCommand = new Command(SaveNewGeom, CanSaveNewGeom);
            SaveMapCommand = new Command(SaveMaps, CanSaveMaps);
            AddMapGeometryCommand = new Command(AllowAddNewGeom, AllowAddNewGeomButtonActivated);
            CancelSaveCommand = new Command(CancelSave, AllowCancelSave);
            CanAddMapGeometry = false;
            Navigation = navigation;
            VMGPSButton = GPSButton;
            VMGeomEditButton = AddMapGeometryButton;
            VMGeomEditButton.BackgroundColor = (Color)Application.Current.Resources["BioDivGrey"];
            GeometryType = String.Empty;
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

            ConfigureMap();


            mapView.Map = Map;
            mapView.Refresh();
            mapView.IsNorthingButtonVisible = false;
            mapView.IsMyLocationButtonVisible = false;
            mapView.IsZoomButtonVisible = false;
            mapView.MyLocationLayer.IsMoving = true;
            mapView.MyLocationFollow = false;
            mapView.RotationLock = true;
            mapView.MyLocationEnabled = true;
            mapView.Info += MapOnInfo;
            OSAppTheme currentTheme = Application.Current.RequestedTheme;
            if (currentTheme == OSAppTheme.Dark)
            {
                mapView.Map.BackColor = Mapsui.Styles.Color.Black;
            }
            else
            {
                mapView.Map.BackColor = Mapsui.Styles.Color.White;
            }

            VMMapView = mapView;
            VMMapView.TouchMove += MapView_TouchMove;
            VMMapView.ViewportInitialized += VMMapView_ViewportInitialized;

            TempCoordinates = new List<Mapsui.Geometries.Point>();

            var positionLat = Preferences.Get("LastPositionLatitude", 47.36);
            var positionLong = Preferences.Get("LastPositionLongitude", 8.54);
            Mapsui.Geometries.Point centre = new Mapsui.Geometries.Point(positionLong, positionLat);
            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centre.X, centre.Y);

            VMMapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Forms.Position(centre.Y, centre.X), false);
            VMMapView.Map.Limiter = new ViewportLimiterKeepWithin
            {
                PanLimits = new BoundingBox(SphericalMercator.FromLonLat(-180, -90),SphericalMercator.FromLonLat(180, 90))
            };

            

            try
            {
                MapLayers = MapModel.MakeArrayOfLayers();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Map.Widgets.Add(new Mapsui.Widgets.ScaleBar.ScaleBarWidget(Map) { TextAlignment = Mapsui.Widgets.Alignment.Center, HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom });
            }


            MessagingCenter.Subscribe<Application,string>(App.Current, "TileSaved", (sender,arg1) =>
            {
                SaveCountText = (string)arg1;
            });
            SaveCountText = String.Empty;

            MessagingCenter.Subscribe<Application>(App.Current, "PermissionsChanged", (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    GPSButtonCommand.ChangeCanExecute();
                });
            });

            MessagingCenter.Subscribe<MapPageVM>(this, "ShapeDrawingUndone", (sender) =>
            {
                RefreshShapes();
                TempLayer = MapModel.CreateTempLayer(TempCoordinates);
                Map.Layers.Insert(Map.Layers.Count, TempLayer);
                //SaveGeomCommand.ChangeCanExecute();
            });

            DeviceDisplay.MainDisplayInfoChanged += HandleRotationChange;

            
        }

        /// <summary>
        /// Start the GPS methods running (GPS is always running, if permissions allow, when the map is shown
        /// </summary>
        public void StartGPS()
        {
            MessagingCenter.Subscribe<GPS, Dictionary<string, double>>(this, "GPSPositionUpdate", (sender, arg) =>
            {
                var dic = arg;
                dic.TryGetValue("latitude", out double latitude);
                dic.TryGetValue("longitude", out double longitude);
                dic.TryGetValue("accuracy", out double accuracy);
                dic.TryGetValue("heading", out double heading);
                dic.TryGetValue("speed", out double speed);
                UpdateLocation(latitude, longitude, accuracy, heading, speed);
            });

            gps.GetPermissions();
            gps.StartGPSAsync();
            StopShowingPosition();
        }

        /// <summary>
        /// Stop the GPS methods running. This is used when leaving the page
        /// </summary>
        public void StopGPS()
        {
            gps.StopGPSAsync();
            MessagingCenter.Unsubscribe<GPS>(this, "GPSPositionUpdate");
        }

        /// <summary>
        /// Monitor if the device is on/offline and enable/disable map saving accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            SaveMapCommand.ChangeCanExecute();
        }

        /// <summary>
        /// Compensate for device rotation in the current bearing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleRotationChange(object sender, DisplayInfoChangedEventArgs e)
        {
            var lat = Preferences.Get("LastPositionLatitude", 0.0);
            var lon = Preferences.Get("LastPositionLongitude", 0.0);
            var accuracy = Preferences.Get("LastPositionAccuracy", 0.0);
            var heading = Preferences.Get("LastPositionHeading", 0.0);
            var speed = Preferences.Get("LastPositionSpeed", 0.0);
            Dictionary<string, double> dic = new Dictionary<string, double>();
            dic.Add("latitude", lat);
            dic.Add("longitude", lon);
            Console.WriteLine(lat + ", " + lon + " +/- " + accuracy);
            dic.Add("accuracy", accuracy);
            
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

            var actualHeading = Math.Abs((heading + deviceRotation) % 360);
            dic.Add("heading", actualHeading );
            dic.Add("speed", speed);
            MessagingCenter.Send<GPS, Dictionary<string, double>>(this.gps, "GPSPositionUpdate", dic);
        }


        /// <summary>
        /// Add a temporary geometry to the map
        /// </summary>
        /// <param name="screenPt"></param>
        public void AddTempPoint(Mapsui.UI.Forms.Position screenPt)
        {
            if (CanAddMapGeometry)
            {
                if (TempCoordinates.Count == 0)
                {
                    MessagingCenter.Send<MapPageVM>(this, "SelectGeometryType");
                }
                if (Map.Layers[Map.Layers.Count - 1] == TempLayer)
                {
                    Map.Layers.Remove(TempLayer);
                }
                var mapPt = new Mapsui.Geometries.Point(Convert.ToDouble(screenPt.Longitude), Convert.ToDouble(screenPt.Latitude));
                if (GeometryType == "Punkt")
                {
                    TempCoordinates = new List<Mapsui.Geometries.Point>() { mapPt };
                }
                else if (GeometryType == "Polygon" && TempCoordinates.Count > 0)
                {
                    var prevCoords = new List<Mapsui.Geometries.Point>(TempCoordinates);
                    if (TempCoordinates.Count == 1)
                    {
                        //Complete the polygon
                        TempCoordinates.Add(TempCoordinates[0]);
                    }
                    TempCoordinates.Insert(TempCoordinates.Count-1, mapPt);
                }
                else
                {
                    TempCoordinates.Add(mapPt);
                }
                
                TempLayer = MapModel.CreateTempLayer(TempCoordinates);
                Map.Layers.Insert(Map.Layers.Count, TempLayer);
                (SaveGeomCommand as Command).ChangeCanExecute();
            }        
        }

        /// <summary>
        /// Create the map
        /// </summary>
        private void ConfigureMap()
        {

            this.Map = new Mapsui.Map
            {
                CRS = "EPSG:3857",
                Transformation = new MinimalTransformation(),
            };
        }

        /// <summary>
        /// Centre the map on the last used coordinates (start the map in its last position)
        /// </summary>
        public void ReCentreMap()
        {
            Task.Run(() =>
            {
                var positionLat = Preferences.Get("LastPositionLatitude", 47.36);
                var positionLong = Preferences.Get("LastPositionLongitude", 8.54);
                Mapsui.Geometries.Point centre = new Mapsui.Geometries.Point(positionLong, positionLat);

                var BBLLx = Double.Parse(Preferences.Get("BBLLx", "100"));
                var BBLLy = Double.Parse(Preferences.Get("BBLLy", "100"));
                var BBURx = Double.Parse(Preferences.Get("BBURx", "100"));
                var BBURy = Double.Parse(Preferences.Get("BBURy", "100"));
                if (BBLLx != 100)
                {
                    var bbox = new Mapsui.Geometries.BoundingBox(BBLLx, BBLLy, BBURx, BBURy);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        VMMapView.Navigator.NavigateTo(bbox, ScaleMethod.Fit);
                    });
                }
                var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centre.X, centre.Y);
            });
        }


        /// <summary>
        /// On showing the page, start the GPS, centre the map and refresh the map
        /// </summary>
        public void OnAppearing()
        {
            InitialiseGPS();
            Task.Run(() =>
            {
                ReCentreMap();
                if (Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet)
                    {
                        IsNotConnected = false;
                    }
                    else
                    {
                        IsNotConnected = true;
                    }
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        RefreshAllLayers();
                    });

                

                MessagingCenter.Subscribe<MapLayer, Dictionary<string, int>>(this, "LayerOrderChanged", (sender, arg) =>
                {

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var dic = arg;
                        dic.TryGetValue("oldZ", out int oldZ);
                        dic.TryGetValue("newZ", out int newZ);
                        var oldLayerZ = MapLayers.Count - oldZ;
                        var newLayerZ = MapLayers.Count - newZ;
                        if (newLayerZ < MapLayers.Count && newLayerZ >= 0)
                        {
                            RefreshAllLayers();
                        }
                    });
                });

                if (App.ZoomMapOut)
                {
                    Task.Delay(100).ContinueWith(t => ZoomMapOut());
                }
            });
            
        }

        /// <summary>
        /// Start the GPS and decide whether and how to show the user's position
        /// </summary>
        private void InitialiseGPS()
        {
            StartGPS();
            var gps = Preferences.Get("GPS", false);
            var centred = Preferences.Get("GPS_Centred", false);
            if (gps && centred) { ShowPosition(); }
            else if (gps && !centred) { ShowPositionNotCentered(); }
            else { StopShowingPosition(); }
        }

        /// <summary>
        /// Renew the map layers and zoom to show all geometries
        /// </summary>
        private void ZoomMapOut()
        {
            RenewAllLayers();
            App.ZoomMapOut = false;
        }

        /// <summary>
        /// On leaving the page, stop the GPS and stop listening for messages
        /// </summary>
        public void OnDisappearing()
        {
            StopGPS();
            /*foreach (var layer in Map.Layers)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Map.Layers.Remove(layer);
                });
            }*/
            MessagingCenter.Unsubscribe<MapLayer>(this, "LayerOrderChanged");
        }

        /// <summary>
        /// Remove and replace the geometries
        /// </summary>
        public void RefreshShapes()
        {
            foreach (var layer in Map.Layers)
            {
                if (layer.Name == "Polygons"|| layer.Name == "Lines"||layer.Name == "Points")
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Map.Layers.Remove(layer);
                    });
                }
            }
            var shapeLayers = MapModel.CreateShapes();
            if (shapeLayers != null && shapeLayers.Count > 0)
            {

                ILayer polylayer;
                ILayer linelayer;
                ILayer pointlayer;
                ILayer polylayerNoRecords;
                ILayer linelayerNoRecords;
                ILayer pointlayerNoRecords;
                ILayer allShapesLayer;
                shapeLayers.TryGetValue("polygons", out polylayer);
                shapeLayers.TryGetValue("lines", out linelayer);
                shapeLayers.TryGetValue("points", out pointlayer);
                shapeLayers.TryGetValue("polygonsNoRecords", out polylayerNoRecords);
                shapeLayers.TryGetValue("linesNoRecords", out linelayerNoRecords);
                shapeLayers.TryGetValue("pointsNoRecords", out pointlayerNoRecords);
                shapeLayers.TryGetValue("all", out allShapesLayer);
                Device.BeginInvokeOnMainThread(() =>
                {
                    Map.Layers.Insert(Map.Layers.Count, polylayer);
                    Map.Layers.Insert(Map.Layers.Count, linelayer);
                    Map.Layers.Insert(Map.Layers.Count, pointlayer);
                    Map.Layers.Insert(Map.Layers.Count, polylayerNoRecords);
                    Map.Layers.Insert(Map.Layers.Count, linelayerNoRecords);
                    Map.Layers.Insert(Map.Layers.Count, pointlayerNoRecords);
                });


            }

            var filterGeom = Preferences.Get("FilterGeometry", String.Empty);
            if (filterGeom != String.Empty)
            {
                int geomId;
                int.TryParse(filterGeom, out geomId);
                if (geomId != 0)
                {
                    try
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            var centre = MapModel.GetCentreOfGeometry(geomId);
                            VMMapView.Navigator.NavigateTo(centre, VMMapView.Viewport.Resolution);
                        });
                    }
                    catch
                    {

                    }
                }
                Preferences.Set("FilterGeometry", String.Empty); //Only centre once. After this, forget the filter, and don't keep returning to this position
            }
            else
            {
                ReCentreMap();
            }
        }

        /// <summary>
        /// Replage the geometries, but do not change the map position
        /// </summary>
        public void NewShapes()
        {
            foreach (var layer in Map.Layers)
            {
                if (layer.Name == "Polygons" || layer.Name == "Lines" || layer.Name == "Points" || layer.Name == "PolygonsNoRecords" || layer.Name == "LinesNoRecords" || layer.Name == "PointsNoRecords")
                {
                    Map.Layers.Remove(layer);
                }
            }
            var shapeLayers = MapModel.CreateShapes();
            if (shapeLayers != null && shapeLayers.Count > 0)
            {
                ILayer polylayer;
                ILayer linelayer;
                ILayer pointlayer;
                ILayer polylayerNoRecords;
                ILayer linelayerNoRecords;
                ILayer pointlayerNoRecords;
                ILayer allShapesLayer;
                shapeLayers.TryGetValue("polygons", out polylayer);
                shapeLayers.TryGetValue("lines", out linelayer);
                shapeLayers.TryGetValue("points", out pointlayer);
                shapeLayers.TryGetValue("polygonsNoRecords", out polylayerNoRecords);
                shapeLayers.TryGetValue("linesNoRecords", out linelayerNoRecords);
                shapeLayers.TryGetValue("pointsNoRecords", out pointlayerNoRecords);
                shapeLayers.TryGetValue("all", out allShapesLayer);
                Map.Layers.Insert(Map.Layers.Count, polylayer);
                Map.Layers.Insert(Map.Layers.Count, linelayer);
                Map.Layers.Insert(Map.Layers.Count, pointlayer);
                Map.Layers.Insert(Map.Layers.Count, polylayerNoRecords);
                Map.Layers.Insert(Map.Layers.Count, linelayerNoRecords);
                Map.Layers.Insert(Map.Layers.Count, pointlayerNoRecords);

                if (allShapesLayer != null)
                {
                    VMMapView.Navigator.NavigateTo(allShapesLayer.Envelope, Mapsui.Utilities.ScaleMethod.Fit);
                }
            }
        }


        private static void MapControlFeatureInfo(object sender, FeatureInfoEventArgs e)
        {
            
        }


        /// <summary>
        /// Navigate to show all geometries once the mapview has been initialised
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VMMapView_ViewportInitialized(object sender, EventArgs e)
        {
            var shapeLayers = MapModel.CreateShapes();
            ILayer allShapesLayer;
            shapeLayers.TryGetValue("all", out allShapesLayer);
            if (allShapesLayer != null)
            {
                VMMapView.Navigator.NavigateTo(allShapesLayer.Envelope, Mapsui.Utilities.ScaleMethod.Fit);
            }
            else
            {
                Mapsui.Geometries.Point LL = new Mapsui.Geometries.Point(Convert.ToDouble(5.84), 45.86);
                var sphericalMercatorCoordinateLL = SphericalMercator.FromLonLat(LL.X, LL.Y);
                Mapsui.Geometries.Point UR = new Mapsui.Geometries.Point(Convert.ToDouble(10.56), 47.83);
                var sphericalMercatorCoordinateUR = SphericalMercator.FromLonLat(UR.X, UR.Y);
                VMMapView.Navigator.NavigateTo(new BoundingBox(sphericalMercatorCoordinateLL, sphericalMercatorCoordinateUR), Mapsui.Utilities.ScaleMethod.Fit);
            }

        }

        /// <summary>
        /// Add layers from the map layer stack into the map
        /// </summary>
        private void AddLayersToMap()
        {
            
            Map.Layers.Insert(0, MapModel.GetBaseMap().MapsuiLayer);
            
            foreach (var layer in MapLayers)
            {
                if (layer != null && layer.Enabled && layer.LayerZ < Map.Layers.Count && layer.LayerZ >= 0)
                {
                    Map.Layers.Insert(1,layer.MapsuiLayer);
                }
                else if (layer != null && layer.Enabled && layer.LayerZ >= Map.Layers.Count)
                {
                    Map.Layers.Add(layer.MapsuiLayer);
                }
            }
        }

        /// <summary>
        /// Replace both map layers and shape
        /// </summary>
        private void RefreshAllLayers()
        {
            RefreshMapLayers();

            RefreshShapes();

        }

        /// <summary>
        /// Replace the map layers and shapes, but do not move the focus point of the map
        /// </summary>
        private void RenewAllLayers()
        {
            RefreshMapLayers();

            NewShapes();

            SetBoundingBox();
        }


        /// <summary>
        /// Remove and replace the map layers and add the scale widget on top
        /// </summary>
        private void RefreshMapLayers()
        {
            foreach (var layer in Map.Layers)
            {
                if (layer != null && layer.GetType() != typeof(Mapsui.UI.Objects.MyLocationLayer))
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Map.Layers.Remove(layer);
                    });
                }
            }

            try
            {
                MapLayers = MapModel.MakeArrayOfLayers();
                Device.BeginInvokeOnMainThread(() =>
                {
                    AddLayersToMap();
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Map.Widgets.Add(new Mapsui.Widgets.ScaleBar.ScaleBarWidget(Map) { TextAlignment = Mapsui.Widgets.Alignment.Center, HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom });
                });
            }
        }

        /// <summary>
        /// If permissions allow, show the GPS position in the map and keep the GPS point centred
        /// </summary>
        public void ShowPosition()
        {
            VMMapView.MyLocationFollow = gps.HasLocationPermission;
            VMMapView.MyLocationEnabled = gps.HasLocationPermission;
            VMGPSButton.ImageSource = "outline_gps_fixed_white_24dp.png";
            if (CurrentPosition != null && (CurrentPosition.Latitude != 0 && CurrentPosition.Longitude != 0))
            {
                VMMapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Forms.Position(CurrentPosition.Latitude, CurrentPosition.Longitude),false);
            }
        }

        /// <summary>
        /// If permissions allow, show the GPS position in the map, but keep the map centred on the same position
        /// </summary>
        public void ShowPositionNotCentered()
        {
            VMMapView.MyLocationFollow = false;
            VMMapView.MyLocationEnabled = gps.HasLocationPermission;
            VMGPSButton.ImageSource = "outline_gps_not_fixed_white_24dp.png";
            if (CurrentPosition != null && (CurrentPosition.Latitude != 0 && CurrentPosition.Longitude != 0))
            {
                VMMapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Forms.Position(CurrentPosition.Latitude, CurrentPosition.Longitude), false);
            }
        }

        /// <summary>
        /// Stop showing the GPS position in the map
        /// </summary>
        public void StopShowingPosition()
        {
            VMMapView.MyLocationEnabled = false;
            VMMapView.MyLocationFollow = false;
            VMGPSButton.ImageSource = "outline_gps_off_white_24dp.png";
        }

        
        /// <summary>
        /// Keep track of the bounding box when the map is scrolled, and set the GPS to 'not centred'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapView_TouchMove(object sender, Mapsui.UI.TouchedEventArgs e)
        {
            SetBoundingBox();
            VMMapView.MyLocationFollow = false;
            Preferences.Set("GPS_Centred", false);
            if (VMMapView.MyLocationEnabled)
            {
                VMGPSButton.ImageSource = "outline_gps_not_fixed_white_24dp.png";
            }
        }

        /// <summary>
        /// Update the current GPS location on receiving a notification from the GPS object
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="accuracy"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        private void UpdateLocation(double latitude, double longitude, double accuracy, double heading, double speed)
        {
            if (latitude != 0 && longitude != 0)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (accuracy < 50)
                        {
                            //Animate when close enough to real location
                            VMMapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Forms.Position(latitude, longitude), true);
                        }
                        else
                        {
                            //Don't animate when moving to location
                            VMMapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Forms.Position(latitude, longitude), false);
                        }
                        
                        VMMapView.MyLocationLayer.UpdateMyDirection(heading, 0);
                        VMMapView.MyLocationLayer.UpdateMySpeed(speed);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                });
            }

        }

        /// <summary>
        /// Save the current bounding box to the preferences so that it may later be reinstated
        /// </summary>
        private void SetBoundingBox()
        {
            var bb = VMMapView.Viewport.Extent;
            Preferences.Set("BBLLx", bb.BottomLeft.X.ToString());
            Preferences.Set("BBLLy", bb.BottomLeft.Y.ToString());
            Preferences.Set("BBURx", bb.TopRight.X.ToString());
            Preferences.Set("BBURy", bb.TopRight.Y.ToString());
        }

        /// <summary>
        /// Go to the map layers page
        /// </summary>
        public void ShowLayerList()
        {
            Navigation.PushAsync(new MapLayersPage(MapLayers),true);
        }

        /// <summary>
        /// On touching a MapInfo object, parse out the object Id and go to that object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void MapOnInfo(object sender, MapInfoEventArgs args)
        {
            if (DateTime.Now > LastObjectSelection.AddSeconds(1)) //ensure that only 1 selection is made at one time
            {
                LastObjectSelection = DateTime.Now;
                var featureName = args.MapInfo.Feature?["Name"]?.ToString();
                int geomId;
                Int32.TryParse((string)featureName, out geomId);
                if (geomId > 0)
                {
                    var objId = geomId.ToString();
                    App.CurrentRoute = "//Records?objectId=" + objId;
                    Shell.Current.GoToAsync($"//Records?objectId={objId}", true);
                }
            }
        }

        /// <summary>
        /// On pressing the gps button, set the relevant show/notshow, centre/not centre setting
        /// </summary>
        private void GPSButtonPressed()
        {
            var gps = Preferences.Get("GPS", false);
            var centred = Preferences.Get("GPS_Centred", false);
            if (!centred || !gps)
            {
                ShowPosition();
                Preferences.Set("GPS", true);
                Preferences.Set("GPS_Centred", true);
            }
            else
            {
                StopShowingPosition();
                Preferences.Set("GPS", false);
                Preferences.Set("GPS_Centred", false);
            }
        }

        /// <summary>
        /// Check to see if the app has the relevant gps permissions and enable/disable the button
        /// </summary>
        /// <returns>enabled/disabled</returns>
        private bool GPSActivated()
        {
            return gps.HasLocationPermission;
        }

        /// <summary>
        /// On canelling geometry creation, remove the temp layer
        /// </summary>
        private void CancelNewGeom()
        {
            CancelAddingMapGeometry();
        }

        /// <summary>
        /// Validate the geometry cancel button (always true)
        /// </summary>
        /// <returns>true</returns>
        private bool CanCancelNewGeom()
        {
            return true;
        }

        /// <summary>
        /// Cancel saving map tiles
        /// </summary>
        private void CancelSave()
        {
            MessagingCenter.Send<Application>(App.Current, "CancelMapSave");
        }

        /// <summary>
        /// Validate the map saving cancel button (always true)
        /// </summary>
        /// <returns>true</returns>
        private bool AllowCancelSave()
        {
            return true;
        }

        /// <summary>
        /// Save the temporary geometry
        /// </summary>
        private void SaveNewGeom()
        {
            MessagingCenter.Subscribe<MapPage,string>(this, "GeometryName", (sender,arg) =>
            {
                var geomName = arg as string;
                ReferenceGeometry.SaveGeometry(TempCoordinates, geomName);
                RemoveTempGeometry();
                RefreshShapes();
                MessagingCenter.Unsubscribe<MapPage, string>(this, "GeometryName");
            });
            Mapsui.Geometries.Point point = TempCoordinates[0];
            var coords = point.ToDoubleArray();
            var coordString = coords[1].ToString("#.000#") + ", " + coords[0].ToString("#.000#");
            MessagingCenter.Send<MapPageVM,string>(this, "RequestGeometryName", coordString);
        }

        /// <summary>
        /// Validate whether the new (temp) geometry can be saved - is it valid?
        /// </summary>
        /// <returns>valid/not valid</returns>
        private bool CanSaveNewGeom()
        {
            if (GeometryType == "Punkt")
            {
                return TempCoordinates.Count > 0;
            }
            else if (GeometryType == "Linie")
            {
                return TempCoordinates.Count > 1;
            }
            else {
                var isValid = MapModel.CheckValidityOfPolygon(TempCoordinates);
                if (TempCoordinates.Count <= 3) { CurrentPolygonSelfIntersecting = false; }
                if (TempCoordinates.Count > 3 && isValid)
                {
                    CurrentPolygonSelfIntersecting = false;
                    return true;
                }
                else
                {
                    if (TempCoordinates.Count > 3)
                    {
                        if (!CurrentPolygonSelfIntersecting) //Polyon was not previously self-intersecting
                        {
                            CurrentPolygonSelfIntersecting = true;
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                bool ok = await App.Current.MainPage.DisplayAlert("Das aktuelle Polygon schneidet sich selbst", "Solange es eine ungültige Geometrie hat kann es nicht gespeichert werden", "Weiter zeichnen", "Rückgängig machen");
                                if (!ok)
                                {
                                    try
                                    {
                                        TempCoordinates.RemoveAt(TempCoordinates.Count - 2);
                                        CurrentPolygonSelfIntersecting = false;
                                        MessagingCenter.Send<MapPageVM>(this, "ShapeDrawingUndone");
                                        SaveGeomCommand.ChangeCanExecute();
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Had a problem undoing the shape: " + e.Message);
                                    }
                                }
                            });
                        }
                    }
                    
                    
                    return false;
                }
            }
        }


        /// <summary>
        /// Determine whether we are in edit(create) mode. If we are, disable clicking on existing layers and turn the button green
        /// </summary>
        private void AllowAddNewGeom()
        {
            CanAddMapGeometry = !CanAddMapGeometry;
            if (CanAddMapGeometry == false)
            {
                CancelAddingMapGeometry();
            }
            else
            {
                foreach (var layer in Map.Layers)
                {
                    if (layer.Name == "Polygons" || layer.Name == "Lines" || layer.Name == "Points")
                    {
                        layer.IsMapInfoLayer = false;  //Ensure that the app doesn't try to select the temp layer when clicking on it
                    }
                }
            }

            if (canAddMapGeometry)
            {
                VMGeomEditButton.BackgroundColor = (Color)Application.Current.Resources["BioDivGreen"];
            }
            else
            {
                VMGeomEditButton.BackgroundColor = (Color)Application.Current.Resources["BioDivGrey"];
            }
        }

        private void CancelAddingMapGeometry()
        {
            RemoveTempGeometry();
            foreach (var layer in Map.Layers)
            {
                if (layer.Name == "Polygons" || layer.Name == "Lines" || layer.Name == "Points")
                {
                    layer.IsMapInfoLayer = true;  //Make the layer selectable
                }
            }
        }

        /// <summary>
        /// Validate whether a new geometry can be saved (always true)
        /// </summary>
        /// <returns>true</returns>
        private bool AllowAddNewGeomButtonActivated()
        {
            return true;
        }

        /// <summary>
        /// Remove the temporary geometry (on save or cancel)
        /// </summary>
        private void RemoveTempGeometry()
        {
            TempCoordinates = new List<Mapsui.Geometries.Point>();
            if (Map.Layers[Map.Layers.Count - 1] == TempLayer)
            {
                Map.Layers.Remove(TempLayer);
            }
            CanAddMapGeometry = false;
            GeometryType = String.Empty;
            VMGeomEditButton.BackgroundColor = (Color)Application.Current.Resources["BioDivGrey"];
        }

        /// <summary>
        /// Save the map tiles to mbtiles files
        /// </summary>
        public void SaveMaps()
        {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SaveCountText = "Berechnet welche Kacheln zu speichern sind";
                });

                BoundingBox bb = VMMapView.Viewport.Extent;
                var extent = new Extent(bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
                MapModel.saveMaps(extent);
        }

        /// <summary>
        /// Validate whether maps can be saved (is the device online?)
        /// </summary>
        /// <returns></returns>
        private bool CanSaveMaps()
        {
            return IsConnected;
        }
    }

    /// <summary>
    /// The command for showing the map layers page
    /// </summary>
    public class LayersButtonCommand : ICommand
    {
        public MapPageVM MapPageViewModel { get; set; }

        public LayersButtonCommand(MapPageVM mapPageVM)
        {
            MapPageViewModel = mapPageVM;
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
            MapPageViewModel.ShowLayerList();
        }
    }


    /// <summary>
    /// The command for showing the records page on selecting a geometry
    /// </summary>
    public class AddRecordButtonCommand : ICommand
    {
        public ObjectsPageVM ObjectsPageViewModel { get; set; }

        public AddRecordButtonCommand(ObjectsPageVM objectsPageVM)
        {
            ObjectsPageViewModel = objectsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }


        public bool CanExecute(object parameter)
        {
            return App.CurrentProjectId != null && App.CurrentProjectId != String.Empty;
        }

        public void Execute(object parameter)
        {
            MessagingCenter.Send<AddRecordButtonCommand>(this, "AddRecord");
        }
    }
}
