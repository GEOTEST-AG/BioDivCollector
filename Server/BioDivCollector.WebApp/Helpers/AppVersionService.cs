using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BioDivCollector.WebApp.Helpers
{
    public interface IAppVersionService
    {
        string Version { get; }
        string BuildTime { get; }
    }

    public class AppVersionService : IAppVersionService
    {
        public string Version =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public string BuildTime => GetBuildDateTime(Assembly.GetEntryAssembly());


        #region Gets the build date and time (by reading the COFF header)

        // http://msdn.microsoft.com/en-us/library/ms680313


        static string GetBuildDateTime(Assembly assembly)
        {
            string resourcePath = "BioDivCollector.WebApp.Resources.BuildDate.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }

           
        }

            #endregion
        }
}
