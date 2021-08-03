using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using BioDivCollector.WebApp.Helpers;
using FormFactory;
using FormFactory.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioDivCollector.WebApp.Controllers
{
    public class ReferenceGeometryController : Controller
    {
        private BioDivContext db = new BioDivContext();
        private ReferenceGeometryExtension _referenceGeometryExtension;
        private GeneralPluginExtension _generalPluginExtension;

        public ReferenceGeometryController(ReferenceGeometryExtension referenceGeometryExtension, GeneralPluginExtension generalPluginExtension)
        {
            _referenceGeometryExtension = referenceGeometryExtension;
            _generalPluginExtension = generalPluginExtension;
        }

        public IActionResult GetUserJson()
        {
            string geojson = createGeoJson();
            if (geojson == "") return StatusCode(500);

            return Content(geojson);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            ReferenceGeometry rg = await db.Geometries
                .Include(m=>m.ProjectGroup).ThenInclude(pg => pg.Group).ThenInclude(g => g.GroupUsers)
                .Where(m => m.GeometryId == id).FirstOrDefaultAsync();

            if (rg == null) return StatusCode(404);
            // No right for it, User is not in Group

            if ((rg.ProjectGroup.Group.GroupUsers.Any(m => m.UserId == user.UserId)) || User.IsInRole("DM"))
            {
                rg.StatusId = StatusEnum.deleted;
                ChangeLog cl = new ChangeLog() { Log = "deleted geometry", User = user };
                db.ChangeLogs.Add(cl);
                ChangeLogGeometry clg = new ChangeLogGeometry() { ChangeLog = cl, Geometry = rg };
                db.ChangeLogsGeometries.Add(clg);

                db.Entry(rg).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Content("OK");
            }

            // No right for it, User is not in Group

            return StatusCode(403);

        }

        public async Task<IActionResult> Rename(Guid id, string newName)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            ReferenceGeometry rg = await db.Geometries
                .Include(m => m.ProjectGroup).ThenInclude(pg => pg.Group).ThenInclude(g => g.GroupUsers)
                .Where(m => m.GeometryId == id).FirstOrDefaultAsync();

            if (rg == null) return StatusCode(404);
            // No right for it, User is not in Group

            if ((rg.ProjectGroup.Group.GroupUsers.Any(m => m.UserId == user.UserId)) || User.IsInRole("DM"))
            {
                rg.GeometryName = newName;
                ChangeLog cl = new ChangeLog() { Log = "renamed geometry to " + newName, User = user };
                db.ChangeLogs.Add(cl);
                ChangeLogGeometry clg = new ChangeLogGeometry() { ChangeLog = cl, Geometry = rg };
                db.ChangeLogsGeometries.Add(clg);

                db.Entry(rg).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Content("OK");
            }

            // No right for it, User is not in Group

            return StatusCode(403);

        }

        public async Task<IActionResult> AddChangelog(Guid id, bool update=false)
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);
            ReferenceGeometry rg = await db.Geometries.FindAsync(id);
            if (rg!=null)
            {
                ChangeLog cl = new ChangeLog() { User = user, Log = "Created new geometry" };
                if (update) cl.Log = "Changed geometry";
                ChangeLogGeometry clg = new ChangeLogGeometry() { ChangeLog = cl, Geometry = rg };
                db.ChangeLogs.Add(cl);
                db.ChangeLogsGeometries.Add(clg);

                await db.SaveChangesAsync();
                return Json("OK");
            }
            return Json("GeometryId not found");

        }

        public readonly string[] formats = { 
    // Basic formats
    "yyyyMMddTHHmmsszzz",
    "yyyyMMddTHHmmsszz",
    "yyyyMMddTHHmmssZ",
    // Extended formats
    "yyyy-MM-ddTHH:mm:sszzz",
    "yyyy-MM-ddTHH:mm:sszz",
    "yyyy-MM-ddTHH:mm:ssZ",
    // All of the above with reduced accuracy
    "yyyyMMddTHHmmzzz",
    "yyyyMMddTHHmmzz",
    "yyyyMMddTHHmmZ",
    "yyyy-MM-ddTHH:mmzzz",
    "yyyy-MM-ddTHH:mmzz",
    "yyyy-MM-ddTHH:mmZ",
    // Accuracy reduced to hours
    "yyyyMMddTHHzzz",
    "yyyyMMddTHHzz",
    "yyyyMMddTHHZ",
    "yyyy-MM-ddTHHzzz",
    "yyyy-MM-ddTHHzz",
    "yyyy-MM-ddTHHZ"
    };


        public async Task<IActionResult> Details(Guid id)
        {

            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            ReferenceGeometry g = await db.Geometries
                .Include(m => m.Records).ThenInclude(u=>u.TextData).ThenInclude(td => td.FormField)
                .Include(m => m.Records).ThenInclude(u => u.NumericData).ThenInclude(td => td.FormField)
                .Include(m => m.Records).ThenInclude(u => u.BooleanData).ThenInclude(td => td.FormField)
                .Include(m => m.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff=>fff.FormField)
                .Include(m => m.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(mo=>mo.PublicMotherFormField)
                .Include(m => m.Records).ThenInclude(u => u.Form).ThenInclude(f => f.FormFormFields).ThenInclude(fff => fff.FormField).ThenInclude(h => h.HiddenFieldChoices)
                .Include(m => m.Records).ThenInclude(u => u.ProjectGroup.Group)
                .Include(m => m.Records).ThenInclude(u => u.RecordChangeLogs).ThenInclude(rcl => rcl.ChangeLog).ThenInclude(cl => cl.User)
                .Where(m => m.GeometryId == id)
                .Where(m => m.StatusId != StatusEnum.deleted).FirstOrDefaultAsync();
            if (g == null) return StatusCode(500);

            List<Project> projects = new List<Project>();
            List<Project> erfassendeProjects = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK));
            }
            if (User.IsInRole("PL"))
            {
                projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
                erfassendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL));
            }

            projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE));
            projects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(erfassendeProjects);

            if (!projects.Any(m=>m.ProjectId == g.ProjectGroupProjectId)) return StatusCode(StatusCodes.Status403Forbidden);
            if (!erfassendeProjects.Any(m=>m.ProjectId == g.ProjectGroupProjectId)) ViewData["ReadOnly"] = true;
            else ViewData["ReadOnly"] = false;

            GeometrieViewModel gvm = new GeometrieViewModel() { Geometry = g };
            gvm.Records = new List<RecordViewModel>();

            List<Group> myGroups;
            if (User.IsInRole("DM")) myGroups = await db.Groups.ToListAsync();
            else if ((User.IsInRole("PK")) || (User.IsInRole("PL")))
            {
                Project p = await db.Projects.Where(m => m.ProjectId == g.ProjectGroupProjectId).Include(m => m.ProjectConfigurator).Include(m => m.ProjectManager).FirstAsync();

                if ((p.ProjectConfigurator.UserId == user.UserId) || (p.ProjectManager.UserId == user.UserId)) myGroups = await db.Groups.ToListAsync();
                else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();
            }
            else myGroups = await db.Groups.Where(m => m.GroupUsers.Any(u => u.UserId == user.UserId)).ToListAsync();

            await RecordsController.CreateDynamicView(db, g, g.ProjectGroup, myGroups, gvm, _generalPluginExtension,user);

            List<string> pluginslist = new List<string>();

            // Are there any plugins for the forms?
            foreach (IReferenceGeometryPlugin rge in _referenceGeometryExtension.Plugins)
            {
                if (g.Records.Any(m => m.FormId == rge.FormId))
                {
                    pluginslist.Add(rge.GetName());
                }
            }

            ViewBag.plugins = pluginslist;

            return View(gvm);
        }

        public async Task<IActionResult> GetGeometriesForProject(Guid? id, string search)
        {
            if (id == null) return NotFound();

            string projectId = HttpContext.Session.GetString("Project");
            if (projectId == null) return Content("ERROR");

            //TODO chb
            //Project myProject = db.Projects.Include(m => m.Geometries).Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefault();
            Project myProject = await db.Projects
                .Include(p => p.ProjectGroups).ThenInclude(m => m.Geometries)
                .Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefaultAsync();

            if (myProject == null) return Content("ERROR");

            List<GeometriePoco> returnlist = new List<GeometriePoco>();
            if (myProject != null)
            {
                if (search == null) search = "";
                foreach (ReferenceGeometry g in myProject.ProjectGroups.SelectMany(pg => pg.Geometries).Where(g => g.StatusId != StatusEnum.deleted))
                {
                    if ((search!=null) && (g.GeometryName!=null) && (g.GeometryName.ToLower().Contains(search.ToLower()))) returnlist.Add(new GeometriePoco() { Id = g.GeometryId, Title = g.GeometryName });
                    else if (search==null) returnlist.Add(new GeometriePoco() { Id = g.GeometryId, Title = g.GeometryName });
                }
            }

            string json = JsonConvert.SerializeObject(returnlist);
            return Content(json, "application/json");
        }

        public async Task<IActionResult> GetPolygonesForProject(Guid? id, string search)
        {
            if (id == null) return NotFound();

            string projectId = HttpContext.Session.GetString("Project");
            if (projectId == null) return Content("ERROR");

            //TODO chb
            //Project myProject = db.Projects.Include(m => m.Geometries).Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefault();
            Project myProject = await db.Projects
                .Include(p => p.ProjectGroups).ThenInclude(m => m.Geometries)
                .Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefaultAsync();

            if (myProject == null) return Content("ERROR");

            if (search == null) search = "";
            List<GeometriePoco> returnlist = new List<GeometriePoco>();
            if (myProject != null)
            {

                foreach (ReferenceGeometry g in myProject.ProjectGroups.SelectMany(pg => pg.Geometries).Where(g => g.StatusId != StatusEnum.deleted && g.Polygon!=null))
                {
                    if ((search != null) && (g.GeometryName != null) && (g.GeometryName.ToLower().Contains(search.ToLower()))) returnlist.Add(new GeometriePoco() { Id = g.GeometryId, Title = g.GeometryName });
                    else if (search == null) returnlist.Add(new GeometriePoco() { Id = g.GeometryId, Title = g.GeometryName });
                }
            }

            string json = JsonConvert.SerializeObject(returnlist);
            return Content(json, "application/json");
        }

        public async Task<IActionResult> GetLinesForProject(Guid? id, string search)
        {
            if (id == null) return NotFound();

            string projectId = HttpContext.Session.GetString("Project");
            if (projectId == null) return Content("ERROR");

            //TODO chb
            //Project myProject = db.Projects.Include(m => m.Geometries).Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefault();
            Project myProject = await db.Projects
                .Include(p => p.ProjectGroups).ThenInclude(m => m.Geometries)
                .Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefaultAsync();

            if (myProject == null) return Content("ERROR");

            List<GeometriePoco> returnlist = new List<GeometriePoco>();
            if (myProject != null)
            {
                if (search == null) search = "";
                foreach (ReferenceGeometry g in myProject.ProjectGroups.SelectMany(pg => pg.Geometries).Where(g => g.StatusId != StatusEnum.deleted && g.Line != null))
                {
                    if ((search != null) && (g.GeometryName != null) && (g.GeometryName.ToLower().Contains(search.ToLower()))) returnlist.Add(new GeometriePoco() { Id = g.GeometryId, Title = g.GeometryName });
                    else if (search == null) returnlist.Add(new GeometriePoco() { Id = g.GeometryId, Title = g.GeometryName });
                }
            }

            string json = JsonConvert.SerializeObject(returnlist);
            return Content(json, "application/json");
        }

        public async Task<IActionResult> SplitMeBabyOneMoreTime(Guid polygonId, Guid lineId)
        {
            await db.Database.ExecuteSqlRawAsync("select splitgeometry('" + lineId + "','" + polygonId + "');");
            return Content("OK");
        }

        #region GeoJson Methods

        public enum GeomType
        {
            Point = 1,
            Line = 2,
            Polygon = 3
        }

        /// <summary>
        /// Creates a GeoJson from all ProjectGeometries in the current project (session has to be set)
        /// </summary>
        /// <returns>GeoJson string</returns>
        private string createGeoJson()
        {
            string projectId = HttpContext.Session.GetString("Project");
            if (projectId == null) return "";

            //TODO chb
            //Project myProject = db.Projects.Include(m => m.Geometries).Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefault();
            Project myProject = db.Projects
                .Include(p=>p.ProjectGroups).ThenInclude(m => m.Geometries)
                .Where(m => m.ProjectId == new Guid(projectId)).FirstOrDefault();

            if (myProject == null) return "";

            if (myProject != null)
            {
                NetTopologySuite.Features.FeatureCollection featureCollection = new NetTopologySuite.Features.FeatureCollection();

                //TODO chb
                //foreach (ReferenceGeometry g in myProject.Geometries.Where(m => m.StatusId != StatusEnum.deleted))
                foreach (ReferenceGeometry g in myProject.ProjectGroups.SelectMany(pg=>pg.Geometries).Where(g => g.StatusId != StatusEnum.deleted))
                {
                    NetTopologySuite.Features.Feature f = null; 
                    if (g.Point != null) f = getFeature(g, GeomType.Point);
                    if (g.Line != null) f = getFeature(g, GeomType.Line);
                    if (g.Polygon != null) f = getFeature(g, GeomType.Polygon);
                    if (f != null) featureCollection.Add(f);

                }


                var jsonSerializer = GeoJsonSerializer.Create();
                var sw = new System.IO.StringWriter();
                jsonSerializer.Serialize(sw, featureCollection);

                return sw.ToString();
            }
            else
            {
                return null;// "No project found";
            }
        }

        private NetTopologySuite.Features.Feature getFeature(ReferenceGeometry g, GeomType type)
        {
            List<Coordinate> coordinatesSwissGrid = new List<Coordinate>();

            Coordinate[] coordinates;
            if (type == GeomType.Point) coordinates = g.Point.Coordinates;
            else if (type == GeomType.Line) coordinates = g.Line.Coordinates;
            else if (type == GeomType.Polygon) coordinates = g.Polygon.Coordinates;
            else coordinates = null;
            foreach (Coordinate coor in coordinates)
            {
                // Convert Coordinates to LV03
                double x = 0, y = 0, h = 0;
                CoordinateConverter.WGS84toLV03(coor.Y, coor.X, 0, ref x, ref y, ref h);

                Coordinate newcoord = new Coordinate(x, y);
                coordinatesSwissGrid.Add(newcoord);
            }
            try
            {
                var gf = new GeometryFactory();
                Geometry polygonSwissGrid = null;
                if (type == GeomType.Point) polygonSwissGrid = gf.CreatePoint(coordinatesSwissGrid[0]);
                else if (type == GeomType.Line) polygonSwissGrid = gf.CreateLineString(coordinatesSwissGrid.ToArray());
                else if (type == GeomType.Polygon) polygonSwissGrid = gf.CreatePolygon(coordinatesSwissGrid.ToArray());

                NetTopologySuite.Features.AttributesTable attribute = new NetTopologySuite.Features.AttributesTable();
                attribute.Add("id", g.GeometryId);
                attribute.Add("name", g.GeometryName);

                /*if (coordinates.Count() > 20)
                {
                    Geometry simplG = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(polygonSwissGrid, 1);
                    polygonSwissGrid = simplG;
                }*/

                if (!polygonSwissGrid.IsValid) return null;

                NetTopologySuite.Features.Feature i = new NetTopologySuite.Features.Feature(polygonSwissGrid, attribute);
                
                return i;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        #endregion

    }

    public class GeometriePoco
    {
        public string Title { get; set; }
        public Guid Id { get; set; }
    }

    public class GeometrieViewModel
    {
        public ReferenceGeometry Geometry { get; set; }
        public List<RecordViewModel> Records { get; set; }
    }

}
