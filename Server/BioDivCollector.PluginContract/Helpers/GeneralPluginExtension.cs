using BioDivCollector.PluginContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.PluginContract.Helpers
{
    public interface IGeneralPluginExtension
    {

        public List<IPlugin> Plugins { get; set; }

    }

    public class GeneralPluginExtension : IGeneralPluginExtension
    {
        public List<IPlugin> Plugins { get; set; }
        public GeneralPluginExtension(List<IPlugin> availablePlugins)
        {
            Plugins = availablePlugins;                
        }
    }
}
