using System;
using System.IO;
using Java.IO;
using Xamarin.Forms;
using Android.Content;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using static System.Net.WebRequestMethods;

[assembly: Dependency(typeof(BioDivCollectorXamarin.Droid.FilesAndroid))]
namespace BioDivCollectorXamarin.Droid
{
    public class FilesAndroid : FileInterface
    {
        public string GetMbTilesPath()
        {
            var directory = Path.Combine(GetRootFolder(), "mbtiles");
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
            //var directory = Path.Combine(GetRootFolder(), Android.OS.Environment.DirectoryPictures);
            var directory = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).ToString(), "BioDivCollector");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }

        /// <summary>
        /// Holt den Pfad zum allgemeinen Downloadordner. Muss in Zukunft geändert werden, sollte irgendwie mit MedieaStore oder so gelöst werden.
        /// </summary>
        /// <returns>Pfad zum allgemeinen Downloadordner</returns>
        public string GetPathToDownloads()
        {
            string path = Android.OS.Environment.DirectoryDownloads;
            string pathToNewFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(path).ToString();
            return pathToNewFolder;
        }
        
        public string GetBackupPath()
        {
            string pathToNewFolder = Path.Combine(GetRootFolder(), "backup");
            if (!Directory.Exists(pathToNewFolder))
            {
                Directory.CreateDirectory(pathToNewFolder);
            }
            return pathToNewFolder;
        }
    }
}

