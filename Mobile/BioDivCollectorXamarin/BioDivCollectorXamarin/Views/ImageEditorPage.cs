using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;
using Syncfusion.SfImageEditor.XForms;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace BioDivCollectorXamarin.Views
{
    [QueryProperty(nameof(RecIdString), "recid")]
    [QueryProperty(nameof(BinaryIdString), "binaryid")]
    [QueryProperty(nameof(FormIdString), "formid")]
    public class SfImageEditorPage : ContentPage
    {
        public string RecIdString
        {
            set
            {
                if (ViewModel == null) { ViewModel = new ImageEditorViewModel(); }
                //int.TryParse(value, out int parsedValue);
                ViewModel.RecordId = value;
            }
        }
        
        public string FormIdString
        {
            set
            {
                if (ViewModel == null) { ViewModel = new ImageEditorViewModel(); }
                int.TryParse(value, out int parsedValue);
                ViewModel.FormFieldId = parsedValue;
            }
        }

        public string BinaryIdString
        {
            set
            {
                if (ViewModel == null) { ViewModel = new ImageEditorViewModel(); }
                ViewModel.BinaryDataId = value;
            }
        }

        private SfImageEditor Editor { get; set; }
        private ImageEditorViewModel ViewModel { get; set; }

        public SfImageEditorPage()
        {
            if (ViewModel == null) { ViewModel = new ImageEditorViewModel(); }
            Editor = ViewModel.CreateEditor();
            Content = Editor;
        }

        public SfImageEditorPage(int formFieldId, string binaryId, string recordId)
        {
            if (ViewModel == null) { ViewModel = new ImageEditorViewModel(); }
            ViewModel.FormFieldId = formFieldId;
            ViewModel.BinaryDataId = binaryId;
            ViewModel.RecordId = recordId;
            Editor = ViewModel.CreateEditor();
            Content = Editor;
        }

        /// <summary>
        /// Set up the view
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Xamarin.Forms.NavigationPage.SetHasNavigationBar(this, true);
            var safeInsets = On<iOS>().SafeAreaInsets();
            Padding = safeInsets;
            ViewModel.AddToolbarItems();
            Shell.SetNavBarIsVisible(this, true);

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(() =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        var rec = await Record.FetchRecord(ViewModel.RecordId);
                        await Shell.Current.GoToAsync($"//Records/Form?formid={rec.formId}&recid={ViewModel.RecordId}&geomid={rec.geometry_fk}");
                    });
                })
            });
        }

        /// <summary>
        /// When the window is opened, either create a blank sheet, or load the image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width > 0 && height > 0)
            {
                ViewModel.GetData();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }
    }
}

