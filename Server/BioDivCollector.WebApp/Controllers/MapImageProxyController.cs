using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.WebApp.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using static System.Net.WebRequestMethods;

namespace BioDivCollector.WebApp.Controllers
{
    public class MapImageProxyController : Controller
    {
        public IConfiguration Configuration { get; }

        private BioDivContext _context = new BioDivContext();

        public MapImageProxyController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Route("/ProxyWMSSecure/{**layerId}")]
        [AllowAnonymous]
        public Task ProxySecure(int layerId)
        {
            Layer l = _context.Layers.Where(m=>m.LayerId == layerId).FirstOrDefault();
            if (l == null)
            {
                return null;
            }

            var queryString = this.Request.QueryString.Value;

            if (l.Username == null)
            {
                return this.HttpProxyAsync($"{l.Url.Substring(0, l.Url.IndexOf("?"))}{queryString}");
            }

            HttpProxyOptions po = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
        .WithBeforeSend((c, hrm) =>
        {
            hrm.Headers.Remove("Cookie");
            // Set something that is needed for the downstream endpoint.
            hrm.Headers.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(
            System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{l.Username}:{l.Password}")));
            return Task.CompletedTask;
        }).Build();
            return  this.HttpProxyAsync($"{l.Url.Substring(0, l.Url.IndexOf("?"))}{queryString}", po);
        }

        [AcceptVerbs(Http.Get, Http.Head, Http.MkCol, Http.Post, Http.Put)]
        [ReadableBodyStream]
        public JsonResult GetGeoServer(string param, string workbench)
        {
            HttpRequest original = this.HttpContext.Request;

            HttpWebRequest newRequest = (HttpWebRequest)WebRequest.Create(Configuration["Environment:Geoserver"] + workbench + "/ows?");

            newRequest.ContentType = original.ContentType;
            newRequest.Method = original.Method;
            newRequest.UserAgent = "Suters Browser";

            HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            string body;
            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                Task<string> task = stream.ReadToEndAsync();
                task.Wait();
                body = task.Result;
                }

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] originalStream = encoding.GetBytes(body);

            Stream reqStream = newRequest.GetRequestStream();
            reqStream.Write(originalStream, 0, originalStream.Length);
            reqStream.Close();


            //newRequest.GetResponse();
            var httpResponse = (HttpWebResponse)newRequest.GetResponse();



            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                // get the postgis-id from the new objects (WFS-T Answer in XML)

                string myxml = streamReader.ReadToEnd();
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(myxml); // suppose that myXmlString contains "<Names>...</Names>"

                List<string> newObjects = new List<string>();
                foreach (XmlNode x in xml.ChildNodes[1].ChildNodes)
                {
                    if (x.Name == "wfs:InsertResults")
                    {
                        foreach (XmlNode feature in x.ChildNodes)
                        {
                            XmlNode featureId = feature.FirstChild;
                            string newId = featureId.Attributes[0].Value.ToString();
                            newObjects.Add(newId);
                        }
                    }
                }
                /*if (workbench == "restb")
                {
                    // and save it with current values to db
                    foreach (string newObjectID in newObjects)
                    {
                        PostGISHatObjektparameter p = new PostGISHatObjektparameter();
                        Objektparameter op = db.Objektparameter.Find(Int32.Parse(param));
                        p.Objektparameter = op;
                        p.PostGISID = Int32.Parse(newObjectID.Replace("postgislandus.", ""));
                        db.Entry(p).State = System.Data.Entity.EntityState.Added;
                        db.SaveChanges();
                    }
                }
                else if (workbench == "mobitechnik")
                {
                    // and save it with current values to db
                    foreach (string newObjectID in newObjects)
                    {
                        MobiObjekt m = new MobiObjekt();
                        m.ID = newObjectID.Replace("immobilie.", "");

                        db.Entry(m).State = System.Data.Entity.EntityState.Added;
                        db.SaveChanges();
                        return Json(m.ID, JsonRequestBehavior.AllowGet);
                    }
                }*/

                return Json(myxml);
            }
        }
    }
}
