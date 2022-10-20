using System;
using System.IO;
using Java.IO;
using Xamarin.Forms;
using Android.Content;
using static BioDivCollectorXamarin.Helpers.Interfaces;

[assembly: Dependency(typeof(BioDivCollectorXamarin.Droid.FilesAndroid))]
namespace BioDivCollectorXamarin.Droid
{
    public class FilesAndroid : FileInterface
    {
        public string GetMbTilesPath()
        {
            var directory = Path.Combine(GetRootFolder(), Android.OS.Environment.DirectoryDocuments);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            directory = Path.Combine(directory, "mbtiles");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }

        public string GetRootFolder()
        {
            string rootFolder = null;
            //Get the root path in android device.
            if (Android.OS.Environment.IsExternalStorageEmulated)
            {
                rootFolder = Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath;
            }
            else
            {
                rootFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            return rootFolder;
        }


        public string GetImagePath()
        {
            var directory = Path.Combine(GetRootFolder(), Android.OS.Environment.DirectoryPictures);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }
    }
}

