using System;
using System.IO;
using Android.Graphics;
using Android.Media;
using Java.IO;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using Path = System.IO.Path;

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
                    await fileOutputStream.WriteAsync(bytes);
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


        public int GetImageRotation(string filePath)
        {
            try
            {
                ExifInterface ei = new ExifInterface(filePath);
                Orientation orientation = (Orientation)ei.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Undefined);
                switch (orientation)
                {
                    case Orientation.Rotate90:
                        return 90;
                    case Orientation.Rotate180:
                        return 180;
                    case Orientation.Rotate270:
                        return 270;
                    default:
                        return 0;
                }
                
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
    }
}