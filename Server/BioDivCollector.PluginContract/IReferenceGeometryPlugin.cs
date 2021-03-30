using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BioDivCollector.PluginContract
{
    public interface IReferenceGeometryPlugin : IPlugin
    {
        public int FormId { get; set; }

    }
}
