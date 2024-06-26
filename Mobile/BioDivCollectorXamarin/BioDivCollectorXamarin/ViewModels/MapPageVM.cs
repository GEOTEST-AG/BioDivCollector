﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Views;
using BruTile;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Projections;
using Mapsui.UI;
using Mapsui.Utilities;
using MathNet.Numerics.Statistics;
using Plugin.Geolocator;
using Xamarin.Essentials;
using Xamarin.Forms;
using Exception = System.Exception;
using Mapsui.Limiting;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using Mapsui.Extensions;
using Mapsui.Nts.Extensions;

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
        /// The button command to undo the last drawn point of a geometry
        /// </summary>
        public Command UndoGeomCommand { get; set; }

        /// <summary>
        /// The button command to clear a temporary geometry
        /// </summary>
        public Command ClearGeomCommand { get; set; }

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
        public Xamarin.Forms.Color AddMapGeometryButtonBackgroundColour { get; set; }

        /// <summary>
        /// Validation of the add map geometry button
        /// </summary>
        private bool canAddMapGeometry;
        public bool CanAddMapGeometry
        {
            get { return canAddMapGeometry; }
            set
            {
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

        private ILayer GPSPointLayer;
        private ILayer GPSLayer;
        private ILayer BearingLayer;

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
        /// The geometry to edit
        /// </summary>
        public int GeomToEdit;

        /// <summary>
        /// A bool to govern when layers can be generated to avoid flicker
        /// </summary>
        private bool IsGeneratingLayer;

        /// <summary>
        /// A temporary list of coordinates used during geometry creation
        /// </summary>
        private List<Mapsui.MPoint> tempCoordinates;

        public List<Mapsui.MPoint> TempCoordinates
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

        public static Queue<(double, double)> GPSPointsQueue { get; set; } = new Queue<(double, double)>(
            new (double, double)[]
            {
                (0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0),(0.0,0.0)
            });

        /// <summary>
        /// The text indicating that the geometries are sill loading
        /// </summary>
        private string geomsLoadingText;
        public string GeomsLoadingText
        {
            get { return geomsLoadingText; }
            set
            {
                geomsLoadingText = value;
                if (value == String.Empty)
                    IsLoading = false;
                else
                    IsLoading = true;
                OnPropertyChanged("GeomsLoadingText");
            }
        }

        private bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                OnPropertyChanged();
            }
        }

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
            UndoGeomCommand = new Command(UndoLastTempPoint, CanUndoNewGeom);
            ClearGeomCommand = new Command(ClearNewGeom, CanClearNewGeom);
            SaveMapCommand = new Command(SaveMaps, CanSaveMaps);
            AddMapGeometryCommand = new Command(AllowAddNewGeom, AllowAddNewGeomButtonActivated);
            CancelSaveCommand = new Command(CancelSave, AllowCancelSave);
            CanAddMapGeometry = false;
            Navigation = navigation;
            VMGPSButton = GPSButton;
            VMGeomEditButton = AddMapGeometryButton;
            VMGeomEditButton.BackgroundColor = (Xamarin.Forms.Color)Application.Current.Resources["BioDivGrey"];
            GeometryType = String.Empty;
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

            ConfigureMap();


            mapView.Map = Map;
            mapView.Refresh();
            mapView.IsNorthingButtonVisible = false;
            mapView.IsMyLocationButtonVisible = false;
            mapView.IsZoomButtonVisible = false;
            mapView.MyLocationLayer.IsMoving = false;
            mapView.MyLocationFollow = false;
            mapView.RotationLock = true;
            mapView.MyLocationEnabled = false;
            mapView.MyLocationLayer.Enabled = false;
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
            //VMMapView.ViewportInitialized += VMMapView_ViewportInitialized;

            TempCoordinates = new List<Mapsui.MPoint>();

            var positionLat = Preferences.Get("LastPositionLatitude", 47.36);
            var positionLong = Preferences.Get("LastPositionLongitude", 8.54);
            Mapsui.MPoint centre = new Mapsui.MPoint(positionLong, positionLat);
            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centre.X, centre.Y);

            MRect mRect = new MRect(SphericalMercator.FromLonLat(-180, -90).x, SphericalMercator.FromLonLat(-180, -90).y, SphericalMercator.FromLonLat(180, 90).x, SphericalMercator.FromLonLat(180, 90).y);

            VMMapView.Map.Navigator.ZoomToBox(mRect);

            Task.Run(async () =>
            {
                var allMapLayers = await MapModel.MakeArrayOfLayers();
                MapLayers = new ObservableCollection<MapLayer>(new List<MapLayer>());
                MapLayers = new ObservableCollection<MapLayer>(allMapLayers);
                Map.Widgets.Enqueue(new Mapsui.Widgets.ScaleBar.ScaleBarWidget(Map) { TextAlignment = Mapsui.Widgets.Alignment.Center, HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom });
            });


            //try
            //{
            //    Task.Run(async () => { MapLayers = await MapModel.MakeArrayOfLayers(); });
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
            //finally
            //{
            //    Map.Widgets.Add(new Mapsui.Widgets.ScaleBar.ScaleBarWidget(Map) { TextAlignment = Mapsui.Widgets.Alignment.Center, HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom });
            //}


            MessagingCenter.Subscribe<Application, string>(App.Current, "TileSaved", (sender, arg1) =>
            {
                SaveCountText = (string)arg1;
            });
            MessagingCenter.Subscribe<Application, string>(App.Current, "DownloadComplete", (sender, arg1) =>
            {
                Preferences.Set("FilterGeometry", String.Empty);
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                    Map.Layers.Remove(TempLayer);
                    }
                    catch (Exception ex)
                    {

                    }
                    TempLayer = MapModel.CreateTempLayer(TempCoordinates);
                    Map.Layers.Insert(Map.Layers.Count, TempLayer);
                });
            });

            MessagingCenter.Unsubscribe<Application, string>(App.Current, "EditGeometry");
            MessagingCenter.Subscribe<Application, int>(App.Current, "EditGeometry", async (sender, arg) =>
            {
                GeomToEdit = arg;
                var tempGeom = await ReferenceGeometry.GetGeometry(GeomToEdit);
                NetTopologySuite.Geometries.Coordinate[] tempPoints = DataDAO.GeoJSON2Geometry(tempGeom.geometry).Coordinates;
                List<Mapsui.MPoint> coordList = tempPoints.Select(c => new Mapsui.MPoint(c.X, c.Y)).ToList();
                GeometryType = ReferenceGeometry.FindGeometryTypeFromCoordinateList(coordList);
                TempCoordinates = coordList; //Not initially assigned to tempCoordinates, as we need to first know GeometryType to decide whether the save command can run
            });

            MessagingCenter.Unsubscribe<Application>(App.Current, "StopGPS");
            MessagingCenter.Subscribe<Application>(App.Current, "StopGPS", (sender) =>{
                StopShowingPosition();
            });

            MessagingCenter.Unsubscribe<Application, string>(App.Current, "SyncMessage");
            MessagingCenter.Subscribe<Application, string>(App.Current, "SyncMessage", (sender, arg) =>
            {
                GeomsLoadingText = arg;
            });

            DeviceDisplay.MainDisplayInfoChanged += HandleRotationChange;

            InitialiseGPS();
        }

        /// <summary>
        /// Start the GPS methods running (GPS is always running, if permissions allow, when the map is shown
        /// </summary>
        public void StartGPS()
        {
            MessagingCenter.Subscribe<GPS>(this, "GPSPositionUpdate", (sender) =>
            {
                var gps = Preferences.Get("GPS", false);
                var centred = Preferences.Get("GPS_Centred", false);
                if (gps)
                {
                    UpdateLocation();
                }
            });

            MessagingCenter.Subscribe<GPS>(this, "BearingUpdate", (sender) =>
            {
                UpdateLocation();
            });

            App.Gps.GetPermissions();
            App.Gps.StartGPSAsync();
            StopShowingPosition();
        }

        /// <summary>
        /// Stop the GPS methods running. This is used when leaving the page
        /// </summary>
        public void StopGPS()
        {
            GPS.StopGPSAsync();
            RemoveGPSLayers();
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
            try
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
                dic.Add("heading", actualHeading);
                dic.Add("speed", speed);
                MessagingCenter.Send<GPS>(App.Gps, "GPSPositionUpdate");
            }
            catch
            {
            }
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

                var mapLayer = Map.Layers.Where(x => x == TempLayer).FirstOrDefault();
                if (mapLayer != null)
                    Map.Layers.Remove(mapLayer);

                var mapPt = new Mapsui.MPoint(Convert.ToDouble(screenPt.Longitude), Convert.ToDouble(screenPt.Latitude));
                if (GeometryType == "Punkt")
                {
                    TempCoordinates = new List<Mapsui.MPoint>() { mapPt };
                }
                else if (GeometryType == "Polygon" && TempCoordinates.Count > 0)
                {
                    var prevCoords = new List<Mapsui.MPoint>(TempCoordinates);
                    if (TempCoordinates.Count == 1)
                    {
                        //Complete the polygon
                        TempCoordinates.Add(TempCoordinates[0]);
                    }
                    TempCoordinates.Insert(TempCoordinates.Count - 1, mapPt);
                }
                else
                {
                    TempCoordinates.Add(mapPt);
                }

                TempLayer = MapModel.CreateTempLayer(TempCoordinates);
                Map.Layers.Insert(Map.Layers.Count, TempLayer);

                try
                {
                    (SaveGeomCommand as Command).ChangeCanExecute();
                    (ClearGeomCommand as Command).ChangeCanExecute();
                    (UndoGeomCommand as Command).ChangeCanExecute();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        public async Task AddLongClickPoint(Mapsui.UI.Forms.Position screenPt)
        {
            Mapsui.MPoint mapPt = new Mapsui.MPoint(Convert.ToDouble(screenPt.Longitude), Convert.ToDouble(screenPt.Latitude));
            List<Mapsui.MPoint> tempCoordinates = new List<Mapsui.MPoint>() { mapPt };
            string fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
            string date = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString("D2") + "-" + DateTime.Now.Day.ToString("D2") + "T" + DateTime.Now.ToLongTimeString();
            string geomId = await ReferenceGeometry.SaveGeometry(tempCoordinates, fullUserName + "_" + date);

            ReferenceGeometry geom = await ReferenceGeometry.GetGeometry(geomId);

            if (geom != null)
            {
                //Wait to ensure that the records page has been created before sending the GenerateNewForm message
                MessagingCenter.Subscribe<Application>(App.Current, "RecordsPageReady", async (sender) =>
                {
                    MessagingCenter.Send<MapPageVM, string>(this, "GenerateNewForm", geomId);
                    MessagingCenter.Unsubscribe<Application>(App.Current, "RecordsPageReady");
                });
                await Shell.Current.GoToAsync($"//Records?objectId={geom.Id}", true);
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
                //Transformation = new MinimalTransformation(),
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
                Mapsui.MPoint centre = new Mapsui.MPoint(positionLong, positionLat);

                var BBLLx = Double.Parse(Preferences.Get("BBLLx", "100"));
                var BBLLy = Double.Parse(Preferences.Get("BBLLy", "100"));
                var BBURx = Double.Parse(Preferences.Get("BBURx", "100"));
                var BBURy = Double.Parse(Preferences.Get("BBURy", "100"));
                if (BBLLx != 100)
                {
                    var bbox = new Mapsui.MRect(BBLLx, BBLLy, BBURx, BBURy);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        VMMapView.Map.Navigator.ZoomToBox(bbox);
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
            //Task.Run(() =>
            //{
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
                        RenewAllLayers();
                    }
                    InitialiseGPS();
                });
            });

            if (App.ZoomMapOut)
            {
                Task.Delay(100).ContinueWith(t => ZoomMapOut());
            }
            //});

        }

        /// <summary>
        /// Allow editing of a predefined geometry
        /// </summary>
        private async Task ConfigureGeometryForEditing()
        {
            if (GeomToEdit > 0)
            {
                try
                {
                    var tempGeom = await ReferenceGeometry.GetGeometry(GeomToEdit);
                    NetTopologySuite.Geometries.Coordinate[] tempPoints = DataDAO.GeoJSON2Geometry(tempGeom.geometry).Coordinates;
                    List<Mapsui.MPoint> coordList = tempPoints.Select(c => new Mapsui.MPoint(c.X, c.Y)).ToList();
                    GeometryType = ReferenceGeometry.FindGeometryTypeFromCoordinateList(coordList);
                    TempCoordinates = coordList; //Not initially assigned to tempCoordinates, as we need to first know GeometryType to decide whether the save command can run

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        TempLayer = MapModel.CreateTempLayer(TempCoordinates);
                        Map.Layers.Insert(Map.Layers.Count, TempLayer);

                        AllowAddNewGeom();

                        SaveGeomCommand.ChangeCanExecute();
                        ClearGeomCommand.ChangeCanExecute();
                        UndoGeomCommand.ChangeCanExecute();

                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
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
            MessagingCenter.Unsubscribe<MapLayer>(this, "LayerOrderChanged");
        }

        /// <summary>
        /// Remove and replace the geometries
        /// </summary>
        public async Task RefreshShapes()
        {
            foreach (var layer in Map.Layers)
            {
                if (layer.Name == "Polygons" || layer.Name == "Lines" || layer.Name == "Points")
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Map.Layers.Remove(layer);
                    });
                }
            }
            var shapeLayers = await MapModel.CreateShapes();
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

                await ConfigureGeometryForEditing();
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
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            var centre = await MapModel.GetCentreOfGeometry(geomId);
                            if (centre != null)
                            {
                                VMMapView.Map.Navigator.FlyTo(centre, VMMapView.Map.Navigator.Resolutions.FirstOrDefault());
                                SetBoundingBox();
                            }
                            else
                            {
                                ReCentreMap();
                            }
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    ReCentreMap();
                });
            }
        }

        /// <summary>
        /// Replage the geometries, but do not change the map position
        /// </summary>
        public async Task NewShapes()
        {
            foreach (var layer in Map.Layers)
            {
                if (layer.Name == "Polygons" || layer.Name == "Lines" || layer.Name == "Points" || layer.Name == "PolygonsNoRecords" || layer.Name == "LinesNoRecords" || layer.Name == "PointsNoRecords")
                {
                    Map.Layers.Remove(layer);
                }
            }
            var shapeLayers = await MapModel.CreateShapes();
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
                    //VMMapView.Navigator.NavigateTo(allShapesLayer.Envelope, Mapsui.Utilities.ScaleMethod.Fit);
                    if (allShapesLayer.Extent.Width > 400 || allShapesLayer.Extent.Height > 400)
                    {
                        VMMapView.Map.Navigator.ZoomToBox(GetBoundingBoxWithBuffer(allShapesLayer), MBoxFit.Fit);
                    }
                    else
                    {
                        //Make sure it doesn't zoom in too far if e.g. there is only one point
                        MRect bb = new MRect(allShapesLayer.Extent.Centroid.X - 200, allShapesLayer.Extent.Centroid.Y - 200, allShapesLayer.Extent.Centroid.X + 200, allShapesLayer.Extent.Centroid.Y + 200);
                        VMMapView.Map.Navigator.ZoomToBox(bb, MBoxFit.Fit);
                    }
                }
            }
            await ConfigureGeometryForEditing();
        }


        private static void MapControlFeatureInfo(object sender, FeatureInfoEventArgs e)
        {

        }


        /// <summary>
        /// Navigate to show all geometries once the mapview has been initialised
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void VMMapView_ViewportInitialized(object sender, EventArgs e)
        {
            var shapeLayers = await MapModel.CreateShapes();
            ILayer allShapesLayer;
            shapeLayers.TryGetValue("all", out allShapesLayer);
            if (allShapesLayer != null)
            {
                //VMMapView.Navigator.NavigateTo(allShapesLayer.Envelope, Mapsui.Utilities.ScaleMethod.Fit);
                if (allShapesLayer.Extent.Width > 400 || allShapesLayer.Extent.Height > 400)
                {
                    VMMapView.Map.Navigator.ZoomToBox(GetBoundingBoxWithBuffer(allShapesLayer), MBoxFit.Fit, 2);
                }
                else
                {
                    //Make sure it doesn't zoom in too far if e.g. there is only one point
                    MRect bb = new MRect(allShapesLayer.Extent.Centroid.X - 200, allShapesLayer.Extent.Centroid.Y - 200, allShapesLayer.Extent.Centroid.X + 200, allShapesLayer.Extent.Centroid.Y + 200);
                    VMMapView.Map.Navigator.ZoomToBox(bb, MBoxFit.Fit);
                }
            }
            else
            {
                Mapsui.MPoint LL = new Mapsui.MPoint(Convert.ToDouble(5.84), 45.86);
                var sphericalMercatorCoordinateLL = SphericalMercator.FromLonLat(LL.X, LL.Y);
                Mapsui.MPoint UR = new Mapsui.MPoint(Convert.ToDouble(10.56), 47.83);
                var sphericalMercatorCoordinateUR = SphericalMercator.FromLonLat(UR.X, UR.Y);
                VMMapView.Map.Navigator.ZoomToBox(new MRect(sphericalMercatorCoordinateLL.x, sphericalMercatorCoordinateLL.y, sphericalMercatorCoordinateUR.x, sphericalMercatorCoordinateUR.y), MBoxFit.Fit);
            }
        }

        private MRect GetBoundingBoxWithBuffer(ILayer layer)
        {
            var width = (layer.Extent.Width / 2) * 1.2;
            var height = (layer.Extent.Height / 2) * 1.2;
            MRect bb = new MRect(layer.Extent.Centroid.X - width, layer.Extent.Centroid.Y - height, layer.Extent.Centroid.X + width, layer.Extent.Centroid.Y + height);
            return bb;
        }

        /// <summary>
        /// Add layers from the map layer stack into the map
        /// </summary>
        private void AddLayersToMap()
        {

            try
            {
                Map.Layers.Insert(0, MapModel.GetBaseMap().MapsuiLayer);
            }
            catch
            {
                //base layer not drawn
            }

            try
            {
                foreach (var layer in MapLayers.OrderByDescending(m => m.LayerZ))
                {
                    Map.Layers.Add(layer.MapsuiLayer);
                }
            }
            catch
            {
                //layer not drawn
            }
        }

        /// <summary>
        /// Replace both map layers and shape
        /// </summary>
        private void RefreshAllLayers()
        {
            Task.Run(async () =>
            {
                await RefreshMapLayers();
                await RefreshShapes();
            });

        }

        /// <summary>
        /// Replace the map layers and shapes, but do not move the focus point of the map
        /// </summary>
        private void RenewAllLayers()
        {
            Task.Run(async () =>
            {
                await RefreshMapLayers();
                await NewShapes();
            });

            //SetBoundingBox();
        }


        /// <summary>
        /// Remove and replace the map layers and add the scale widget on top
        /// </summary>
        private async Task RefreshMapLayers()
        {
            foreach (var layer in Map.Layers)
            {
                if (layer != null && layer.GetType() != typeof(Mapsui.UI.Objects.MyLocationLayer))
                {
                    //Device.BeginInvokeOnMainThread(() =>
                    //{
                    Map.Layers.Remove(layer);
                    //});
                }
            }

            try
            {
                var newMapLayers = await MapModel.MakeArrayOfLayers();
                MapLayers = new ObservableCollection<MapLayer>(newMapLayers);
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
            Device.BeginInvokeOnMainThread(() =>
            {
                App.Gps.StartGPSAsync();
                UpdateLocation();
                Preferences.Set("GPS", true);
                Preferences.Set("GPS_Centred", true);
                VMGPSButton.Text = "\ue1b3";
            });
        }

        /// <summary>
        /// If permissions allow, show the GPS position in the map, but keep the map centred on the same position
        /// </summary>
        public void ShowPositionNotCentered()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                App.Gps.StartGPSAsync();
                UpdateLocation();
                Preferences.Set("GPS", true);
                Preferences.Set("GPS_Centred", false);
                VMGPSButton.Text = "\ue1b4";
            });
        }

        /// <summary>
        /// Stop showing the GPS position in the map
        /// </summary>
        public void StopShowingPosition()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                RemoveGPSLayers();
                GPS.StopGPSAsync();
                Preferences.Set("GPS", false);
                Preferences.Set("GPS_Centred", false);
                VMGPSButton.Text = "\ue1b5";
            });
        }

        /// <summary>
        /// Update the current GPS location on receiving a notification from the GPS object
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="accuracy"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        private void UpdateLocation()
        {
                Task.Run(async() =>
                {
                    await UpdateLocationWork();
                });
        }

        private async Task UpdateLocationWork()
        {
            if (!IsGeneratingLayer)
            {
                IsGeneratingLayer = true;
                try
                {
                    var lat = Preferences.Get("LastPositionLatitude", 0.0);
                    var lon = Preferences.Get("LastPositionLongitude", 0.0);
                    var accuracy = (int)Preferences.Get("LastPositionAccuracy", 0);
                    var heading = 0;
                    if (Device.RuntimePlatform == Device.iOS)
                    {
                        heading = (int)Preferences.Get("LastPositionHeading", 0);
                    }
                    var prevlat = Preferences.Get("PrevLastPositionLatitude", 0.0);
                    var prevlon = Preferences.Get("PrevLastPositionLongitude", 0.0);
                    var prevaccuracy = Preferences.Get("PrevLastPositionAccuracy", 0);
                    var prevheading = 0;
                    if (Device.RuntimePlatform == Device.iOS)
                    {
                        prevheading = Preferences.Get("PrevLastPositionHeading", 0);
                    }
                    var layerCount = VMMapView.Map.Layers.Where(l => l.Name == "GPS" || l.Name == "Bearing").Count();
                    var centred = Preferences.Get("GPS_Centred", false);

                    if (GPSPointsQueue.Count > 10)
                    {
                        GPSPointsQueue.Dequeue();
                    }

                    (double, double) newCoords = (lat, lon);
                    GPSPointsQueue.Enqueue(newCoords);

                    if (lat != 0 && lon != 0)
                    {

                        if (centred)
                        {
                            CentreOnPoint(lon, lat);
                        }
                        if (((Single)lat != (Single)prevlat || (Single)lon != (Single)prevlon || accuracy != prevaccuracy || layerCount < 2) /*&& IsGeneratingLayer == false*/ && Preferences.Get("GPS", false))
                        {

                            double[] latArray = new double[]
                                {
                            lat,
                            GPSPointsQueue.ElementAt(1).Item1,
                            GPSPointsQueue.ElementAt(2).Item1,
                            GPSPointsQueue.ElementAt(3).Item1,
                            GPSPointsQueue.ElementAt(4).Item1,
                            GPSPointsQueue.ElementAt(5).Item1,
                            GPSPointsQueue.ElementAt(6).Item1,
                            GPSPointsQueue.ElementAt(7).Item1,
                            GPSPointsQueue.ElementAt(8).Item1,
                            GPSPointsQueue.ElementAt(9).Item1,
                            GPSPointsQueue.ElementAt(10).Item1,
                                };
                            var medianLat = latArray.Median();
                            double[] lonArray = new double[]
                            {
                            lon,
                            GPSPointsQueue.ElementAt(1).Item2,
                            GPSPointsQueue.ElementAt(2).Item2,
                            GPSPointsQueue.ElementAt(3).Item2,
                            GPSPointsQueue.ElementAt(4).Item2,
                            GPSPointsQueue.ElementAt(5).Item2,
                            GPSPointsQueue.ElementAt(6).Item2,
                            GPSPointsQueue.ElementAt(7).Item2,
                            GPSPointsQueue.ElementAt(8).Item2,
                            GPSPointsQueue.ElementAt(9).Item2,
                            GPSPointsQueue.ElementAt(10).Item2,
                            };
                            var medianLon = lonArray.Median();

                            if (medianLat != 0 && medianLon != 0)
                            {
                                Preferences.Set("MedianPositionLatitude", medianLat);
                                Preferences.Set("MedianPositionLongitude", medianLon);
                            }
                            else
                            {
                                Preferences.Set("MedianPositionLatitude", lat);
                                Preferences.Set("MedianPositionLongitude", lon);
                            }

                            try
                            {
                                await AddGPSLayer(lat, lon, accuracy, heading, medianLat, medianLon);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                Preferences.Set("LastPositionLatitude", Preferences.Get("PrevLastPositionLatitude", 0.0));
                                Preferences.Set("LastPositionLongitude", Preferences.Get("PrevLastPositionLongitude", 0.0));
                                Preferences.Set("LastPositionAccuracy", Preferences.Get("PrevLastPositionAccuracy", 0));
                                IsGeneratingLayer = false;
                            }
                        }
                        else if (Math.Abs(prevheading - heading) > 0 && (Single)lat == (Single)prevlat && (Single)lon == (Single)prevlon && accuracy == prevaccuracy /*&& IsGeneratingLayer == false*/ && Preferences.Get("GPS", false) && Device.RuntimePlatform == "iOS")
                        {
                            //IsGeneratingLayer = true;
                            try
                            {
                                await AddBearingLayer(Preferences.Get("MedianPositionLatitude", lat), Preferences.Get("MedianPositionLongitude", lon), accuracy, heading);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                Preferences.Set("LastPositionHeading", Preferences.Get("PrevLastPositionHeading", 0));
                                IsGeneratingLayer = false;
                            }
                        }
                        else
                        {
                            IsGeneratingLayer = false;
                        }
                        //IsGeneratingLayer = false;
                    }
                    else
                    {
                        IsGeneratingLayer = false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    IsGeneratingLayer = false;
                }
            }
        }


        /// <summary>
        /// Moves the map to x,y, whilst keeping the extent the same
        /// </summary>
        private void CentreOnPoint(double x, double y)
        {
            try
            {
                var coords = SphericalMercator.FromLonLat(x, y);
                var x1 = Double.Parse(Preferences.Get("BBLLx", "1000"));
                var x2 = Double.Parse(Preferences.Get("BBURx", "1000"));
                var dx = x2 - x1;
                var y1 = Double.Parse(Preferences.Get("BBLLy", "1000"));
                var y2 = Double.Parse(Preferences.Get("BBURy", "1000"));
                var dy = y2 - y1;

                double newx1 = coords.x - (dx / 2);
                double newx2 = coords.x + (dx / 2);
                double newy1 = coords.y - (dy / 2);
                double newy2 = coords.y + (dy / 2);

                MRect bbox = new MRect(newx1, newy1, newx2, newy2);
                Device.BeginInvokeOnMainThread(() =>
                {
                    VMMapView.Map.Navigator.ZoomToBox(bbox, MBoxFit.Fit);
                });

                SetBoundingBox();
            }
            catch
            {

            }

        }

        private async Task AddGPSLayer(double latitude, double longitude, int accuracy, int heading, double medianLat, double medianLon)
        {
            var newLayers = new List<ILayer>();
            if (medianLat != 0)
            {
                GPSLayer = CreateGPSLayer(medianLat, medianLon, accuracy, heading);
            }
            else
            {
                GPSLayer = CreateGPSLayer(latitude, longitude, accuracy, heading);
            }
            newLayers.Add(GPSLayer);
            if (Device.RuntimePlatform == "iOS")
            {
                if (medianLat != 0)
                {
                    BearingLayer = CreateBearingLayer(medianLat, medianLon, accuracy, heading);
                }
                else
                {
                    BearingLayer = CreateBearingLayer(latitude, longitude, accuracy, heading);
                }
                newLayers.Add(BearingLayer);
            }
            if (medianLat != 0)
            {
                GPSPointLayer = CreateGPSPointLayer(medianLat, medianLon, accuracy, heading);
            }
            else
            {
                GPSPointLayer = CreateGPSPointLayer(latitude, longitude, accuracy, heading);
            }
            newLayers.Add(GPSPointLayer);
            
            
            try
            {
                //if (Device.RuntimePlatform == Device.iOS)
                //{
                //    ReplaceGPSLayers(newLayers);
                //}
                //else if (Device.RuntimePlatform == Device.Android)
                //{
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        ReplaceGPSLayers(newLayers);
                    });
                //}


                Preferences.Set("PrevLastPositionLatitude", latitude);
                Preferences.Set("PrevLastPositionLongitude", longitude);
                Preferences.Set("PrevLastPositionAccuracy", accuracy);
                Preferences.Set("PrevLastPositionHeading", heading);
            }
            catch
            {
                Preferences.Set("LastPositionLatitude", Preferences.Get("PrevLastPositionLatitude", 0.0));
                Preferences.Set("LastPositionLongitude", Preferences.Get("PrevLastPositionLongitude", 0.0));
                Preferences.Set("LastPositionAccuracy", Preferences.Get("PrevLastPositionAccuracy", 0));
                Preferences.Set("LastPositionHeading", Preferences.Get("PrevLastPositionHeading", 0));
                IsGeneratingLayer = false;
            }
        }

        private void ReplaceGPSLayers(IEnumerable<ILayer> newLayers)
        {
            var gpsLayers = VMMapView.Map.Layers.Where(l => l.Name == "GPS" || l.Name == "Bearing").ToArray();
            var gpsIsOn = Preferences.Get("GPS", false);
            if (gpsLayers.Count() > 0)
            {
                VMMapView.Map.Layers.Remove(gpsLayers);
            }
            if (gpsIsOn)
            {
                VMMapView.Map.Layers.Add(newLayers.ToArray());
            }
            IsGeneratingLayer = false;
        }

        private async Task AddBearingLayer(double latitude, double longitude, int accuracy, int heading)
        {
            BearingLayer = CreateBearingLayer(latitude, longitude, accuracy, heading);

            try
            {

                if (Device.RuntimePlatform == Device.iOS)
                {
                    var bearingLayer = VMMapView.Map.Layers.Where(l => l.Name == "Bearing").ToArray();
                    var gpsIsOn = Preferences.Get("GPS", false);
                    if (bearingLayer.Count() > 0)
                    {
                        VMMapView.Map.Layers.Remove(bearingLayer);
                    }
                    if (gpsIsOn)
                    {
                        VMMapView.Map.Layers.Add(BearingLayer);
                    }
                    Preferences.Set("PrevLastPositionHeading", heading);
                        IsGeneratingLayer = false;
                }
                else if (Device.RuntimePlatform == Device.Android)
                {
                    Task.Run(async () =>
                    {
                        var bearingLayer = VMMapView.Map.Layers.Where(l => l.Name == "Bearing").ToArray();
                        if (bearingLayer.Count() > 0)
                        {
                            VMMapView.Map.Layers.Remove(bearingLayer);
                        }
                        if (Preferences.Get("GPS", false))
                        {
                            VMMapView.Map.Layers.Add(BearingLayer);
                        }
                        Preferences.Set("PrevLastPositionHeading", heading);
                        IsGeneratingLayer = false;
                    });
                }
            }
            catch
            {
                Preferences.Set("LastPositionHeading", Preferences.Get("PrevLastPositionHeading", 0));
                IsGeneratingLayer = false;
            }
            
        }


        private void RemoveGPSLayers()
        {
            var gpsLayers = VMMapView.Map.Layers.Where(l => l.Name == "GPS" || l.Name == "Bearing" || l.Name == "GPSPoint").ToList();
            Device.BeginInvokeOnMainThread(() =>
            {
                foreach (var layer in gpsLayers)
                {
                    VMMapView.Map.Layers.Remove(layer);
                }
            });
        }


        private ILayer CreateGPSLayer(double latitude, double longitude, int accuracy, int heading)
        {
            var c = new Mapsui.UI.Forms.Circle
            {
                Center = new Mapsui.UI.Forms.Position(latitude, longitude),
                Radius = Mapsui.UI.Forms.Distance.FromMeters(accuracy),
                StrokeWidth = 3,
                Quality = 60
            };

            c.Feature.Styles = new List<IStyle>(){ new VectorStyle
            {
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(80,66,135,245)),
                Outline = null,
                Line =
                    {
                        Color = Mapsui.Styles.Color.Transparent,
                        Width = 5
                    }
            } };
            var points = new List<GeometryFeature>() { c.Feature };
            ILayer gpsLayer = MapModel.CreatePolygonLayer(points, Mapsui.Styles.Color.Transparent, Mapsui.Styles.Color.FromArgb(50, 66, 135, 245));
            gpsLayer.Name = "GPS";
            gpsLayer.IsMapInfoLayer = false;
            return gpsLayer;
        }

        private ILayer CreateGPSPointLayer(double latitude, double longitude, int accuracy, int heading)
        {
            var point = SphericalMercator.FromLonLat(longitude, latitude);
            var netPoint = new NetTopologySuite.Geometries.Point(point.x, point.y);

            var feature = new GeometryFeature
            {
                Geometry = netPoint,
                ["Name"] = "GPS",
                ["Label"] = "GPS"
            };

            var points = new List<GeometryFeature>() { feature };
            var path = "BioDivCollectorXamarin.Images.loc.png";
            var bitmapId = MapModel.GetBitmapIdForEmbeddedResource(path);
            var style = new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.40, SymbolOffset = new Offset(0, 0) };
            ILayer gpsLayer = new Mapsui.Layers.Layer("Points")
            {
                //TODO add correct projection
                //CRS = "EPSG:3857",
                DataSource = new MemoryProvider(points),
                IsMapInfoLayer = false,
                Style = style
            };
            gpsLayer.Name = "GPSPoint";
            gpsLayer.Name = "GPS";
            gpsLayer.IsMapInfoLayer = false;
            return gpsLayer;
        }

        private ILayer CreateBearingLayer(double latitude, double longitude, double accuracy, double heading)
        {
            //Polygon
            var R = 6378.1; //Radius of the Earth
            var brng = (Math.PI / 180) * heading; //Bearing is 90 degrees converted to radians.
            var d = accuracy / 1000;
            var d2 = accuracy / 8000;

            var lat1 = (Math.PI / 180) * latitude; //Current lat point converted to radians
            var lon1 = (Math.PI / 180) * longitude; //Current long point converted to radians

            var lat1a = Math.Asin(Math.Sin(lat1) * Math.Cos(d2 / R) + Math.Cos(lat1) * Math.Sin(d2 / R) * Math.Cos(brng - (30 * Math.PI / 180)));
            var lon1a = lon1 + Math.Atan2(Math.Sin(brng - (30 * Math.PI / 180)) * Math.Sin(d2 / R) * Math.Cos(lat1), Math.Cos(d2 / R) - Math.Sin(lat1) * Math.Sin(lat1a));

            var lat1b = Math.Asin(Math.Sin(lat1) * Math.Cos(d2 / R) + Math.Cos(lat1) * Math.Sin(d2 / R) * Math.Cos(brng - (15 * Math.PI / 180)));
            var lon1b = lon1 + Math.Atan2(Math.Sin(brng - (15 * Math.PI / 180)) * Math.Sin(d2 / R) * Math.Cos(lat1), Math.Cos(d2 / R) - Math.Sin(lat1) * Math.Sin(lat1b));

            var lat1c = Math.Asin(Math.Sin(lat1) * Math.Cos(d2 / R) + Math.Cos(lat1) * Math.Sin(d2 / R) * Math.Cos(brng));
            var lon1c = lon1 + Math.Atan2(Math.Sin(brng) * Math.Sin(d2 / R) * Math.Cos(lat1), Math.Cos(d2 / R) - Math.Sin(lat1) * Math.Sin(lat1c));

            var lat1d = Math.Asin(Math.Sin(lat1) * Math.Cos(d2 / R) + Math.Cos(lat1) * Math.Sin(d2 / R) * Math.Cos(brng + (15 * Math.PI / 180)));
            var lon1d = lon1 + Math.Atan2(Math.Sin(brng + (15 * Math.PI / 180)) * Math.Sin(d2 / R) * Math.Cos(lat1), Math.Cos(d2 / R) - Math.Sin(lat1) * Math.Sin(lat1d));

            var lat1e = Math.Asin(Math.Sin(lat1) * Math.Cos(d2 / R) + Math.Cos(lat1) * Math.Sin(d2 / R) * Math.Cos(brng + (30 * Math.PI / 180)));
            var lon1e = lon1 + Math.Atan2(Math.Sin(brng + (30 * Math.PI / 180)) * Math.Sin(d2 / R) * Math.Cos(lat1), Math.Cos(d2 / R) - Math.Sin(lat1) * Math.Sin(lat1e));

            var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng - (30 * Math.PI / 180)));
            var lon2 = lon1 + Math.Atan2(Math.Sin(brng - (30 * Math.PI / 180)) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat2));

            var lat3 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng - (20 * Math.PI / 180)));
            var lon3 = lon1 + Math.Atan2(Math.Sin(brng - (20 * Math.PI / 180)) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat3));

            var lat4 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng - (10 * Math.PI / 180)));
            var lon4 = lon1 + Math.Atan2(Math.Sin(brng - (10 * Math.PI / 180)) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat4));

            var lat5 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng));
            var lon5 = lon1 + Math.Atan2(Math.Sin(brng) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat5));

            var lat6 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng + (10 * Math.PI / 180)));
            var lon6 = lon1 + Math.Atan2(Math.Sin(brng + (10 * Math.PI / 180)) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat6));

            var lat7 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng + (20 * Math.PI / 180)));
            var lon7 = lon1 + Math.Atan2(Math.Sin(brng + (20 * Math.PI / 180)) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat7));

            var lat8 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) + Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng + (30 * Math.PI / 180)));
            var lon8 = lon1 + Math.Atan2(Math.Sin(brng + (30 * Math.PI / 180)) * Math.Sin(d / R) * Math.Cos(lat1), Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat8));

            lat1a = lat1a * 180 / Math.PI;
            lon1a = lon1a * 180 / Math.PI;
            lat1b = lat1b * 180 / Math.PI;
            lon1b = lon1b * 180 / Math.PI;
            lat1c = lat1c * 180 / Math.PI;
            lon1c = lon1c * 180 / Math.PI;
            lat1d = lat1d * 180 / Math.PI;
            lon1d = lon1d * 180 / Math.PI;
            lat1e = lat1e * 180 / Math.PI;
            lon1e = lon1e * 180 / Math.PI;
            lat2 = lat2 * 180 / Math.PI;
            lon2 = lon2 * 180 / Math.PI;
            lat3 = lat3 * 180 / Math.PI;
            lon3 = lon3 * 180 / Math.PI;
            lat4 = lat4 * 180 / Math.PI;
            lon4 = lon4 * 180 / Math.PI;
            lat5 = lat5 * 180 / Math.PI;
            lon5 = lon5 * 180 / Math.PI;
            lat6 = lat6 * 180 / Math.PI;
            lon6 = lon6 * 180 / Math.PI;
            lat7 = lat7 * 180 / Math.PI;
            lon7 = lon7 * 180 / Math.PI;
            lat8 = lat8 * 180 / Math.PI;
            lon8 = lon8 * 180 / Math.PI;

            GeometryFactory geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory();

            var pt1e = SphericalMercator.FromLonLat(lon1e, lat1e);
            var pt1d = SphericalMercator.FromLonLat(lon1d, lat1d);
            var pt1c = SphericalMercator.FromLonLat(lon1c, lat1c);
            var pt1b = SphericalMercator.FromLonLat(lon1b, lat1b);
            var pt1a = SphericalMercator.FromLonLat(lon1a, lat1a);
            var pt2 = SphericalMercator.FromLonLat(lon2, lat2);
            var pt3 = SphericalMercator.FromLonLat(lon3, lat3);
            var pt4 = SphericalMercator.FromLonLat(lon4, lat4);
            var pt5 = SphericalMercator.FromLonLat(lon5, lat5);
            var pt6 = SphericalMercator.FromLonLat(lon6, lat6);
            var pt7 = SphericalMercator.FromLonLat(lon7, lat7);
            var pt8 = SphericalMercator.FromLonLat(lon8, lat8);

            Coordinate[] tpoints = new Coordinate[]
            {
                new NetTopologySuite.Geometries.Coordinate(pt1e.x, pt1e.y),
                new NetTopologySuite.Geometries.Coordinate(pt1d.x, pt1d.y),
                new NetTopologySuite.Geometries.Coordinate(pt1c.x, pt1c.y),
                new NetTopologySuite.Geometries.Coordinate(pt1b.x, pt1b.y),
                new NetTopologySuite.Geometries.Coordinate(pt1a.x, pt1a.y),
                new NetTopologySuite.Geometries.Coordinate(pt2.x, pt2.y),
                new NetTopologySuite.Geometries.Coordinate(pt3.x, pt3.y),
                new NetTopologySuite.Geometries.Coordinate(pt4.x, pt4.y),
                new NetTopologySuite.Geometries.Coordinate(pt5.x, pt5.y),
                new NetTopologySuite.Geometries.Coordinate(pt6.x, pt6.y),
                new NetTopologySuite.Geometries.Coordinate(pt7.x, pt7.y),
                new NetTopologySuite.Geometries.Coordinate(pt8.x, pt8.y),
                //To create a LinearRing first and last coordinate must be equal
                new NetTopologySuite.Geometries.Coordinate(pt1e.x, pt1e.y)
            };

            var linearRing = new LinearRing(tpoints);
            var polygon = new Polygon(linearRing);

            var bearingfeature = new GeometryFeature
            {
                Geometry = polygon,
                ["Name"] = "Bearing",
                ["Label"] = "Bearing"
            };

            bearingfeature.Styles = new List<IStyle>()
            {
                new VectorStyle
                {
                    Fill = new Mapsui.Styles.Brush {FillStyle = FillStyle.Solid, Color = Mapsui.Styles.Color.FromArgb(100,66,135,245), },
                    Outline = null,
                    Line = null
                }
            };


            var points = new List<GeometryFeature>() { bearingfeature };
            ILayer gpsLayer = MapModel.CreatePolygonLayer(points, Mapsui.Styles.Color.Transparent, Mapsui.Styles.Color.FromArgb(100, 66, 135, 245));
            gpsLayer.Name = "Bearing";
            gpsLayer.IsMapInfoLayer = false;
            return gpsLayer;
        }


        /// <summary>
        /// Keep track of the bounding box when the map is scrolled, and set the GPS to 'not centred'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapView_TouchMove(object sender, Mapsui.UI.TouchedEventArgs e)
        {
            SetBoundingBox();
            Preferences.Set("GPS_Centred", false);
            var gps = Preferences.Get("GPS", false);
            if (gps)
            {
                VMGPSButton.Text = "\ue1b4";
            }
        }


        /// <summary>
        /// Save the current bounding box to the preferences so that it may later be reinstated
        /// </summary>
        private void SetBoundingBox()
        {
            MRect bb = VMMapView.Map.Navigator.Viewport.ToExtent();
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
            Navigation.PushAsync(new MapLayersPage(), true);
        }

        /// <summary>
        /// On touching a MapInfo object, parse out the object Id and go to that object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void MapOnInfo(object sender, MapInfoEventArgs args)
        {
            if (DateTime.Now > LastObjectSelection.AddSeconds(1) && CanAddMapGeometry == false) //ensure that only 1 selection is made at one time
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
            var isLocationAvailable = IsLocationAvailable();

            if (isLocationAvailable)
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
            else
            {
                App.Current.MainPage.DisplayAlert("GPS nicht aktiviert", "Auf diesem Gerät wurde kein aktiviertes GPS gefunden. Stellen Sie sicher, dass das Gerät über ein GPS verfügt und dieses eingeschalten ist.", "OK");
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
            return App.Gps.HasLocationPermission;
        }

        /// <summary>
        /// Check if the GPS is activated on the device. This is important for Android, where you can turn GPS off
        /// </summary>
        /// <returns></returns>
        public bool IsLocationAvailable()
        {
            if (!CrossGeolocator.IsSupported)
                return false;

            return CrossGeolocator.Current.IsGeolocationEnabled;
        }

        /// <summary>
        /// On canelling geometry creation, remove the temp layer
        /// </summary>
        private void CancelNewGeom()
        {
            GeomToEdit = 0;
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
        /// Validate if the undo function is available
        /// </summary>
        /// <returns>true</returns>
        private bool CanUndoNewGeom()
        {
            return TempCoordinates.Count > 1;
        }

        /// <summary>
        /// Validate if the clear function is available
        /// </summary>
        /// <returns>true</returns>
        private bool CanClearNewGeom()
        {
            return TempCoordinates.Count > 0;
        }

        /// <summary>
        /// Cancel saving map tiles
        /// </summary>
        private void CancelSave()
        {
            GeomToEdit = 0;
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
        private async void SaveNewGeom()
        {

            if (GeomToEdit > 0)
            {
                await ReferenceGeometry.UpdateGeometry(TempCoordinates, GeomToEdit);
                GeomToEdit = 0;
                AllowAddNewGeom();
                RemoveTempGeometry();
                await NewShapes();
            }
            else
            {
                Mapsui.MPoint point = TempCoordinates[0];
                Coordinate coords = point.ToCoordinate();
                string coordString = coords[1].ToString("#.000#") + ", " + coords[0].ToString("#.000#");

                string geomName = await Shell.Current.CurrentPage.DisplayPromptAsync("Geometriename", "Bitte geben Sie einen Geometrienamen ein", accept: "Speichern", cancel: "Abbrechen");

                while (geomName == "")
                {
                    geomName = await Shell.Current.CurrentPage.DisplayPromptAsync("Geometriename", "Leere Geometrienamen zurzeit nicht möglich. Bitte geben Sie einen Geometrienamen ein", accept: "Speichern", cancel: "Abbrechen");
                }

                List<string> geomNames = await ReferenceGeometry.GetAllGeometryNames();
                bool geomNameExists = false;

                foreach (string name in geomNames)
                {
                    if (name == geomName)
                    {
                        geomNameExists = true;
                    }
                }

                while (geomNameExists == true)
                {
                    geomName = await Shell.Current.CurrentPage.DisplayPromptAsync("Geometriename", "Der eingegebene Geometriename existiert bereits. Bitte geben Sie einen anderen Geometrienamen ein", accept: "Speichern", cancel: "Abbrechen");
                    int i = 0;
                    foreach (var name in geomNames)
                    {
                        if (name == geomName)
                        {
                            i++;
                        }
                    }
                    if (i == 0)
                    {
                        geomNameExists = false;
                    }
                    else
                    {
                        geomNameExists = true;
                    }
                }

                string geomId = await ReferenceGeometry.SaveGeometry(TempCoordinates, geomName);

                ReferenceGeometry geom = await ReferenceGeometry.GetGeometry(geomId);

                if (geom != null)
                {
                    //MessagingCenter.Send<MapPageVM, string>(this, "GenerateNewForm", geomId);

                    //await RecordsPage.AddFormToNewGeometry(geom.Id.ToString());

                    //Wait to ensure that the records page has been created before sending the GenerateNewForm message
                    MessagingCenter.Subscribe<Application>(App.Current,"RecordsPageReady", async (sender) =>
                    {
                        MessagingCenter.Send<MapPageVM, string>(this, "GenerateNewForm", geomId);
                        MessagingCenter.Unsubscribe<Application>(App.Current, "RecordsPageReady");
                    });
                    await Shell.Current.GoToAsync($"//Records?objectId={geom.Id}", true);
                }

                GeomToEdit = 0;
                RemoveTempGeometry();
                //RefreshShapes();
            }
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
            else
            {
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
                        if (!CurrentPolygonSelfIntersecting) //Polygon was not previously self-intersecting
                        {
                            CurrentPolygonSelfIntersecting = true;
                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                bool ok = await App.Current.MainPage.DisplayAlert("Das aktuelle Polygon schneidet sich selbst", "Solange es eine ungültige Geometrie hat kann es nicht gespeichert werden", "Weiter zeichnen", "Rückgängig machen");
                                if (!ok)
                                {
                                    try
                                    {
                                        UndoLastTempPoint();
                                        CurrentPolygonSelfIntersecting = false;
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
        /// Remove the last-drawn point in a new geometry
        /// </summary>
        private void UndoLastTempPoint()
        {
            try
            {
                if (TempCoordinates.Count > 1)
                {
                    if (TempCoordinates[0].X == TempCoordinates[TempCoordinates.Count - 1].X && TempCoordinates[0].Y == TempCoordinates[TempCoordinates.Count - 1].Y)
                    {
                        //Keep start and end coordinates of polygon
                        TempCoordinates.RemoveAt(TempCoordinates.Count - 2);
                    }
                    else
                    {
                        TempCoordinates.RemoveAt(TempCoordinates.Count - 1);
                    }
                    if (TempCoordinates.Count == 2 && TempCoordinates[0].X == TempCoordinates[TempCoordinates.Count - 1].X && TempCoordinates[0].Y == TempCoordinates[TempCoordinates.Count - 1].Y)
                    {
                        //Delete the joining point of a polygon if we only have 2 points the same
                        TempCoordinates.RemoveAt(TempCoordinates.Count - 1);
                    }

                    MessagingCenter.Send<MapPageVM>(this, "ShapeDrawingUndone");
                    (ClearGeomCommand as Command).ChangeCanExecute();
                    (UndoGeomCommand as Command).ChangeCanExecute();
                }
            }
            catch
            {

            }

        }

        /// <summary>
        /// Clear a new geometry
        /// </summary>
        private void ClearNewGeom()
        {
            if (TempCoordinates.Count > 1)
            {
                TempCoordinates = new List<Mapsui.MPoint>();
                //Map.Layers.Remove(TempLayer);
                var mapLayer = Map.Layers.Where(x => x == TempLayer).FirstOrDefault();
                if (mapLayer != null)
                    Map.Layers.Remove(mapLayer);
                (ClearGeomCommand as Command).ChangeCanExecute();
                (UndoGeomCommand as Command).ChangeCanExecute();
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

            try
            {
                if (canAddMapGeometry)
                {
                    VMGeomEditButton.BackgroundColor = (Xamarin.Forms.Color)Application.Current.Resources["BioDivGreen"];
                }
                else
                {
                    VMGeomEditButton.BackgroundColor = (Xamarin.Forms.Color)Application.Current.Resources["BioDivGrey"];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
            Device.InvokeOnMainThreadAsync(() =>
            {
                TempCoordinates = new List<Mapsui.MPoint>();
                var mapLayer = Map.Layers.Where(x => x == TempLayer).FirstOrDefault();
                if (mapLayer != null)
                    Map.Layers.Remove(mapLayer);
                CanAddMapGeometry = false;
                GeometryType = String.Empty;
                VMGeomEditButton.BackgroundColor = (Xamarin.Forms.Color)Application.Current.Resources["BioDivGrey"];
            });
        }

        /// <summary>
        /// Save the map tiles to mbtiles files
        /// </summary>
        public void SaveMaps()
        {
            Task.Run(async () =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    SaveCountText = "Berechnet welche Kacheln zu speichern sind";
                });

                MRect bb = VMMapView.Map.Navigator.Viewport.ToExtent();
                Extent extent = new Extent(bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
                await MapModel.saveMaps(extent);
            });
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
