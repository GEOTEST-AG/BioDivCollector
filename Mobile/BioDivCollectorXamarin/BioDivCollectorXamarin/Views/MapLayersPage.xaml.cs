﻿using BioDivCollectorXamarin.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
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
        public MapLayersPage()
        {
            InitializeComponent();
            ViewModel = new MapLayersPageVM();
            BindingContext = ViewModel;
            LayerList.ItemsSource = ViewModel.MapLayers;
            LayerList.HeightRequest = DeviceDisplay.MainDisplayInfo.Height;

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(App.Current, "ListSourceChanged", (sender) =>
            {
                //Remake map layer list
                LayerList.ItemsSource = null;
                LayerList.ItemsSource = ViewModel.MapLayers;
                LayerList.ScrollTo(0, false);
            });

            MessagingCenter.Unsubscribe<Xamarin.Forms.Application>(App.Current, "UpdateMapLayers");
            MessagingCenter.Subscribe<Xamarin.Forms.Application>(App.Current, "UpdateMapLayers", (sender) =>
            {
                UpdateLayerList();
            });
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            var safeInsets = On<iOS>().SafeAreaInsets();
            Padding = new Thickness(0, safeInsets.Top, 0, 0);
            OSMButton.HeightRequest = 53 + safeInsets.Bottom;
            SwisstopoButton.HeightRequest = 53 + safeInsets.Bottom;
            SwissimageButton.HeightRequest = 53 + safeInsets.Bottom;
            ButtonLayout.HeightRequest = 57 + safeInsets.Bottom;
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
                OSMButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
                SwisstopoButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["PressedButtonStyle"];
                SwissimageButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
            }
            else if (baseLayer == "swissimage")
            {
                ViewModel.BaseLayerName = "Orthofoto Schweiz";
                OSMButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
                SwisstopoButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
                SwissimageButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["PressedButtonStyle"];
            }
            else
            {
                ViewModel.BaseLayerName = "Open Street Map";
                OSMButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["PressedButtonStyle"];
                SwisstopoButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
                SwissimageButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
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
            OSMButton.Style= (Style)Xamarin.Forms.Application.Current.Resources["PressedButtonStyle"];
            SwisstopoButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
            SwissimageButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
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
            OSMButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
            SwisstopoButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["PressedButtonStyle"];
            SwissimageButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
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
            OSMButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
            SwisstopoButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["ReleasedButtonStyle"];
            SwissimageButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["PressedButtonStyle"];
        }

        /// <summary>
        /// Handle the closeButton-Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CloseButton_Clicked(System.Object sender, System.EventArgs e)
        {
            MessagingCenter.Send<Xamarin.Forms.Application>(App.Current, "SetLayer");
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("..");
            });
        }

        /// <summary>
        /// Handle what happens on layerList itemDragging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LayerList_ItemDragging(System.Object sender, Syncfusion.ListView.XForms.ItemDraggingEventArgs e)
        {
            if (e.Action == Syncfusion.ListView.XForms.DragAction.Drop)
            {
                ViewModel.LayerList_ItemDragging(sender, e);
            }
        }

        /// <summary>
        /// Handles what happens if a layerCheckbox is checked or unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void CheckBox_CheckedChanged(System.Object sender, Xamarin.Forms.CheckedChangedEventArgs e)
        {
            CheckBox checky = sender as CheckBox;
            Preferences.Set("ShowLocalOnly", checky.IsChecked);
            if (checky.IsChecked)
            {
                await ViewModel.AddFileLayers();
            }
            else
            {
                await ViewModel.RemoveFileLayers();
            }
            ViewModel.UpdateMapLayers();
        }

        /// <summary>
        /// Update the layerList
        /// </summary>
        private void UpdateLayerList()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ViewModel.MapLayers.Count > 0)
                {
                    LayerList.ItemsSource = null;
                    LayerList.ItemsSource = ViewModel.MapLayers;
                }
                else
                {
                    LayerList.ItemsSource = null;
                }
            });
        }
    }
}