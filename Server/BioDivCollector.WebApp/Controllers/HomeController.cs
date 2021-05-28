using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BioDivCollector.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using BioDivCollector.DB.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Xml;
using System.Text;

namespace BioDivCollector.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private BioDivContext db = new BioDivContext();

        public IConfiguration Configuration { get; }

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            int projects = db.Projects.Where(m=>m.StatusId!=StatusEnum.deleted).ToList().Count();
            int records = db.Records.Where(m => m.StatusId != StatusEnum.deleted).ToList().Count();
            int users = db.Users.Where(m => m.StatusId != StatusEnum.deleted).ToList().Count();
            ViewData["projects"] = projects;
            if (projects > 10) ViewData["projects"] = (((int)projects) / 10) * 10;
            if (projects > 50) ViewData["projects"] = (((int)projects) / 50) * 50;
            if (projects > 100) ViewData["projects"] = (((int)projects) / 100) * 100; 
            
            ViewData["records"] = records;
            if (records > 10) ViewData["records"] = (((int)records) / 10) * 10;
            if (records > 50) ViewData["records"] = (((int)records) / 50) * 50;
            if (records > 100) ViewData["records"] = (((int)records) / 100) * 100;

            ViewData["users"] = users;
            if (users > 10) ViewData["users"] = (((int)users) / 10) * 10;
            if (users > 50) ViewData["users"] = (((int)users) / 50) * 50;
            if (users > 100) ViewData["users"] = (((int)users) / 100) * 100;

            // Get Announcements from Jira Service Desk
            try
            {
                var client = new RestClient(Configuration["Environment:JiraServiceDeskUrl"]);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", "Basic " + Configuration["Environment:JiraServiceDeskAuth"]);
                IRestResponse response = client.Execute(request);

                dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                string header = json.header.Value;
                string message = json.message.Value;
                ViewBag.AnnouncementHeader = header;
                ViewBag.AnnouncementMessage = message;
            }
            catch (Exception e)
            {
                // There is no active announcement, that's absolutely ok
            }


            ViewBag.registerLink = Configuration["Jwt:Url"] + "/auth/realms/" + Configuration["Jwt:Realm"];

            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            User user = Helpers.UserHelper.GetCurrentUser(User, db);

            List<Project> projects = new List<Project>();
            List<Project> erfassendeProjects = new List<Project>();
            List<Project> editProjectSetting = new List<Project>();
            if (User.IsInRole("DM"))
            {
                projects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.DM);
                erfassendeProjects = projects;
            }
            else if (User.IsInRole("EF")) erfassendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.EF);
            if (User.IsInRole("PK"))
            {
                List<Project> pkprojects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PK);
                projects.AddRange(pkprojects);
                erfassendeProjects.AddRange(pkprojects);
            }
            if (User.IsInRole("PL"))
            {
                List<Project> plprojects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.PL);
                projects.AddRange(plprojects);
                erfassendeProjects.AddRange(plprojects);
            }
            editProjectSetting = projects;
            List<Project> nurLesendeProjects = await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE);
            nurLesendeProjects.AddRange(await DB.Helpers.ProjectManager.UserProjectsAsync(db, user, RoleEnum.LE_OGD));
            projects.AddRange(nurLesendeProjects);
            projects.AddRange(erfassendeProjects);



            List<ProjectPocoForIndex> newProjectList = new List<ProjectPocoForIndex>();

            foreach (Project p in projects.Distinct())
            {
                Project addProject = await db.Projects
                    .Include(m => m.Status)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Geometries)
                    .Include(m => m.ProjectGroups).ThenInclude(u => u.Group).ThenInclude(g => g.GroupUsers).ThenInclude(gu => gu.User)
                    .Include(m => m.ProjectStatus)
                    .Include(m => m.ProjectManager)
                    .Include(m => m.ProjectConfigurator)
                    .Where(m => m.Status.Id != StatusEnum.deleted && m.ProjectId == p.ProjectId).FirstAsync();

                ProjectPocoForIndex pp = new ProjectPocoForIndex() { Project = addProject };
                if (!erfassendeProjects.Contains(addProject)) pp.IsReadOnly = true;
                else pp.IsReadOnly = false;

                if (editProjectSetting.Contains(addProject)) pp.IsPKOrPLOrDM = true;
                newProjectList.Add(pp);

            }
            ViewData["Username"] = user.UserId;

            return View(newProjectList);
        }

        /// <summary>
        /// Get the confluence FAQ Site Content and show it in BDC Themed View
        /// </summary>
        /// <returns></returns>
        public IActionResult FAQ()
        {
            var client = new RestClient("https://biodivcollector.atlassian.net/wiki/rest/api/content/524294?expand=body.storage");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Basic Y2hyaXN0b3BoLnN1dGVyQGdlb3Rlc3QuY2g6TFZQRXcxZThZTmcxOG5PVmlOTmYxQzlF");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                try
                {
                    StringBuilder sbXML = new StringBuilder();
                    sbXML.Append("<root>");
                    sbXML.Append("<messageBody />");
                    sbXML.Append("</root>");

                    XmlDocument xmlDOC = new XmlDocument();
                    xmlDOC.LoadXml(sbXML.ToString());
                    xmlDOC.DocumentElement.SelectSingleNode("messageBody").InnerText = json.body.storage.value;


                    string content = "<root>"+json.body.storage.value+"</root>";
                    content = content.Replace("ac:", "");
                    string output = "";
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(content);
                    // TODO Interpretate the awful confluence xml and transfer it to nice html
                    foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (node.Name== "ac:structured-macro")
                        {

                        }
                        else output += node.InnerText; //or loop through its children as well

                    }
                    return Content(content);
                }
                catch (Exception e)
                {
                    // Refresh token invalid. Active Session is not valid anymore...
                    return View((object)"Konnte FAQ nicht laden");

                }
            }
            return View((object)"Konnte FAQ nicht laden");

        }
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, ErrorMessage = context.Error.Message });
        }

        public IActionResult NotAllowed()
        {
            return View();
        }

        [Authorize]
        public IActionResult Login() => View("Index");

        public async Task Logout()
        {

            await MyCustomSignOut("/Home/Index");
            //await HttpContext.SignOutAsync("Cookies");
            //await HttpContext.SignOutAsync("OpenIdConnect");
        }

        public async Task MyCustomSignOut(string redirectUri)
        {
            // inject the HttpContextAccessor to get "context"
            await HttpContext.SignOutAsync("Cookies");
            var prop = new AuthenticationProperties()
            {
                RedirectUri = redirectUri
            };
            // after signout this will redirect to your provided target
            await HttpContext.SignOutAsync("OpenIdConnect", prop);
        }

    }
}
