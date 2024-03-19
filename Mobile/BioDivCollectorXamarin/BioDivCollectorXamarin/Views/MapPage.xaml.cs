using System;
using BioDivCollectorXamarin.ViewModels;
using Mapsui.UI.Forms;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapPage : ContentPage
    {
        MapPageVM ViewModel;
        
        /// <summary>
        /// Initialise the map and listen for geometry selection
        /// </summary>
        public MapPage()
        {
            InitializeComponent();
            ViewModel = new MapPageVM(MapsuiMapView,GPSButton, AddMapGeometryButton, Navigation);
            BindingContext = ViewModel;

            MessagingCenter.Subscribe<MapPageVM>(this, "SelectGeometryType", (sender) =>
            {
                 Device.BeginInvokeOnMainThread(() =>
                    {
                        ShowGeometryChoice();
                    });
            });

            MessagingCenter.Subscribe<MapPageVM,string>(this, "RequestGeometryName", (sender,arg) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    RequestGeometryName(arg);
                });
            });
        }

        /// <summary>
        /// Set the route (for state restoration) and deal with iOS safe areas
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.CurrentRoute = "//Map";
            //Recalculate safe insets in case they somehow changed
            Device.BeginInvokeOnMainThread(() =>
            {
                var safeInsets = On<iOS>().SafeAreaInsets();
                safeInsets.Top = 0;
                Padding = safeInsets;
                ViewModel.OnAppearing();
            });
            if (Preferences.Get("GPS", false))
            {
                App.Gps.StartGPSAsync();
            }
        }

        /// <summary>
        /// Inform the view model that the map is disappearing
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }

        /// <summary>
        /// Ask the user what type of geometry they would like to create
        /// </summary>
        private async void ShowGeometryChoice()
        {
            string action = await DisplayActionSheet("Geometrietyp selektieren", null , null , "Punkt", "Linie", "Polygon");
            ViewModel.GeometryType = action;
        }

        /// <summary>
        /// Ask the user what they would like to call the geometry
        /// </summary>
        private async void RequestGeometryName(string defaultName)
        {
            string result = await DisplayPromptAsync("Geometriename", "Bitte geben Sie eine Geometriename ein", accept:"Speichern", cancel:"Abbrechen");
            MessagingCenter.Send<MapPage, string>(this, "GeometryName", result);
        }

        /// <summary>
        /// Trigger map saving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            ViewModel.SaveMaps();
        }

        /// <summary>
        /// Add a point to the temporary geometry by long-clicking the map (only active when geometry creation is active)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapsuiMapView_MapLongClicked(object sender, MapLongClickedEventArgs e)
        {
            ViewModel.CanAddMapGeometry = true;
            var mp = sender as Mapsui.Map;
            ViewModel.AddTempPoint(e.Point);
        }

        /// <summary>
        /// Add a point to the temporary geometry by clicking the map (only active when geometry creation is active)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapsuiMapView_MapClicked(object sender, MapClickedEventArgs e)
        {
            var mp = sender as Mapsui.Map;
            ViewModel.AddTempPoint(e.Point);
        }
    }
}