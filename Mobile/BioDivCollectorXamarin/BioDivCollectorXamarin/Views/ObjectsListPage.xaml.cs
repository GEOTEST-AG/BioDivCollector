using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ObjectsListPage : ContentPage
    {
        public ObjectsPageVM ViewModel { get; set; }

        /// <summary>
        /// Initialise the geometry list and listen for a record being added
        /// </summary>
        public ObjectsListPage()
        {
            InitializeComponent();

            ViewModel = new ObjectsPageVM();
            BindingContext = ViewModel;

            MessagingCenter.Subscribe<AddRecordButtonCommand>(this, "AddRecord", (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Navigation.PushAsync(new FormSelectionPage(null),true);
                });

            });
        }

        /// <summary>
        /// Add the route (state restoration) and refresh the model every time we switch to the view
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.CurrentRoute = "//Records/Geometries";
            ViewModel = new ObjectsPageVM();
            BindingContext = ViewModel;
        }

        /// <summary>
        /// Pass the selected geometry on to the next page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null)
                return;
            var obj = e.Item as ReferenceGeometry;

            //Deselect Item
            var listView = (ListView)sender;
            listView.SelectedItem = null;
            if (listView.Id == ObjectList.Id)
            {
                var objId = obj.Id.ToString();
                App.CurrentRoute = "//Records?objectId=" + objId;
                await Shell.Current.GoToAsync($"//Records?objectId={objId}", true);
            }
            else
            {
                var rec = await Record.FetchRecord(obj.Id);
                await Navigation.PushAsync(new FormPage(obj.Id,rec.formId,rec.geometry_fk), true);
            }
            
        }


        /// <summary>
        /// Pass the selected geometry on to the next page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObjectList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null)
                return;
            var obj = e.Item as ReferenceGeometry;

            //Deselect Item
            var listView = (ListView)sender;
            listView.SelectedItem = null;
            var objId = obj.Id.ToString();
            App.CurrentRoute = "//Records?objectId=" + objId;
            Shell.Current.GoToAsync($"//Records?objectId={objId}",true);
        }


    }
}
