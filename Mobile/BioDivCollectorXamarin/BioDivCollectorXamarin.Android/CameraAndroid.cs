using System;
using System.IO;
using Java.IO;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using static BioDivCollectorXamarin.Helpers.Interfaces;

[assembly: Dependency(typeof(BioDivCollectorXamarin.Droid.CameraAndroid))]
namespace BioDivCollectorXamarin.Droid
{
    public class CameraAndroid : CameraInterface
    {
        public Java.IO.File cameraFile;
        Java.IO.File dirFile;

        public void SaveToAlbum(Byte[] bytes)
        {
            //Android does not have an album
        }

        public async void SaveToFile(Byte[] bytes, string filename)
        {
            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filepath = Path.Combine(directory, filename + ".jpg");

            try
            {
                if (System.IO.File.Exists(filepath)) { System.IO.File.Delete(filepath); }
                using (var fileOutputStream = new FileOutputStream(filepath))
                {

                    try
                    {
                        if (bytes != null)
                        {
                            await fileOutputStream.WriteAsync(bytes);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }

        }
    }
}