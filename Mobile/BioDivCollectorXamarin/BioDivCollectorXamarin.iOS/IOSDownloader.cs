using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Helpers;
using Foundation;
using Xamarin.Essentials;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using BioDivCollectorXamarin.Helpers;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.iOS
{
    public class IosDownloader : IDownloader
    {
        public event EventHandler OnFileDownloaded;


        public void DownloadFileAsync(string url, string folder, string documentName)
        {
            string pathToNewFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), folder);
            Directory.CreateDirectory(pathToNewFolder);

            try
            {
                Task.Run(async () => {
                    WebClient webClient = new WebClient();
                    webClient.UseDefaultCredentials = false;
                    var token = Preferences.Get("AccessToken","");
                    webClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                    string pathToNewFile = Path.Combine(pathToNewFolder, documentName);
                    webClient.DownloadFileAsync(new Uri(url), pathToNewFile);
                });
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
            /*await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(pathToNewFile),
                PresentationSourceBounds = DeviceInfo.Platform == DevicePlatform.iOS && DeviceInfo.Idiom == DeviceIdiom.Tablet
        ? new System.Drawing.Rectangle(0, 20, 0, 0)
        : System.Drawing.Rectangle.Empty
            });*/
        }
    }
}


