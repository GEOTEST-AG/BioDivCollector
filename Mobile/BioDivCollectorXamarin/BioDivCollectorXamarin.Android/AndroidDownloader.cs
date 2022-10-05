using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Helpers;
using Xamarin.Essentials;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.Droid
{
    public class AndroidDownloader : IDownloader
    {
        public event EventHandler OnFileDownloaded;

        public async void DownloadFileAsync(string url, string folder, string documentName)
        {
            string pathToNewFolder = Path.Combine(Android.OS.Environment.DirectoryDocuments, folder);
            Directory.CreateDirectory(pathToNewFolder);

            try
            {
                WebClient webClient = new WebClient();
                webClient.UseDefaultCredentials = false;
                var token = await SecureStorage.GetAsync("AccessToken");
                webClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                string pathToNewFile = Path.Combine(pathToNewFolder, Path.GetFileName(url));
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadFileAsync(new Uri(url), pathToNewFile);
                Launcher.OpenAsync(pathToNewFile);
            }
            catch (Exception ex)
            {
                if (OnFileDownloaded != null)
                    OnFileDownloaded.Invoke(this, new DownloadEventArgs(false));
            }
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (OnFileDownloaded != null)
                    OnFileDownloaded.Invoke(this, new DownloadEventArgs(false));
            }
            else
            {
                if (OnFileDownloaded != null)
                    OnFileDownloaded.Invoke(this, new DownloadEventArgs(true));
            }
        }
    }
}

