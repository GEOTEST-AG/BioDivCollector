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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace BioDivCollector.WFS.Controllers
{

    [Route("{*url}")]
    [Route("biodivcollector-wfs/{*url}")]
    [ApiController]
    public class GeoServerProxyController : ControllerBase
    {

        BioDivContext db = new BioDivContext();

        private readonly ILogger<ValuesController> _logger;
        IConfigurationRoot configuration;
        string geoserver;

        public GeoServerProxyController(ILogger<ValuesController> logger)
        {
            _logger = logger;

            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<GeoServerProxyController>()
                .AddEnvironmentVariables()
                .Build();

            geoserver = configuration.GetSection("Environment").GetSection("Geoserver").Value + configuration.GetSection("Environment").GetSection("GeoserverNameSpace").Value + "/";
        }
        
        
        // GET: GeoserverProxy
        [BasicAuth]
        [HttpGet]
        [HttpPost]
        public async Task<ActionResult> Get(string url)
        {
            ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
            string username = identity.FindFirst("preferred_username").Value;
            User user = db.Users.Find(username);

            List<Project> projects = new List<Project>();
            if (User.IsInRole("DM")) projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);

                if (User.IsInRole("EF")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF));
                if (User.IsInRole("PK")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                if (User.IsInRole("PL")) projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            

            projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE));

            string query = HttpContext.Request.QueryString.Value;

            url = geoserver + url + query;
            url = url.Replace("//geoserver/", "/geoserver/");

            // remove bbox, because to mutualy exclusive with cql_filter
            //url = RemoveQueryStringByKey(url, "bbox");
            /*url += "&cql_filter=bdcguid_projekt in (";
            foreach (Project p in projects.Distinct())
            {
                url += "'" + p.ProjectId + "',";
            }
            url = url.Substring(0, url.Length - 1) + ")";*/



                // Create a request for the URL. 		
    
            var uri = new Uri(url);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            if (newQueryString.Get("REQUEST") == "GetFeature")
            {

                string originalFilter = "";
                // do we have already a filter?
                if (HttpContext.Request.Method != "GET")
                {
                    HttpContext.Request.EnableBuffering();
                    //req.ContentType = "application/json";
                    HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                    //HttpContext.Request.Body.Position = 0;  //***** THIS IS REALLY IMPORTANT GOTCHA

                    var requestStreamOriginal = HttpContext.Request.Body;

                    using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
                    {
                        originalFilter = await stream.ReadToEndAsync();
                    }

                }
                else
                {
                    if (HttpContext.Request.Query.Where(m => m.Key.ToUpper() == "FILTER").Any())
                    {
                        originalFilter = HttpContext.Request.Query.Where(m => m.Key.ToUpper() == "FILTER").Select(m => m.Value).FirstOrDefault();
                    }
                }


                string GetFeatureXML = "<fes:Filter xmlns:fes=\"http://www.opengis.net/fes/2.0\" xmlns:gml=\"http://www.opengis.net/gml/3.2\">";

                //string GetFeatureXML = "<wfs:GetFeature service=\"WFS\" version=\"1.0.0\"\r\n xmlns:fes=\"http://www.opengis.net/fes/2.0\"   outputFormat=\"GML2\"\r\n  xmlns:gml=\"http://www.opengis.net/gml/3.2\" xmlns:wfs=\"http://www.opengis.net/wfs\"\r\n  xmlns:ogc=\"http://www.opengis.net/ogc\"\r\n  xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n  xsi:schemaLocation=\"http://www.opengis.net/wfs\r\n                      http://schemas.opengis.net/wfs/1.0.0/WFS-basic.xsd\">\r\n  <wfs:Query typeName=\"\t" + newQueryString.Get("TYPENAME") + "\">\r\n    <ogc:Filter>\r\n";
                bool hasOriginalfilter = false;
                if (originalFilter != "")
                {
                    hasOriginalfilter = true;
                    originalFilter = originalFilter.Replace("<fes:Filter xmlns:fes=\"http://www.opengis.net/fes/2.0\">", "");
                    originalFilter = originalFilter.Replace("<fes:Filter xmlns:fes=\"http://www.opengis.net/fes/2.0\" xmlns:gml=\"http://www.opengis.net/gml/3.2\">", "");

                    originalFilter = originalFilter.Replace("</fes:Filter>", "");
                    GetFeatureXML += "<fes:And>" + originalFilter;
                }


                GetFeatureXML += "<fes:Or>";
                foreach (Project p in projects.Distinct())
                {
                    GetFeatureXML += "<fes:PropertyIsEqualTo><fes:ValueReference>bdcguid_projekt</fes:ValueReference><fes:Literal>" + p.ProjectId + "</fes:Literal></fes:PropertyIsEqualTo>";
                    //GetFeatureXML += "<ogc:PropertyIsEqualTo>\r\n       <ogc:PropertyName>bdcguid_projekt</ogc:PropertyName>\r\n <ogc:Literal>" + p.ProjectId + "</ogc:Literal>\r\n </ogc:PropertyIsEqualTo>\r\n";
                }

                GetFeatureXML += "</fes:Or>";

                if (hasOriginalfilter) GetFeatureXML += "</fes:And>";
                GetFeatureXML += "</fes:Filter>";
                GetFeatureXML = GetFeatureXML.Replace("\n", "").Replace("\r", "");
                url = RemoveQueryStringByKey(url, "FILTER");
                url = RemoveQueryStringByKey(url, "BBOX");
                url = Uri.EscapeUriString(url);

                var client = new RestClient(url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("FILTER", GetFeatureXML);
                try
                {
                    IRestResponse response = client.Execute(request);
                    string data = response.Content.Replace(configuration.GetSection("Environment").GetSection("Geoserver").Value + configuration.GetSection("Environment").GetSection("GeoserverNameSpace").Value, configuration.GetSection("Environment").GetSection("WFSOGDUrl").Value);
                    byte[] bytes2 = Encoding.UTF8.GetBytes(data);
                    Stream s = new MemoryStream(bytes2);


                    return base.File(s, "application/xml");

                }
                catch (Exception ex)
                {
                    return Content("Error: " + url);
                }

            }
            else
            {

                var req = HttpWebRequest.Create(url);

                //req.Method = "POST";
                req.Credentials = CredentialCache.DefaultCredentials;
                try
                {
                    // No more ProtocolViolationException!
                    HttpWebResponse response = (HttpWebResponse)req.GetResponse();

                    string contentType = response.ContentType;
                    Stream content2 = response.GetResponseStream();
                    StreamReader contentReader = new StreamReader(content2);

                    string data = contentReader.ReadToEnd().Replace(configuration.GetSection("Environment").GetSection("Geoserver").Value + configuration.GetSection("Environment").GetSection("GeoserverNameSpace").Value, configuration.GetSection("Environment").GetSection("WFSUrl").Value);
                    byte[] bytes2 = Encoding.UTF8.GetBytes(data);
                    Stream s = new MemoryStream(bytes2);


                    return base.File(s, "application/xml");
                }
                catch (Exception e)
                {
                    return Content("Error: " + url);
                }
            }

        }

        public string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            // this removes the key if exists
            newQueryString.Remove(key);

            // this gets the page path from root without QueryString
            string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            string query = "";
            bool isFirst = true;
            foreach (string param in newQueryString)
            {
                if (!isFirst) query += "&";
                isFirst = false;
                query += param + "=" + newQueryString[param];
            }


            return newQueryString.Count > 0
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, query)
                : pagePathWithoutQueryString;
        }
    }
}
