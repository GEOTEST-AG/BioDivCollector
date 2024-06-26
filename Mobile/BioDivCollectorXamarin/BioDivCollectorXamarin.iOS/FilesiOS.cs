using static BioDivCollectorXamarin.Helpers.Interfaces;
using System;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(BioDivCollectorXamarin.iOS.FilesIOS))]
namespace BioDivCollectorXamarin.iOS
{
    public class FilesIOS : FileInterface
    {
        public string GetMbTilesPath()
        {
            string dbpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dbpath = Path.Combine(dbpath, "mbtiles");
            if (!Directory.Exists(dbpath))
            {
                Directory.CreateDirectory(dbpath);
            }
            return dbpath;
        }

        public string GetImagePath()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var directory = System.IO.Path.Combine(path, "photos");
            if (!File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }
        
        public string GetBackupPath()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var directory = System.IO.Path.Combine(path, "backup");
            if (!File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }
        
        public string GetPathToDownloads()
        {
            return String.Empty;
        }
    }
}