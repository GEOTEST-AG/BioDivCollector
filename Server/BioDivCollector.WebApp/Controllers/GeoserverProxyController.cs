 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BioDivCollector.WebApp.Controllers
{
    public class GeoserverProxyController : Controller
    {
        public IConfiguration Configuration { get; }


        public GeoserverProxyController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // GET: GeoserverProxy
        public ActionResult Http()
        {


         string geoserver = Configuration["Environment:Geoserver"];
        //return Content()
        string content;
            string url = HttpContext.Request.Path.Value.Replace("/proxy", "");
            string query = HttpContext.Request.QueryString.Value;


            url = geoserver + url + query;
            url = url.Replace("//geoserver/", "/geoserver/");
            // Create a request for the URL. 		
            var req = HttpWebRequest.Create(url);
            req.Method = HttpContext.Request.Method;

            //-- No need to copy input stream for GET (actually it would throw an exception)
            if (req.Method != "GET")
            {
                //req.ContentType = "application/json";
                req.ContentType = HttpContext.Request.ContentType;

                Request.Body.Position = 0;  //***** THIS IS REALLY IMPORTANT GOTCHA

                var requestStream = HttpContext.Request.Body;
                Stream webStream = null;
                try
                {
                    //copy incoming request body to outgoing request
                    if (requestStream != null && requestStream.Length > 0)
                    {
                        req.ContentLength = requestStream.Length;
                        webStream = req.GetRequestStream();
                        requestStream.CopyTo(webStream);
                    }
                }
                finally
                {
                    if (null != webStream)
                    {
                        webStream.Flush();
                        webStream.Close();
                    }
                }
            }

            // If required by the server, set the credentials.
            req.Credentials = CredentialCache.DefaultCredentials;
            try
            {
                // No more ProtocolViolationException!
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();

                // Display the status.
                //Console.WriteLine(response.StatusDescription);

                //Response.ContentType = response.ContentType;
                // Get the stream containing content returned by the server.

                string contentType = response.ContentType;
                Stream content2 = response.GetResponseStream();
                StreamReader contentReader = new StreamReader(content2);

                return base.File(content2, contentType);
            }
            catch (Exception e)
            {
                return Content("Error");
                //throw new HttpException(404, "The Proxy returned an error: " + e.ToString(), e.InnerException);
            }

        }
    }
}
