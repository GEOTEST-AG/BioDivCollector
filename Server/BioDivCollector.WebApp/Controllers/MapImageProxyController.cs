using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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

        public MapImageProxyController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [AllowAnonymous]
        public ActionResult GetProxyImage(string Layer, string TileMatrix, string TileCol, string TileRow)
        {
            //https://wmts102.geo.admin.ch/1.0.0/ch.swisstopo.lubis-luftbilder_farbe/default/99991231/21781/26/1227/1428.png
            if (Layer == "ch.swisstopo.swissimage")
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://wmts.geo.admin.ch/1.0.0/" + Layer + "/default/current/21781/" + TileMatrix + "/" + TileCol + "/" + TileRow + ".jpeg");
                request.Referer = "http://localhost";
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode.ToString().ToLower() == "ok")
                {
                    string contentType = response.ContentType;
                    Stream content = response.GetResponseStream();
                    StreamReader contentReader = new StreamReader(content);

                    return base.File(content, "image/jpg");

                }

                return new HttpStatusCodeResult(404, "Error in image proxy");

            }

            if (Layer == "geologie")
            {
                Layer = "swisstopo-pixelkarte";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://maps.geotest.ch/mapproxy/wmts/geologie/" + Layer + "/" + TileMatrix + "/" + TileCol + "/" + TileRow + ".png");
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode.ToString().ToLower() == "ok")
                {
                    string contentType = response.ContentType;
                    Stream content = response.GetResponseStream();
                    StreamReader contentReader = new StreamReader(content);

                    return base.File(content, "image/png");

                }

                return new HttpStatusCodeResult(404, "Error in image proxy");
            }
            else
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://maps.geotest.ch/mapproxy/wmts/swisstopo/" + Layer + "/" + TileMatrix + "/" + TileCol + "/" + TileRow + ".jpeg");
                    request.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    if (response.StatusCode.ToString().ToLower() == "ok")
                    {
                        string contentType = response.ContentType;
                        Stream content = response.GetResponseStream();
                        StreamReader contentReader = new StreamReader(content);

                        return base.File(content, "image/jpg");

                    }

                    return new HttpStatusCodeResult(404, "Error in image proxy");
                }
                catch (Exception e)
                {
                    return new HttpStatusCodeResult(404, "Error in image proxy: " + e.ToString());
                }
            }

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
