﻿using System;

namespace BioDivCollectorXamarin.Helpers
{
    public class Interfaces
    {
        public interface CameraInterface
        {
            void SaveToAlbum(Byte[] bytes);
            void SaveToFile(Byte[] bytes, string filename);
            int GetImageRotation(string filePath);
        }

        public interface FileInterface
        {
            string GetMbTilesPath();
            string GetImagePath();
            string GetBackupPath();
            string GetPathToDownloads();
        }

        public interface IDownloader
        {
            void DownloadFileAsync(string url, string folder, string documentName);
            event EventHandler OnFileDownloaded;
        }

        public interface ILocalize
        {
            void SetLocale(string language);
        }
    }

    public class DownloadEventArgs : EventArgs
    {
        public bool FileSaved = false;
        public DownloadEventArgs(bool fileSaved)
        {
            FileSaved = fileSaved;
        }
    }
}
