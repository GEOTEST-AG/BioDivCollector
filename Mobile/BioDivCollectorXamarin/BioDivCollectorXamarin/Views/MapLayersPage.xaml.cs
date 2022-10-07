using System.Collections.ObjectModel;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapLayersPage : ContentPage
    {

        MapLayersPageVM ViewModel;

        /// <summary>
        /// Initialise the layer list for a specific set of layers
        /// </summary>
        /// <param name="layers"></param>
        public MapLayersPage(ObservableCollection<MapLayer> layers)
        {
            InitializeComponent();
            ViewModel = new MapLayersPageVM(layers);
            BindingContext = ViewModel;
            LayerList.ItemsSource = ViewModel.MapLayers;
            LayerList.HeightRequest = DeviceDisplay.MainDisplayInfo.Height;
        }

        /// <summary>
        /// On appearing, set the route (for state restoration) and set up the basemap buttons according to the last selected base map
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.CurrentRoute = "//Map/MapLayers";
            var baseLayer = Preferences.Get("BaseLayer", "swisstopo_pixelkarte");
            if (baseLayer == "swisstopo_pixelkarte")
            {
                ViewModel.BaseLayerName = "Landeskarte Schweiz";
                OSMButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
                SwisstopoButton.Style = (Style)Application.Current.Resources["PressedButtonStyle"];
                SwissimageButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
            }
            else if (baseLayer == "swissimage")
            {
                ViewModel.BaseLayerName = "Orthofoto Schweiz";
                OSMButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
                SwisstopoButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
                SwissimageButton.Style = (Style)Application.Current.Resources["PressedButtonStyle"];
            }
            else
            {
                ViewModel.BaseLayerName = "Open Street Map";
                OSMButton.Style = (Style)Application.Current.Resources["PressedButtonStyle"];
                SwisstopoButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
                SwissimageButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
            }
            
        }


        /// <summary>
        /// Set the basemap and change the UI elements when a base map button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OSMButton_Clicked(System.Object sender, System.EventArgs e)
        {
            Preferences.Set("BaseLayer", "osm");
            ViewModel.ChangeBaseLayerLabel();
            OSMButton.Style= (Style)Application.Current.Resources["PressedButtonStyle"];
            SwisstopoButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
            SwissimageButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
        }

        /// <summary>
        /// Set the basemap and change the UI elements when a base map button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SwisstopoButton_Clicked(System.Object sender, System.EventArgs e)
        {
            Preferences.Set("BaseLayer", "swisstopo_pixelkarte");
            ViewModel.ChangeBaseLayerLabel();
            OSMButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
            SwisstopoButton.Style = (Style)Application.Current.Resources["PressedButtonStyle"];
            SwissimageButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
        }

        /// <summary>
        /// Set the basemap and change the UI elements when a base map button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SwissimageButton_Clicked(System.Object sender, System.EventArgs e)
        {
            Preferences.Set("BaseLayer", "swissimage");
            ViewModel.ChangeBaseLayerLabel();
            OSMButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
            SwisstopoButton.Style = (Style)Application.Current.Resources["ReleasedButtonStyle"];
            SwissimageButton.Style = (Style)Application.Current.Resources["PressedButtonStyle"];
        }

        void CheckBox_CheckedChanged(System.Object sender, Xamarin.Forms.CheckedChangedEventArgs e)
        {
            CheckBox checky = sender as CheckBox;
            Preferences.Set("ShowLocalOnly", checky.IsChecked);
            if (checky.IsChecked)
            {
                ViewModel.AddFileLayers();
            }
            else
            {
                ViewModel.RemoveFileLayers();
            }
            ViewModel.UpdateMapLayers();
        }
    }
}