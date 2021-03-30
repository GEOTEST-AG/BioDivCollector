using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Claims;
using System.Text.Json.Serialization;
using BioDivCollector.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace BioDivCollector.WebApp.Controllers
{
    public class UsersController : Controller
    {
        BioDivContext db = new BioDivContext();
        public IConfiguration Configuration { get; }


        public UsersController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("DM")) return RedirectToAction("Edit");
            List<UserPoco> users = await GetAllUsers();
            return View(users);
        }


        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            User me = UserHelper.GetCurrentUser(User, db);
            if (!User.IsInRole("DM")) id = me.UserId;
            else if (id == null) id = me.UserId;

            if (id == null)
            {
                return NotFound();
            }

            var edituser = await db.Users.FindAsync(id);

            List<UserPoco> ups = await GetAllUsers();
            if (edituser == null)
            {
                if (ups.Where(m=>m.username == id).Any())
                {
                    UserPoco up = ups.Where(m => m.username == id).First();
                    edituser = new User();

                    edituser.Name = up.lastName;
                    edituser.FirstName = up.firstName;
                    edituser.Email = up.email;
                    edituser.UserId = up.username;

                    edituser.Status = db.Statuses.Where(m => m.Id == StatusEnum.unchanged).FirstOrDefault();

                    db.Entry(edituser).State = EntityState.Added;
                    db.Users.Add(edituser);
                    await db.SaveChangesAsync();

                }
            }
            
            EditUserViewModel euvm = new EditUserViewModel() { UserId = edituser.UserId, Email = edituser.Email, FirstName = edituser.FirstName, Name = edituser.Name, enabled = ups.Where(m => m.username == edituser.UserId).FirstOrDefault().enabled, roles = ups.Where(m => m.username == edituser.UserId).FirstOrDefault().roles };

            return View(euvm);
        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string? id, [Bind("UserId, FirstName, Name, Email, enabled")] EditUserViewModel edituserVM)
        {
            User me = UserHelper.GetCurrentUser(User, db);
            if ((!User.IsInRole("DM")) && (me.UserId != edituserVM.UserId)) return RedirectToAction("NotAllowed", "Home");
            if (!User.IsInRole("DM")) id = me.UserId;
            if (id != edituserVM.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                try
                {
                    User oldUser = await db.Users.FindAsync(edituserVM.UserId);
                    oldUser.FirstName = edituserVM.FirstName;
                    oldUser.Name = edituserVM.Name;
                    oldUser.Email = edituserVM.Email;

                    db.Update(oldUser);
                    await db.SaveChangesAsync();


                    // change it in keycloak
                    List<UserPoco> ups = await GetAllUsers(false);
                    string access_token = GetAdminAccessToken();
                    if (access_token != "Error")
                    {
                        var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users/" + ups.Where(m => m.username == edituserVM.UserId).FirstOrDefault().id);
                        client.Timeout = -1;
                        var request = new RestRequest(Method.PUT);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\"firstName\": \"" + oldUser.FirstName + "\",\r\n  \"lastName\": \"" + oldUser.Name + "\",\r\n  \"email\": \"" + oldUser.Email + "\", \"enabled\": \"" + edituserVM.enabled + "\"}", ParameterType.RequestBody);
                        request.AddHeader("Authorization", "Bearer " + access_token);
                        IRestResponse response = client.Execute(request);

                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(edituserVM);
        }

        public async Task<IActionResult> AddRoleToUser(string UserId, string Role)
        {

            var edituser = await db.Users.FindAsync(UserId);
            List<UserPoco> ups = await GetAllUsers(false);
            List<RolesPoco> roles = GetAllRoles();

            string access_token = GetAdminAccessToken();
            if (access_token != "Error")
            {
                var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users/" + ups.Where(m => m.username == edituser.UserId).FirstOrDefault().id + "/role-mappings/clients/ebe1c5ca-02f1-4ce1-b5a2-a39d7b9c83c4");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", "[{\"id\": \"" + roles.Where(m => m.name == Role).First().id + "\", \"name\": \"" + roles.Where(m => m.name == Role).First().name + "\"}]", ParameterType.RequestBody);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);

            }

            return Content("OK");

        }


        public async Task<IActionResult> RemoveRoleFromUser(string UserId, string Role)
        {
            User me = UserHelper.GetCurrentUser(User, db);
            if (!User.IsInRole("DM"))
            {
                return RedirectToAction("NotAllowed", "Home");
            }
            var edituser = await db.Users.FindAsync(UserId);
            List<UserPoco> ups = await GetAllUsers(false);
            List<RolesPoco> roles = GetAllRoles();

            string access_token = GetAdminAccessToken();
            if (access_token != "Error")
            {
                var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users/" + ups.Where(m => m.username == edituser.UserId).FirstOrDefault().id + "/role-mappings/clients/ebe1c5ca-02f1-4ce1-b5a2-a39d7b9c83c4");
                client.Timeout = -1;
                var request = new RestRequest(Method.DELETE);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", "[{\"id\": \"" + roles.Where(m => m.name == Role).First().id + "\", \"name\": \"" + roles.Where(m => m.name == Role).First().name + "\"}]", ParameterType.RequestBody);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);

            }
            return Content("OK");
        }


        public async Task<IActionResult> GetUsers()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users/");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + accessToken);
            IRestResponse response = client.Execute(request);

            return Content(response.Content);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "DM")]
        public IActionResult CreateUser()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("username, firstName, lastName, email")] NewUser newUser)
        {
            if (ModelState.IsValid)
            {

                var accessToken = await HttpContext.GetTokenAsync("access_token");

                string initPassword = generate_password(7);

                var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users/");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", "{\"username\": \"" + newUser.username + "\",\r\n  \"enabled\": true,\r\n  \"emailVerified\": false,\r\n  \"firstName\": \"" + newUser.firstName + "\",\r\n  \"lastName\": \"" + newUser.lastName + "\",\r\n  \"email\": \"" + newUser.email + "\",\r\n  \"attributes\": {\"Firma\": [\"\"]},\r\n \"credentials\":[{\"type\":\"password\",\"value\":\"" + initPassword + "\",\"temporary\":true}],\r\n  \"requiredActions\": [\"VERIFY_EMAIL\",\"UPDATE_PROFILE\",\"UPDATE_PASSWORD\"]}", ParameterType.RequestBody);
                request.AddHeader("Authorization", "Bearer " + accessToken);
                IRestResponse response = client.Execute(request);

                // User created
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {

                    User newU = new User();
                    newU.FirstName = newUser.firstName;
                    newU.Name = newUser.lastName;
                    newU.Email = newUser.email;
                    newU.UserId = newUser.username;

                    newU.Status = db.Statuses.Where(m => m.Id == StatusEnum.unchanged).FirstOrDefault();

                    db.Users.Add(newU);
                    await db.SaveChangesAsync();


                    try
                    {
                        newUser.initial_password = initPassword;

                        MailMessage mail = new MailMessage();
                        SmtpClient SmtpServer = new SmtpClient("localhost");

                        mail.From = new MailAddress("registration@biodivcollector.ch", "BioDivCollector");
                        mail.To.Add(newUser.email);
                        mail.Subject = "Zugangsdaten zu biodivcollector.ch";
                        mail.Body = "Hallo " + newUser.firstName + " " + newUser.lastName + "\n\nEs wurde ein Zugang auf https://www.biodivcollector.ch erstellt. Du kannst dich dort mit folgenden Zugangsdaten einloggen: \n\nBenutzername: " + newUser.username + "\nInitial-Passwort: " + initPassword + "\n\nWir freuen uns auf deine erste Anmeldung. Viel Spass.";

                        SmtpServer.Port = 25;
                        SmtpServer.Credentials = new System.Net.NetworkCredential(Configuration["Environment:RegisterEmail"], Configuration["Environment:RegisterEmailPassword"]);

                        SmtpServer.Send(mail);
                    }
                    catch (Exception e)
                    {
                        newUser.error = "Konnte Mail nicht verschicken: " + e.Message;
                    }

                }
                else if (response.Content.Contains("User exists")) newUser.error = "Benutzer exisistiert bereits (entweder Mailadresse oder Benutzername)";
                else newUser.error = response.StatusDescription + "(" + response.Content + ")";


            }
            return View(newUser);
        }


        #region helperMethods

        public string GetAdminAccessToken()
        {
            var client = new RestClient(Configuration["Jwt:Url"] + "/auth/realms/" + Configuration["Jwt:Realm"] + "/protocol/openid-connect/token");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", "admin-cli");
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_secret", Configuration["Jwt:Admin-Key"]);
            request.AddParameter("scope", "openid");
            request.AddParameter("username", Configuration["Jwt:Admin-User"]);
            request.AddParameter("password", Configuration["Jwt:Admin-Password"]);
            IRestResponse response = client.Execute(request);
            dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
            try
            {
                string newAccessToken = json.access_token.Value;

                return newAccessToken;
            }
            catch (Exception e)
            {
                // Refresh token invalid. Active Session is not valid anymore...
                return "Error";

            }
        }

        public List<string> GetAllUsersByRole(string Role)
        {
            string access_token = GetAdminAccessToken();
            if (access_token != "Error")
            {
                var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/clients/ebe1c5ca-02f1-4ce1-b5a2-a39d7b9c83c4/roles/" + Role + "/users");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);

                dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                List<string> returnList = new List<string>();

                try
                {
                    foreach (dynamic user in json)
                    {
                        returnList.Add(user.username.Value);
                    }

                    return returnList;
                }
                catch (Exception e)
                {
                    // Refresh token invalid. Active Session is not valid anymore...
                    return null;

                }
            }
            return null;
        }


        public async Task<List<UserPoco>> GetAllUsers(bool withDBUser = true)
        {
            string access_token = GetAdminAccessToken();
            if (access_token != "Error")
            {
                var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);

                dynamic json = Newtonsoft.Json.Linq.JArray.Parse(response.Content);

                List<UserPoco> returnList = new List<UserPoco>();
                List<User> dbusers = await db.Users
                    .Include(m => m.UserGroups).ThenInclude(u => u.Group).ThenInclude(gr => gr.GroupProjects).ThenInclude(gp => gp.Project)
                    .ToListAsync();
                //try
                {
                    foreach (dynamic user in json)
                    {
                        UserPoco up = new UserPoco();

                        up.id = user.id.ToString();
                        if (user.email != null) up.email = user.email.ToString();
                        if (user.firstName != null) up.firstName = user.firstName.ToString();
                        if (user.lastName != null) up.lastName = user.lastName.ToString();
                        if (user.username != null) up.username = user.username.ToString();
                        if (user.enabled != null) up.enabled = (bool)user.enabled;

                        // search for user in db
                        if (withDBUser)
                        {
                            up.dbUser = dbusers.Where(m => m.UserId == up.username).FirstOrDefault();
                            if (up.dbUser != null)
                            {
                                List<UserProjectPoco> upps = new List<UserProjectPoco>();
                                foreach (GroupUser gu in up.dbUser.UserGroups)
                                {
                                    foreach (ProjectGroup gp in gu.Group.GroupProjects)
                                    {
                                        if (gp.ReadOnly)
                                        {
                                            UserProjectPoco upp = new UserProjectPoco() { Project = gp.Project, Role = "LE" };
                                            upps.Add(upp);
                                        }
                                        else
                                        {
                                            UserProjectPoco upp = new UserProjectPoco() { Project = gp.Project, Role = "EF" };
                                            upps.Add(upp);
                                        }
                                    }
                                }

                                List<Project> pkprojects = await db.Projects.Where(m => m.ProjectConfigurator.UserId == up.dbUser.UserId).ToListAsync();
                                foreach (Project p in pkprojects)
                                {
                                    UserProjectPoco upp = new UserProjectPoco() { Project = p, Role = "PK" };
                                    upps.Add(upp);
                                }

                                List<Project> plprojects = await db.Projects.Where(m => m.ProjectManager.UserId == up.dbUser.UserId).ToListAsync();
                                foreach (Project p in pkprojects)
                                {
                                    UserProjectPoco upp = new UserProjectPoco() { Project = p, Role = "PL" };
                                    upps.Add(upp);
                                }

                                List<UserProjectPoco> distinctUpp = new List<UserProjectPoco>();
                                foreach (UserProjectPoco upp in upps)
                                {
                                    if (!distinctUpp.Any(m => m.Project == upp.Project)) distinctUpp.Add(upp);
                                    else
                                    {
                                        distinctUpp.Where(m => m.Project == upp.Project).First().Role += ", " + upp.Role;
                                    }
                                }

                                up.projects = distinctUpp;

                            }
                        }

                        // search for client Roles
                        var clientRoles = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/users/" + up.id + "/role-mappings/clients/ebe1c5ca-02f1-4ce1-b5a2-a39d7b9c83c4");
                        clientRoles.Timeout = -1;
                        var request2 = new RestRequest(Method.GET);
                        request2.AddHeader("Authorization", "Bearer " + access_token);
                        IRestResponse response2 = clientRoles.Execute(request2);

                        dynamic roles = Newtonsoft.Json.Linq.JArray.Parse(response2.Content);
                        up.roles = new List<string>();
                        foreach (dynamic role in roles)
                        {
                            up.roles.Add(role.description.ToString());
                        }


                        returnList.Add(up);
                    }

                    return returnList;
                }
                /*catch (Exception e)
                {
                    // Refresh token invalid. Active Session is not valid anymore...
                    return null;

                }*/
            }
            return null;
        }


        public List<RolesPoco> GetAllRoles()
        {
            string access_token = GetAdminAccessToken();
            if (access_token != "Error")
            {
                var client = new RestClient(Configuration["Jwt:Url"] + "/auth/admin/realms/" + Configuration["Jwt:Realm"] + "/clients/ebe1c5ca-02f1-4ce1-b5a2-a39d7b9c83c4/roles");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);

                dynamic json = Newtonsoft.Json.Linq.JArray.Parse(response.Content);

                List<RolesPoco> returnList = new List<RolesPoco>();
                //try
                {
                    foreach (dynamic role in json)
                    {
                        RolesPoco rp = new RolesPoco();

                        rp.id = role.id.ToString();
                        rp.name = role.name?.ToString();
                        rp.description = role.description?.ToString();

                        returnList.Add(rp);
                    }

                    return returnList;
                }

            }
            return null;
        }

        /// <summary>
        /// generates the initial password. Code from http://www.ne555.at/2014/index.php/pc-programmierung/einfuehrung-c/286-passwortgenerator.html
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        private string generate_password(int lang)
        {
            string pw = ""; // "" = leerer String
            int zeichen, n = 0;
            Random zufall = new Random();

            do
            {
                zeichen = (zufall.Next(48, 123));
                if ((zeichen >= 48 && zeichen <= 57) || (zeichen > 97 && zeichen <= 122))
                //ASCII-Code 97 bis 122 sind Kleinbuchstaben, 48 bis 57 sind Ziffern
                {
                    pw = pw + (char)(zeichen);
                    //Zufallswert (pw) wird durch voranstellen von (char) in char,(Zeichen) konvertiert.
                    n++;
                }
            } while (n < lang);
            return pw;
        }

        public string RenewTokens(string refreshToken)
        {

            var client = new RestClient(Configuration["Jwt:Url"] + "/auth/realms/" + Configuration["Jwt:Realm"] + "/protocol/openid-connect/token");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", "BioDivCollector");
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refreshToken);
            request.AddParameter("client_secret", "33ce9f89-22a9-48ee-b1df-d2d19fc4ec4c");
            IRestResponse response = client.Execute(request);

            dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
            try
            {
                string newAccessToken = json.access_token.Value;

                return newAccessToken;
            }
            catch (Exception e)
            {
                // Refresh token invalid. Active Session is not valid anymore...
                return "Error";

            }


        }
        #endregion

    }

    public class EditUserViewModel
    {
        public string UserId { get; set; }

        public string Name { get; set; }
        public string FirstName { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public bool enabled { get; set; }

        public List<string> roles { get; set; }
    }

    public class RolesPoco
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class UserPoco
    {
        public string id { get; set; }
        [DisplayName("Username")]
        public string username { get; set; }
        [DisplayName("Vorname")]
        public string firstName { get; set; }
        [DisplayName("Nachname")]
        public string lastName { get; set; }
        [EmailAddress]
        [DisplayName("Email")]
        public string email { get; set; }
        [DisplayName("Aktiv (kann sich einloggen)")]
        public bool enabled { get; set; }

        public List<string> roles { get; set; }

        public User dbUser { get; set; }

        public List<UserProjectPoco> projects { get; set; }
    }

    public class UserProjectPoco
    {
        public Project Project { get; set; }
        public string Role { get; set; }
    }

    public class NewUser
    {
        [DisplayName("Username")]
        public string username { get; set; }
        [DisplayName("Vorname")]
        public string firstName { get; set;  }
        [DisplayName("Nachname")]
        public string lastName { get; set; }
        [EmailAddress]
        [DisplayName("Email")]
        public string email { get; set; }
        [DisplayName("Firma")]
        public string firma { get; set; }
        public string initial_password { get; set; }
        public string error { get; set; }
    }

}
