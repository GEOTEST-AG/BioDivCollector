using System;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProjectListPage : ContentPage
    {
        public ProjectListVM ViewModel { get; set; }

        /// <summary>
        /// Initialise the project list and ensure that it returns to the project page on synchronisation
        /// </summary>
        public ProjectListPage()
        {
            InitializeComponent();
            ViewModel = new ProjectListVM();
            BindingContext = ViewModel;

            MessagingCenter.Subscribe<DataDAO, string>(this, "SyncComplete", (sender, message) =>
            {
                if (message == "Data successfully downloaded" || message.Contains("Data successfully downloaded"))
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Shell.Current.Navigation.PopAsync();
                    });
                }
            });
        }

        /// <summary>
        /// Register the route for state restoration
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.CurrentRoute = "//Projects/ProjectList";
        }

        /// <summary>
        /// Deal with the nav bar on leaving the page
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Send<Application>(App.Current, "RemoveNavBar");
        }
    }

    /// <summary>
    /// Provide a bool value according to whether the string is null or empty
    /// </summary>
    public class StringNullOrEmptyBoolConverter : IValueConverter
    {
        /// <summary>Returns false if string is null or empty
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var s = value as string;
            return !string.IsNullOrWhiteSpace(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Provide a bool value according to whether the string is null or empty
    /// </summary>
    public class FormFilledOutColourConverter : IValueConverter
    {
        /// <summary>Returns false if string is null or empty
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var s = value as string;
            var empty = Record.FetchIfRecordHasOnlyEmptyChildren(s).Result;
            return empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Provide a bool value according to whether the string is null
    /// </summary>
    public class ObjectNullBoolConverter : IValueConverter
    {
        /// <summary>Returns false if string is null or empty
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}