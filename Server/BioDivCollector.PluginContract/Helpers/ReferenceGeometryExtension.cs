using BioDivCollector.PluginContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.PluginContract.Helpers
{
    public interface IReferenceGeometryExtenstion
    {

        public List<IReferenceGeometryPlugin> Plugins { get; set; }

    }

    public class ReferenceGeometryExtension : IReferenceGeometryExtenstion
    {
        public List<IReferenceGeometryPlugin> Plugins { get; set; }
        public ReferenceGeometryExtension(List<IReferenceGeometryPlugin> availablePlugins)
        {
            Plugins = availablePlugins;                
        }
    }
}
