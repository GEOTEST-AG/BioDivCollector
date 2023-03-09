using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BioDivCollector.DB.Models.Domain;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Security.Claims;
using BioDivCollector.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BioDivCollector.WebApp.Controllers
{
    public class LayersController : Controller
    {
        private BioDivContext _context = new BioDivContext();

        public LayersController()
        {
        }

        // GET: Layers
        public async Task<IActionResult> Index()
        {
            DB.Models.Domain.User me = UserHelper.GetCurrentUser(User, _context);
            List<Layer> alllayers = await _context.Layers.Include(m => m.LayerUsers).Include(m => m.LayerChangeLogs).ThenInclude(c=>c.ChangeLog).ThenInclude(cl => cl.User).Where(m => m.Public == true).ToListAsync();
            alllayers.AddRange(await _context.Layers.Include(m => m.LayerUsers).Include(m => m.LayerChangeLogs).ThenInclude(c => c.ChangeLog).ThenInclude(cl=>cl.User).Where(m => m.LayerUsers.Any(u => u.UserId == me.UserId)).ToListAsync());


            List<LayerViewModel> lvms = new List<LayerViewModel>();
            foreach (Layer l in alllayers)
            {
                bool isEditable = true;
                if (!User.IsInRole("DM"))
                {
                    if (l.LayerUsers == null) isEditable = false; 
                    else if (l.LayerUsers.Where(m => m.UserId == me.UserId).Count() == 0)
                    {
                        ChangeLogLayer chll = l.LayerChangeLogs.Take(1).FirstOrDefault();
                        if (chll?.ChangeLog.User.UserId != me.UserId)
                        {
                            isEditable = false;
                        }
                    }
                }

                LayerViewModel lvm = new LayerViewModel() { Layer = l, Editable = isEditable };
                lvms.Add(lvm);
            }

            return View(lvms);
        }


        // GET: Layers/Create
        public IActionResult Create()
        {
            return View();
        }

        private List<WMSLayer> _LayerCache;
        private string _wmsurlcache;

        public IActionResult GetWMSLayers(string wmsurl, string search, string username, string password)
        {
            if ((search==null) || (search == "uniqueSearchQueryOrElseCacheWillBeUsed")) search = "";
            try
           {
                if (_wmsurlcache != wmsurl)
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    byte[] data;
                    using (WebClient webClient = new WebClient())
                    {
                        if (((username != null) && (password != null)) && (!wmsurl.Contains(username + ":")))
                        {
                            webClient.Credentials = new NetworkCredential(username, password);
                        }
                        data = webClient.DownloadData(wmsurl);
                    }

                    string str = Encoding.GetEncoding("UTF-8").GetString(data);
                    XDocument xdoc = XDocument.Parse(str);
                    XNamespace df = xdoc.Root.Name.Namespace;

                    if (xdoc.FirstNode.GetType() == typeof(XDocumentType))
                    {
                        xdoc.FirstNode.Remove();
                    }

                    var layers = from l in ((XElement)xdoc.FirstNode).Descendants(df + "Layer")
                                 select new WMSLayer
                                 {
                                     Name = (string)l.Element(df + "Name"),
                                     Title = (string)l.Element(df + "Title")
                                 };

                    _wmsurlcache = wmsurl;
                    _LayerCache = layers.ToList();
                }

                string returnlist = "{\"items\":[ ";
                foreach (WMSLayer we in _LayerCache.Where(  m=>(m.Title!=null) && (m.Name!=null) && (m.Title.Contains(search) || m.Name.Contains(search))))
                {
                    returnlist += "{\"ID\":\"" + we.Name.Replace("\"", "\\\"") + "\",\"Title\":\"" + we.Title.Replace("\"", "\\\"") + "\"},";
                }


                returnlist = returnlist.Substring(0, returnlist.Length - 1) + "]}";
                return Content(returnlist, "application/json");
            }
            catch (Exception e)
            {
                string errorreturnlist = "{\"items\":[ "+ "{\"ID\":\"ERROR\",\"Title\":\"Bitte WMS GetCapabilities-URL eingeben. Fehler beim Parsen.\"}]}";
                return Content(errorreturnlist, "application/json");
            }
        }


        // POST: Layers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LayerId,Public,Title,Url,WMSLayer, Username, Password")] Layer layer)
        {
            if (ModelState.IsValid)
            {
                User me = UserHelper.GetCurrentUser(User, _context);


                ChangeLog cl = new ChangeLog() { Log = "New Layer " + layer.Title, User = me };
                ChangeLogLayer cll = new ChangeLogLayer() { ChangeLog = cl, Layer = layer };
                layer.LayerChangeLogs = new List<ChangeLogLayer>();
                layer.LayerChangeLogs.Add(cll);

                if (!layer.Public)
                {
                    UserLayer ul = new UserLayer() { User = me, Layer = layer };
                    layer.LayerUsers = new List<UserLayer>();
                    layer.LayerUsers.Add(ul);

                }

                _context.Add(layer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(layer);
        }

        // GET: Layers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var layer = await _context.Layers.Include(m => m.LayerUsers).Include(m=>m.LayerChangeLogs).ThenInclude(m=>m.ChangeLog).ThenInclude(m=>m.User).Where(m => m.LayerId == id).FirstOrDefaultAsync();
            if (layer == null)
            {
                return NotFound();
            }

            // check if Layer is my userlayer or I am the Creator (first in changelog) or I am DM
            if (!User.IsInRole("DM"))
            {
                User me = UserHelper.GetCurrentUser(User, _context);
                if (layer.LayerUsers.Where(m=>m.UserId==me.UserId).Count()==0)
                {
                    ChangeLogLayer chll = layer.LayerChangeLogs.Take(1).FirstOrDefault();
                    if (chll?.ChangeLog.User.UserId != me.UserId)
                    {
                        return RedirectToAction("NotAllowed","Home");
                    }
                }
            }

            layer.Password = "";

            return View(layer);
        }

        // POST: Layers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LayerId,Public,Title,Url,WMSLayer, Username, Password")] Layer layer)
        {
            if (id != layer.LayerId)
            {
                return NotFound();
            }
            var layerOld = await _context.Layers.Include(m => m.LayerUsers).Include(m => m.LayerChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User).Where(m => m.LayerId == id).FirstOrDefaultAsync();

            User me = UserHelper.GetCurrentUser(User, _context);
            // check if Layer is my userlayer or I am the Creator (first in changelog) or I am DM
            if (!User.IsInRole("DM"))
            {
                if (layerOld.LayerUsers.Where(m => m.UserId == me.UserId).Count() == 0)
                {
                    ChangeLogLayer chll = layer.LayerChangeLogs.Take(1).FirstOrDefault();
                    if (chll?.ChangeLog.User.UserId != me.UserId)
                    {
                        return RedirectToAction("NotAllowed", "Home");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    layerOld.Public = layer.Public;
                    layerOld.Title = layer.Title;
                    layerOld.Url = layer.Url;
                    layerOld.WMSLayer = layer.WMSLayer;
                    
                    if (layer.Username != null) { 
                        layerOld.Username= layer.Username;
                    
                    }
                    if ((layer.Password!= null) && (layer.Password!="")) {
                        layerOld.Password= layer.Password;
                    }

                    ChangeLog cl = new ChangeLog() { Log = "Changed Layer " + layer.Title, User = me };
                    ChangeLogLayer cll = new ChangeLogLayer() { ChangeLog = cl, Layer = layer };
                    layerOld.LayerChangeLogs.Add(cll);

                    _context.Update(layerOld);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LayerExists(layer.LayerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(layer);
        }

        // GET: Layers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var layer = await _context.Layers.Include(m => m.LayerUsers).Include(m => m.LayerChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User).Where(m => m.LayerId == id).FirstOrDefaultAsync();


            User me = UserHelper.GetCurrentUser(User, _context);
            // check if Layer is my userlayer or I am the Creator (first in changelog) or I am DM
            if (!User.IsInRole("DM"))
            {
                if (layer.LayerUsers.Where(m => m.UserId == me.UserId).Count() == 0)
                {
                    ChangeLogLayer chll = layer.LayerChangeLogs.Take(1).FirstOrDefault();
                    if (chll?.ChangeLog.User.UserId != me.UserId)
                    {
                        return RedirectToAction("NotAllowed", "Home");
                    }
                }
            }
            if (layer == null)
            {
                return NotFound();
            }

            return View(layer);
        }

        // POST: Layers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var layer = await _context.Layers.Include(m => m.LayerUsers).Include(m => m.LayerChangeLogs).ThenInclude(m => m.ChangeLog).ThenInclude(m => m.User).Where(m => m.LayerId == id).FirstOrDefaultAsync();

            User me = UserHelper.GetCurrentUser(User, _context);
            // check if Layer is my userlayer or I am the Creator (first in changelog) or I am DM
            if (!User.IsInRole("DM"))
            {
                if (layer.LayerUsers.Where(m => m.UserId == me.UserId).Count() == 0)
                {
                    ChangeLogLayer chll = layer.LayerChangeLogs.Take(1).FirstOrDefault();
                    if (chll?.ChangeLog.User.UserId != me.UserId)
                    {
                        return RedirectToAction("NotAllowed", "Home");
                    }
                }
            }

            List<UserHasProjectLayer> upls = await _context.UsersHaveProjectLayers.Where(m => m.LayerId == layer.LayerId).ToListAsync();
            foreach (UserHasProjectLayer upl in upls)
            {
                _context.UsersHaveProjectLayers.Remove(upl);
            }
            layer.LayerUsers = null;
            layer.LayerChangeLogs = null;


            _context.Layers.Remove(layer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Change the settings for layer visible or not in settings
        /// </summary>
        /// <param name="id">LayerId</param>
        /// <param name="visible">Visible yes / no</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeEnabledLayer(int id, bool visible)
        {
            string projectid = HttpContext.Session.GetString("Project");
            Project p = _context.Projects.Find(new Guid(projectid));
            User me = UserHelper.GetCurrentUser(User, _context);
            Layer l = _context.Layers.Find(id);

            if ((p!=null) && (me!=null) && (l!=null))
            {
                UserHasProjectLayer upl = _context.UsersHaveProjectLayers.Where(m => m.User == me && m.Project == p && m.Layer == l).FirstOrDefault();
                if (upl == null)
                {
                    upl = new UserHasProjectLayer() { Layer = l, Project = p, User = me, Visible = visible, Transparency = 1 };
                    _context.UsersHaveProjectLayers.Add(upl);
                }
                else
                {
                    upl.Visible = visible;
                    _context.Entry(upl).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
                return Content("OK");
            }
            return Content("Error");
        }

        /// <summary>
        /// Change the settings for layer visible or not in settings
        /// </summary>
        /// <param name="id">LayerId</param>
        /// <param name="visible">Visible yes / no</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeTransparencyLayer(int id, double transparency)
        {
            string projectid = HttpContext.Session.GetString("Project");
            Project p = _context.Projects.Find(new Guid(projectid));
            User me = UserHelper.GetCurrentUser(User, _context);
            Layer l = _context.Layers.Find(id);

            if ((p != null) && (me != null) && (l != null))
            {
                UserHasProjectLayer upl = _context.UsersHaveProjectLayers.Where(m => m.User == me && m.Project == p && m.Layer == l).FirstOrDefault();
                if (upl == null)
                {
                    upl = new UserHasProjectLayer() { Layer = l, Project = p, User = me, Transparency = transparency };
                    _context.UsersHaveProjectLayers.Add(upl);
                }
                else
                {
                    upl.Transparency = transparency;
                    _context.Entry(upl).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
                return Content("OK");
            }
            return Content("Error");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeLayerOrder([FromBody] LayerIds ids)
        {
            string projectid = HttpContext.Session.GetString("Project");
            Project p = _context.Projects.Find(new Guid(projectid));
            User me = UserHelper.GetCurrentUser(User, _context);

            if ((p != null) && (me != null))
            {
                for (int i = 0; i < ids.Ids.Count(); i++)
                {
                    Layer l = _context.Layers.Find(Int32.Parse(ids.Ids[i]));
                    UserHasProjectLayer upl = _context.UsersHaveProjectLayers.Where(m => m.User == me && m.Project == p && m.Layer == l).FirstOrDefault();
                    if (upl == null)
                    {
                        upl = new UserHasProjectLayer() { Layer = l, Project = p, User = me, Transparency = 1, Visible = false, Order = i };
                        _context.UsersHaveProjectLayers.Add(upl);
                    }
                    else
                    {
                        upl.Order = i;
                        _context.Entry(upl).State = EntityState.Modified;
                    }
                }
                await _context.SaveChangesAsync();
                return Content("OK");
            }
            return Content("Error");
        }



        private bool LayerExists(int id)
        {
            return _context.Layers.Any(e => e.LayerId == id);
        }
    }

    public class WMSLayer
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string SRS { get; set; }

        public List<string> Styles = new List<string>();
    }

    public class LayerIds
    {
        public List<string> Ids { get; set; }
    }

    public class LayerViewModel
    {
        public Layer Layer { get; set; }
        public bool Editable { get; set; }
    }
}
