using System;
using System.Collections.Generic;
using System.Text;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.IO;

namespace BioDivCollectorXamarin.Helpers
{
    public class Log
    {
        public static string LogMessage { get; set; }

        private async Task SaveDebuggerMessage()
        {
            if (LogMessage != String.Empty && LogMessage != null)
            {
                var date = DateTime.Now.Date;
                var dateOnly = date.Year + "-" + date.Month + "-" + date.Day;
                var pathToDownloads = "";
                pathToDownloads = Path.Combine(DependencyService.Get<FileInterface>().GetPathToDownloads() + "/DebuggMessage_" + dateOnly + ".txt");
                if (File.Exists(pathToDownloads))
                {
                    string text = File.ReadAllText(pathToDownloads);
                    text = text + Environment.NewLine + Environment.NewLine + LogMessage;
                    File.Delete(pathToDownloads);
                    File.WriteAllText(pathToDownloads, text);
                    LogMessage = "";
                }
                else
                {
                    File.WriteAllText(pathToDownloads, LogMessage);
                    LogMessage = "";
                }
            }
        }
    }
}
