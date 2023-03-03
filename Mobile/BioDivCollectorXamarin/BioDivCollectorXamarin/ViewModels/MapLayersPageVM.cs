using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class MapLayersPageVM : BaseViewModel
    {
        /// <summary>
        /// An array containing all of the map layers
        /// </summary>
        private ObservableCollection<BioDivCollectorXamarin.Models.MapLayer> mapLayers;
        public ObservableCollection<BioDivCollectorXamarin.Models.MapLayer> MapLayers
        {
            get { return mapLayers; }
            set
            {
                mapLayers = value;
                //MessagingCenter.Send<Application>(App.Current, "UpdateMapLayers");
                OnPropertyChanged("MapLayers");
            }
        }

        /// <summary>
        /// Moves the layer up in the layer stack
        /// </summary>
        public Command MoveLayerUpCommand { get; set; }

        /// <summary>
        /// Deletes the local copy of the layer
        /// </summary>
        public Command DeleteLayerCommand { get; set; }

        /// <summary>
        /// Choose an open street map base layer
        /// </summary>
        public Command OSMButtonCommand { get; set; }

        /// <summary>
        /// Choose a Swisstopo pixelmap base layer
        /// </summary>
        public Command PixelkarteButtonCommand { get; set; }

        /// <summary>
        /// Choose a Swissimage base layer
        /// </summary>
        public Command OrthofotoButtonCommand { get; set; }

        /// <summary>
        /// Shows info on how to create a local layer
        /// </summary>
        public Command LayersInfoCommand { get; set; }

        /// <summary>
        /// The locally saved mbtiles file size for the current layer
        /// </summary>
        public string BaseLayerSize { get; set; }

        /// <summary>
        /// Determines whether the list should show the localonly files
        /// </summary>
        private bool showLocalOnly;
        public bool ShowLocalOnly
        {
            get
            {
                return showLocalOnly;
            }
            set
            {
                Task.Run(async () =>
                {
                    showLocalOnly = value;
                    Preferences.Set("ShowLocalOnly", showLocalOnly);
                    if (showLocalOnly)
                    {
                        await AddFileLayers();
                    }
                    else
                    {
                        await RemoveFileLayers();
                    }
                    UpdateMapLayers();
                });
            }
        }

        /// <summary>
        /// The current base layer
        /// </summary>
        private string baseLayerName;
        public string BaseLayerName
        {
            get
            {
                return baseLayerName;
            }
            set
            {
                Task.Run(async () =>
                {
                    baseLayerName = value;
                    if (MapLayers == null)
                    {
                        var newMapLayers = await MapModel.MakeArrayOfLayers();
                        MapLayers = new ObservableCollection<MapLayer>(newMapLayers);
                    }
                    BaseLayerSize = MapModel.GetLocalStorageSizeForLayer(Preferences.Get("BaseLayer", "swisstopo_pixelkarte"));
                    OnPropertyChanged("BaseLayerName");
                    OnPropertyChanged("BaseLayerSize");
                });
            }
        }


        /// <summary>
        /// Initialise the page with the project specific map layers
        /// </summary>
        /// <param name="mapLayers"></param>
        public MapLayersPageVM()
        {
            DeleteLayerCommand = new Command(OnDelete, ValidateDelete);
            OSMButtonCommand = new Command(OSMButtonPressed, CanPressOSMButton);
            PixelkarteButtonCommand = new Command(PixelkarteButtonPressed, CanPressPixelkarteButton);
            OrthofotoButtonCommand = new Command(OrthofotoButtonPressed, CanPressOrthofotoButton);
            LayersInfoCommand = new Command(() =>
            {
                Shell.Current.Navigation.PushAsync(new LayersInfoPage());
            });
            BaseLayerName = "Base";
            ShowLocalOnly = Preferences.Get("ShowLocalOnly", false);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var allMapLayers = await MapModel.MakeArrayOfLayers();
                MapLayers = new ObservableCollection<MapLayer>(new List<MapLayer>());
                MapLayers = new ObservableCollection<MapLayer>(allMapLayers);
            });

            MessagingCenter.Subscribe<Application>(App.Current, "LayerOrderChanged", (sender) =>
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    var newMapLayers = await MapModel.MakeArrayOfLayers();
                    MapLayers = new ObservableCollection<MapLayer>(newMapLayers);
                    UpdateMapLayers();
                    OnPropertyChanged("MapLayers");
                    MessagingCenter.Send<Application>(App.Current, "ListSourceChanged");
                });
            });




            MessagingCenter.Subscribe<Application,string>(App.Current, "DownloadComplete", (sender,json) =>
            {
                mapLayers = new ObservableCollection<BioDivCollectorXamarin.Models.MapLayer>();
                foreach (var layer in MapLayers)
                {
                    if (layer != null)
                    { mapLayers.Add(layer); }
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged("MapLayers");
                });
            });

        }

        /// <summary>
        /// Updates the available map layers seen on the page
        /// </summary>
        public void UpdateMapLayers()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                var newMapLayers = await MapModel.MakeArrayOfLayers();
                MapLayers = new ObservableCollection<MapLayer>(newMapLayers);
                OnPropertyChanged("MapLayers");
                MessagingCenter.Send<MapLayersPageVM>(this, "ListSourceChanged"); //No idea why this works the way it does. If you take the above two lines out, it doesn't work, and if you just add OnPropertyChanged, it doesn't work
            });
        }

        /// <summary>
        /// Delete the locally stored mbtiles file for the layer
        /// </summary>
        /// <param name="parameter"></param>
        private void OnDelete(object parameter)
        {
            if (parameter == this)
            {
                var baseLayer = Preferences.Get("BaseLayer", String.Empty);
                if (baseLayer != String.Empty)
                {
                    MapModel.DeleteMapLayer(baseLayer);
                }
            }
            else
            {
                var layer = (parameter as MapLayer);
                MapModel.DeleteMapLayer(layer.Name);
            }
            Task.Run(async () =>
            {
                var newMapLayers = await MapModel.MakeArrayOfLayers();
                MapLayers = new ObservableCollection<MapLayer>(newMapLayers);
            });
            ChangeBaseLayerLabel(); //Trigger the mbtiles file size to be recalculated
        }

        /// <summary>
        /// Validate deletion (always allowed)
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>true</returns>
        private bool ValidateDelete(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Set the label shown against the file size based on the saved preferences key
        /// </summary>
        public void ChangeBaseLayerLabel()
        {
            var BL = Preferences.Get("BaseLayer", "");
            switch (BL)
            {
                case "osm":
                    BaseLayerName = "Open Street Maps";
                    break;
                case "swissimage":
                    BaseLayerName = "Orthofoto Schweiz";
                    break;
                default:
                    BaseLayerName = "Landeskarte Schweiz";
                    break;
            }

        }

        /// <summary>
        /// Sort the layers through drag and drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal async void LayerList_ItemDragging(System.Object sender, Syncfusion.ListView.XForms.ItemDraggingEventArgs e)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var layer = (e.ItemData as MapLayer);
                if (layer != null)
                {
                    MapLayers.Move(e.OldIndex, e.NewIndex);
                    int i = 0;

                    foreach (MapLayer templayer in MapLayers)
                    {
                        var j = 0;
                        var successCheck = false;
                        while (successCheck != true)
                        {
                            templayer.LayerZ = i + 1;
                            Layer dblayer;
                            try
                            {
                                dblayer = await conn.Table<Layer>().Where(l => l.Id == templayer.LayerId).FirstOrDefaultAsync();
                                dblayer.order = templayer.LayerZ;
                                await conn.UpdateAsync(dblayer);
                                successCheck = true;
                                i++;
                            }
                            catch
                            {
                                j++;
                                if (j == 1000)
                                {
                                    Device.BeginInvokeOnMainThread(async () =>
                                    {
                                        await App.Current.MainPage.DisplayAlert("", "Der Layer konnte nicht verschoben werden. Bitte erneut versuchen", "OK");
                                    });
                                    successCheck = true;
                                }
                            }
                        }
                    }
                    MessagingCenter.Send<Application>(App.Current, "LayerOrderChanged");
                }
            }
            catch
            {

            }
        }
        
        /// <summary>
        /// Set the base layer to OSM if the OSM button is pressed
        /// </summary>
        /// <param name="parameter"></param>
        private void OSMButtonPressed(object parameter)
        {
            BaseLayerName = "Open Street Map";
        }

        /// <summary>
        /// Validate if the button can be pressed (always true)
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>true</returns>
        private bool CanPressOSMButton(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Set the base layer to Landeskarte if the pixelkarte button is pressed
        /// </summary>
        /// <param name="parameter"></param>
        private void PixelkarteButtonPressed(object parameter)
        {
            BaseLayerName = "Landeskarte Schweiz";
        }

        /// <summary>
        /// Validate if the button can be pressed (always true)
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>true</returns>
        private bool CanPressPixelkarteButton(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Set the base layer to Orthofoto if the Orthofoto button is pressed
        /// </summary>
        /// <param name="parameter"></param>
        private void OrthofotoButtonPressed(object parameter)
        {
            BaseLayerName = "Orthofoto Schweiz";
        }

        /// <summary>
        /// Validate if the button can be pressed (always true)
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>true</returns>
        private bool CanPressOrthofotoButton(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Add layers to the database to correspond with the local mbtiles files
        /// </summary>
        internal async Task AddFileLayers()
        {
            await MapModel.AddOfflineLayersToProject();
        }

        /// <summary>
        /// Delete layers from the database corresponding to mbtiles files
        /// </summary>
        internal async Task RemoveFileLayers()
        {
            await MapModel.RemoveOfflineLayersFromProject();
        }
    }
}
