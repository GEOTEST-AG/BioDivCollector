using BioDivCollectorXamarin;
using Foundation;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using System;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(BioDivCollectorXamarin.iOS.CameraIOS))]
namespace BioDivCollectorXamarin.iOS
{
    public class CameraIOS : CameraInterface
    {

        public async void SaveToAlbum(Byte[] bytes)
        {
            try
            {
                var data = NSData.FromArray(bytes);
                var image = new UIImage(data, 1);
                image.SaveToPhotosAlbum(null);
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Bild konnte nicht gespeichert werden", "", "OK");
            }
        }

        public async void SaveToFile(Byte[] bytes, string filename)
        {
            try
            {
                var data = NSData.FromArray(bytes);
                var image = new UIImage(data, 1);
                await SavePhoto(image, filename);
            }
            catch
            {

            }

        }

        public async Task<string> SavePhoto(UIImage photo, string imageName)
        {
            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filePath = System.IO.Path.Combine(directory, imageName + ".jpg");
            NSData imgData = photo.AsJPEG();
            NSError err = null;
            if (imgData.Save(filePath, false, out err))
            {
                Console.WriteLine("Saved image to " + filePath);
            }
            else
            {
                //Handle the Error!
                await App.Current.MainPage.DisplayAlert("Nicht gespeichert", "Das Bild (" + filePath + ") könnte leider nicht gespeichert werden weil " + err.LocalizedDescription, "OK");
                Console.WriteLine("Could NOT save to " + filePath + " because" + err.LocalizedDescription);
            }
            return null;
        }

    }
}