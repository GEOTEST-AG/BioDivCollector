using System;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BioDivCollector.WebApp.Controllers;

namespace BioDivCollector.SamplePlugin
{
    public class StandardPluginViewComponent : BaseReferenceGeometryPluginViewComponent
    {
        private BioDivContext db = new BioDivContext();

        public override int FormId { get => 2; set => throw new NotImplementedException(); }

        public override string GetName()
        {
            return "StandardPlugin";
        }

        public override async Task<IViewComponentResult> InvokeAsync(ReferenceGeometry referenceGeometry)
        {
            return View(referenceGeometry);
        }
    }
}
