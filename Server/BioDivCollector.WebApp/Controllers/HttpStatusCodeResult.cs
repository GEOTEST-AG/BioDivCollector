using Microsoft.AspNetCore.Mvc;

namespace BioDivCollector.WebApp.Controllers
{
    internal class HttpStatusCodeResult : ActionResult
    {
        private int v1;
        private string v2;

        public HttpStatusCodeResult(int v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
    }
}