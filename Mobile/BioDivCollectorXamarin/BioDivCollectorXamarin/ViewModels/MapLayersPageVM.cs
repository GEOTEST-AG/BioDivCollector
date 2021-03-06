using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BioDivCollectorXamarin.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class MapLayersPageVM:BaseViewModel
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
        /// The locally saved mbtiles file size for the current layer
        /// </summary>
        public string BaseLayerSize { get; set; }

        /// <summary>
        /// The current base layer
        /// </summary>
        private string baseLayerName;
        public string BaseLayerName { get
            {
                return baseLayerName;
            }
            set
            {
                baseLayerName = value;
                if (MapLayers == null) { MapLayers = MapModel.MakeArrayOfLayers(); }
                BaseLayerSize = MapModel.GetLocalStorageSizeForLayer(Preferences.Get("BaseLayer", "swisstopo_pixelkarte"));
                OnPropertyChanged("BaseLayerName");
                OnPropertyChanged("BaseLayerSize");
            }
        }


        /// <summary>
        /// Initialise the page with the project specific map layers
        /// </summary>
        /// <param name="mapLayers"></param>
        public MapLayersPageVM(ObservableCollection<BioDivCollectorXamarin.Models.MapLayer> mapLayers)
        {
            //MoveLayerUpCommand1 = new MoveLayerUpCommand(this);
            MoveLayerUpCommand = new Command(OnMoveLayerUp, ValidateMove);
            DeleteLayerCommand = new Command(OnDelete, ValidateDelete);
            OSMButtonCommand = new Command(OSMButtonPressed, CanPressOSMButton);
            PixelkarteButtonCommand = new Command(PixelkarteButtonPressed, CanPressPixelkarteButton);
            OrthofotoButtonCommand = new Command(OrthofotoButtonPressed, CanPressOrthofotoButton);
            MapLayers = MapModel.MakeArrayOfLayers();
            BaseLayerName = "Base";

            MessagingCenter.Subscribe<MapLayer>(this, "RefreshLayerList", (sender) =>
            {
                MapLayers = new ObservableCollection<BioDivCollectorXamarin.Models.MapLayer>();
                foreach (var layer in mapLayers)
                {
                    if (layer != null)
                    { MapLayers.Add(layer); }
                }

            });

            MessagingCenter.Subscribe<MapLayersPageVM>(this, "LayerOrderChanged", (sender) =>
            {

                Device.BeginInvokeOnMainThread(() =>
                {
                    var model = new MapModel();
                    MapLayers = MapModel.MakeArrayOfLayers();
                    OnPropertyChanged("MapLayers");
                });
            });
 
            MessagingCenter.Subscribe<Application,string>(App.Current, "DownloadComplete", (sender,json) =>
            {
                MapLayers = new ObservableCollection<BioDivCollectorXamarin.Models.MapLayer>();
                foreach (var layer in mapLayers)
                {
                    if (layer != null)
                    { MapLayers.Add(layer); }
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    //var model = new MapModel();
                    //MapLayers = MapModel.MakeArrayOfLayers();
                    OnPropertyChanged("MapLayers");
                });
            });

        }

        /// <summary>
        /// Delete the locally stored mbtiles file for the layer
        /// </summary>
        /// <param name="parameter"></param>
        private async void OnDelete(object parameter)
        {
            await Task.Run(() =>
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
                MapLayers = MapModel.MakeArrayOfLayers();
                ChangeBaseLayerLabel(); //Trigger the mbtiles file size to be recalculated
            });
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
        /// Move a layer up the layer stack
        /// </summary>
        /// <param name="parameter"></param>
        private async void OnMoveLayerUp(object parameter)
        {
            await Task.Run(() =>
            {
                var layer = (parameter as MapLayer);
                if (layer != null)
                {
                    layer.LayerZ = layer.LayerZ + 1;
                    MessagingCenter.Send<MapLayersPageVM>(this, "LayerOrderChanged");
                }
            });
        }

        /// <summary>
        /// Check if the layers can be moved up the layer stack
        /// </summary>
        /// <param name="parameter"></param>
        private bool ValidateMove(object parameter)
        {
            var layer = (parameter as MapLayer);
            if (layer != null && layer.LayerZ == mapLayers.Count) { return false; }
            return true;
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
    }
}
