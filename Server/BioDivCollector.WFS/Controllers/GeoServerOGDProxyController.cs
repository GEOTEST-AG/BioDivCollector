using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.WFS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BioDivCollector.WFS.Controllers
{

    [Route("ogd/{*url}")]
    [ApiController]
    [AllowAnonymous]
    public class GeoServerOGDProxyController : ControllerBase
    {

        BioDivContext db = new BioDivContext();

        private readonly ILogger<ValuesController> _logger;
        IConfigurationRoot configuration;
        string geoserver;

        public GeoServerOGDProxyController(ILogger<ValuesController> logger)
        {
            _logger = logger;

            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<GeoServerProxyController>()
                .AddEnvironmentVariables()
                .Build();
            geoserver = configuration.GetSection("Environment").GetSection("Geoserver").Value + configuration.GetSection("Environment").GetSection("GeoserverOGDNameSpace").Value + "/";
        }
        
        [HttpGet]
        // GET: GeoserverProxy
        [AllowAnonymous]
        public ActionResult Get(string url)
        {
            
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

                string contentType = response.ContentType;
                Stream content2 = response.GetResponseStream();
                StreamReader contentReader = new StreamReader(content2);

                string data = contentReader.ReadToEnd()
    .Replace(configuration.GetSection("Environment").GetSection("Geoserver").Value + configuration.GetSection("Environment").GetSection("GeoserverOGDNameSpace").Value, configuration.GetSection("Environment").GetSection("WFSOGDUrl").Value);
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                Stream s = new MemoryStream(bytes);


                return base.File(s, "application/xml");
            }
            catch (Exception e)
            {
                return Content("Error");
            }

        }

    }
}
