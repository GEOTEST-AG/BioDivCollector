using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Button = Xamarin.Forms.Button;

namespace BioDivCollectorXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CameraView : ContentPage
    {

        private CameraViewModel ViewModel;

        public CameraView(Record rec, int formFieldId, string binaryDataId)
        {
            InitializeComponent();
            Preferences.Set("lastRecId", rec.recordId);
            Preferences.Set("lastBinaryId", binaryDataId);
            Preferences.Set("lastFormFieldId", formFieldId);
            ViewModel = new CameraViewModel(rec, formFieldId, binaryDataId);
            this.BindingContext = ViewModel;
            Title = "Bild hinzufügen";
            ContentStack.Margin = 20;
            ContentStack.VerticalOptions = LayoutOptions.CenterAndExpand;

            Button browsePhoto = new Button();
            browsePhoto.Clicked += ViewModel.BrowsePhoto_Clicked;
            browsePhoto.TextColor = Color.White;
            browsePhoto.BackgroundColor = Color.Transparent;
            browsePhoto.VerticalOptions = LayoutOptions.End;
            browsePhoto.ImageSource = new FontImageSource() { FontFamily = "Material", Glyph = "\ue413", Color = Color.White, Size = 40 };
            browsePhoto.Text = "Album";
            browsePhoto.FontSize = 24;
            browsePhoto.BackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
            browsePhoto.WidthRequest = 150;
            browsePhoto.HeightRequest = 80;
            browsePhoto.Padding = 20;

            Button takePhoto = new Button();
            takePhoto.Clicked += ViewModel.TakePhoto_Clicked;
            takePhoto.TextColor = Color.White;
            takePhoto.BackgroundColor = Color.Transparent;
            takePhoto.VerticalOptions = LayoutOptions.Start;
            takePhoto.ImageSource = new FontImageSource() { FontFamily = "Material", Glyph = "\ue412", Color = Color.White, Size = 40 };
            takePhoto.Text = "Kamera";
            takePhoto.FontSize = 24;
            takePhoto.BackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
            takePhoto.WidthRequest = 150;
            takePhoto.HeightRequest = 80;
            takePhoto.Padding = 20;

            Button makeSketch = new Button();
            makeSketch.Clicked += ViewModel.MakeSketch_Clicked;
            makeSketch.TextColor = Color.White;
            makeSketch.BackgroundColor = Color.Transparent;
            makeSketch.VerticalOptions = LayoutOptions.Start;
            makeSketch.ImageSource = new FontImageSource() { FontFamily = "Material", Glyph = "\ue3c6", Color = Color.White, Size = 40 };
            makeSketch.Text = "Sketch";
            makeSketch.FontSize = 24;
            makeSketch.BackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
            makeSketch.WidthRequest = 150;
            makeSketch.HeightRequest = 80;
            makeSketch.Padding = 20;

            ContentStack.Children.Add(browsePhoto);
            ContentStack.Children.Add(takePhoto);
            ContentStack.Children.Add(makeSketch);

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(() =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Shell.Current.Navigation.PopAsync();
                    });
                })
            });

            MessagingCenter.Subscribe<byte[]>(this, "ImageSelected", (args) =>
            {
                //MessagingCenter.Unsubscribe<byte[]>(this, "ImageSelected");
                Device.BeginInvokeOnMainThread(() =>
                {
                    ViewModel.SwitchView();
                });
            });

        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}
