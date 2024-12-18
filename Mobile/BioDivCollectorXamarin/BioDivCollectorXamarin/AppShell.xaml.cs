﻿using System.Runtime.CompilerServices;
using BioDivCollectorXamarin.Views;
using Xamarin.Forms;

namespace BioDivCollectorXamarin
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        /// <summary>
        /// Enable tabs and state restoration
        /// </summary>
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
            Routing.RegisterRoute(nameof(ObjectsListPage), typeof(ObjectsListPage));
            Routing.RegisterRoute(nameof(RecordsPage), typeof(RecordsPage));
            Routing.RegisterRoute("//Records/Form", typeof(FormPage));
            Routing.RegisterRoute("//Records/Form/ImageEditor", typeof(SfImageEditorPage));
            Routing.RegisterRoute("//Projects/ProjectList", typeof(ProjectListPage));
            Routing.RegisterRoute("//Map/MapLayers", typeof(MapLayersPage));
            Routing.RegisterRoute("//Records/FormSelection", typeof(FormSelectionPage));
            Routing.RegisterRoute("Records/Geometries", typeof(ObjectsListPage));
            Routing.RegisterRoute("Register", typeof(RegistrationPage));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

        }

        /// <summary>
        /// Pop to root for each tab when the current project is changed
        /// </summary>
        public static void ClearNavigationStacks()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var tabs = Shell.Current.Items[0].Items;
                foreach (var tab in tabs)
                {
                    tab.Navigation.PopToRootAsync();
                }
            });
        }
    }
}
