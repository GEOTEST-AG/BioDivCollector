using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BioDivCollector.PluginContract
{
    public abstract class BaseReferenceGeometryPluginViewComponent : ViewComponent, IPlugin, IReferenceGeometryPlugin
    {
        public abstract int FormId { get; set; }

        public abstract string GetName();

        public abstract Task<IViewComponentResult> InvokeAsync(ReferenceGeometry referenceGeometry);
    }
}
