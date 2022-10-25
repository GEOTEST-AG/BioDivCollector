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

        public byte[] RotateImage(System.IO.Stream imageStream, string filePath)
        {
            int rotationDegrees = GetImageRotation(filePath);
            ExifInterface ei = new ExifInterface(filePath);
            Orientation orientation = (Orientation)ei.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Undefined);
            //ei.SetAttribute(ExifInterface.TagOrientation, ((int)Orientation.Normal).ToString());
            //ei.SaveAttributes();
            byte[] byteArray = new byte[imageStream.Length];
            try
            {
                imageStream.Read(byteArray, 0, (int)imageStream.Length);

                Bitmap originalImage = BitmapFactory.DecodeByteArray(byteArray, 0, byteArray.Length);
                Matrix matrix = new Matrix();
                matrix.PostRotate((float)rotationDegrees);

                Bitmap rotatedBitmap = Bitmap.CreateBitmap(originalImage, 0, 0, originalImage.Width,
                    originalImage.Height, matrix, true);

                using (MemoryStream ms = new MemoryStream())
                {
                    rotatedBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                return byteArray;
            }
        }
    }
}