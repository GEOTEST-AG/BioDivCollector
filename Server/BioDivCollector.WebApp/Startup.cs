using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using BioDivCollector.WebApp.Helpers;
using McMaster.NETCore.Plugins;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;


namespace BioDivCollector.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mvcBuilder = services.AddRazorPages().AddRazorRuntimeCompilation()
        .AddMvcOptions(options => options.Filters.Add(new AuthorizeFilter()));


            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //var embeddedFileProvider = new EmbeddedFileProvider(typeof(FormFactory.FF).GetTypeInfo().Assembly, nameof(FormFactory));
            //Add the file provider to the Razor view engine
            services.Configure<Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.MvcRazorRuntimeCompilationOptions>(options =>
            {
                //options.FileProviders.Add(embeddedFileProvider);
            });
            IdentityModelEventSource.ShowPII = true;
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options => {
                options.Cookie.Name = "access_token";
                options.Events = new CookieAuthenticationEvents()
                {
                    OnSigningIn = (context) =>
                    {
                        ClaimsIdentity identity = (ClaimsIdentity)context.Principal.Identity;

                        // Check if User is already in DB
                        using (DB.Models.Domain.BioDivContext db = new DB.Models.Domain.BioDivContext())
                        {
                            var userid = identity.FindFirst("preferred_username");
                            User u = db.Users.Find(userid.Value);
                            if (u==null)
                            {
                                u = new User();

                                u.Name = identity.FindFirst(ClaimTypes.Surname).Value;
                                u.FirstName = identity.FindFirst(ClaimTypes.GivenName).Value;
                                u.Email = identity.FindFirst(ClaimTypes.Email).Value;
                                u.UserId = userid.Value;

                                u.Status = db.Statuses.Where(m => m.Id == StatusEnum.unchanged).FirstOrDefault();
                                 
                            
                                db.Users.Add(u);
                                db.SaveChanges();

                                // Send Mails to all DM's
                                Controllers.UsersController usersController = new Controllers.UsersController(Configuration);
                                var task = usersController.SendNewUserMailToDM(u);
                                task.Wait();

                            }

                        }

                        context.Principal = new ClaimsPrincipal(identity);

                        return Task.FromResult(0);
                    },
                    OnValidatePrincipal = context =>
                    {
                        return OnValidatePrincipal(context);
                    }
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = Configuration["JWT:Url"]+"/auth/realms/"+ Configuration["JWT:Realm"];
                options.RequireHttpsMetadata = true;
                options.ClientId = "BioDivCollector";
                options.ClientSecret = Configuration["JWT:Key"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.RequireHttpsMetadata = false;
                options.Events = new OpenIdConnectEvents // required for single sign out
                {
                    OnRedirectToIdentityProviderForSignOut = async (context) => context.ProtocolMessage.IdTokenHint = await context.HttpContext.GetTokenAsync("id_token")
                };




            });

            //services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            services.AddTransient<IAppVersionService, AppVersionService>();

            services.AddDistributedMemoryCache();
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(60);   
            });

            services.AddAuthorization();

            services.AddControllersWithViews();
            services.AddTransient<BioDivContext>();


            // Plugins loading
            var loaders = new List<PluginLoader>();

            foreach (var dir in Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "plugins")))
            {
                var pluginFile = Path.Combine(dir, Path.GetFileName(dir) + ".dll");
                if (File.Exists(pluginFile))
                {
                    var loader = PluginLoader.CreateFromAssemblyFile(
                        pluginFile,
                        sharedTypes: new[] { typeof(IPlugin) });
                    loaders.Add(loader);
                    mvcBuilder.AddPluginLoader(loader);
                }
                
            }

            // Load all ReferenceGeoemtryExtensions
            List<IReferenceGeometryPlugin> referenceGeometryPlugins = new List<IReferenceGeometryPlugin>();
            List<IPlugin> generalPlugins = new List<IPlugin>();
            foreach (var loader in loaders)
            {
                foreach (var pluginType in loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .Where(t => typeof(IReferenceGeometryPlugin).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    // This assumes the implementation of IPlugin has a parameterless constructor
                    var plugin = Activator.CreateInstance(pluginType) as IReferenceGeometryPlugin;
                    referenceGeometryPlugins.Add(plugin);
                }

                foreach (var pluginType in loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    // This assumes the implementation of IPlugin has a parameterless constructor
                    //var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                    //generalPlugins.Add(plugin);
                }
            }
            services.Add(new ServiceDescriptor(typeof(ReferenceGeometryExtension), new ReferenceGeometryExtension(referenceGeometryPlugins)));


        }

        private async Task OnValidatePrincipal(CookieValidatePrincipalContext context)
        {
            const string accessTokenName = "access_token";
            const string refreshTokenName = "refresh_token";
            const string expirationTokenName = "expires_at";

            if (context.Principal.Identity.IsAuthenticated)
            {
                var exp = context.Properties.GetTokenValue(expirationTokenName);
                if (exp != null)
                {
                    var expires = DateTime.Parse(exp, CultureInfo.InvariantCulture).ToUniversalTime();
                    if (expires < DateTime.UtcNow)
                    {
                        // If we don't have the refresh token, then check if this client has set the
                        // "AllowOfflineAccess" property set in Identity Server and if we have requested
                        // the "OpenIdConnectScope.OfflineAccess" scope when requesting an access token.
                        var refreshToken = context.Properties.GetTokenValue(refreshTokenName);
                        if (refreshToken == null)
                        {
                            context.RejectPrincipal();
                            return;
                        }

                        var cancellationToken = context.HttpContext.RequestAborted;

                        var client = new RestClient(Configuration["JWT:Admin-Token-Url"]);
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                        request.AddParameter("client_id", Configuration["JWT:Client"]);
                        request.AddParameter("grant_type", "refresh_token");
                        request.AddParameter("refresh_token", refreshToken);
                        request.AddParameter("client_secret", Configuration["JWT:Key"]);
                        IRestResponse response = client.Execute(request);

                        dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                        try
                        {
                            string newAccessToken = json.access_token.Value;

                            // Update the tokens
                            long exx = json.expires_in.Value;

                            var expirationValue = DateTime.UtcNow.AddSeconds(exx).ToString("o", CultureInfo.InvariantCulture);
                            context.Properties.StoreTokens(new[]
                            {
                                new AuthenticationToken { Name = refreshTokenName, Value = json.refresh_token.Value },
                                new AuthenticationToken { Name = accessTokenName, Value = json.access_token.Value },
                                new AuthenticationToken { Name = expirationTokenName, Value = expirationValue }
                            });

                            // Update the cookie with the new tokens
                            context.ShouldRenew = true;

                        }
                        catch (Exception e)
                        {
                            // Refresh token invalid. Active Session is not valid anymore...
                            context.RejectPrincipal();
                            return;

                        }

                        
                    }
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();

            /*var supportedCultures = new[] { new CultureInfo("de-CH") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("de-CH"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
            */
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".apk"] = "application/vnd.android.package-archive";

            

            app.UseStaticFiles();
            {
                var options = new StaticFileOptions
                {
                    ContentTypeProvider = provider,
                    RequestPath = "",
                    //FileProvider = new EmbeddedFileProvider(typeof(FormFactory.FF).GetTypeInfo().Assembly, nameof(FormFactory))
                };

                app.UseStaticFiles(options);
            }

            app.UseRouting();
            app.UseSession();

            app.UseAuthentication();
            

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "MapImageProxy",
                    pattern: "MapImageProxy/{Layer}/{TileMatrix}/{TileCol}/{TileRow}",
                    defaults: new { controller = "MapImageProxy", action = "GetProxyImage" }
                    );
                endpoints.MapControllerRoute(
                    name: "GeoServerProxy",
                    pattern: "proxy/{*path}",
                    defaults: new { controller = "GeoserverProxy", action = "Http" }
                    );
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            string baseDir = env.ContentRootPath;
            AppDomain.CurrentDomain.SetData("DataDirectory", System.IO.Path.Combine(baseDir, "App_Data"));

        }


    }

    public class ClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
          ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
            var realmAccessClaim3 = claimsIdentity.FindFirst((claim) => claim.Type == "resource_access");
            var realmAccessClaim2 = claimsIdentity.FindFirst((claim) => claim.Type == "roles");

            // flatten realm_access because Microsoft identity model doesn't support nested claims
            // by map it to Microsoft identity model, because automatic JWT bearer token mapping already processed here
            if (claimsIdentity.IsAuthenticated && claimsIdentity.HasClaim((claim) => claim.Type == "resource_access"))
            {
                var realmAccessClaim = claimsIdentity.FindFirst((claim) => claim.Type == "resource_access");

                //var realmAccessClaim2 = realmAccessClaim.FindFirst((claim) => claim.Type == "realm_access");
                var realmAccessAsDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(realmAccessClaim.Value);
                if (realmAccessAsDict["roles"] != null)
                {
                    foreach (var role in realmAccessAsDict["roles"])
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }

            return Task.FromResult(principal);
        }
    }
}
// routes.MapRoute("MapImageProxy", "MapImageProxy/{Layer}/{TileMatrix}/{TileCol}/{TileRow}", new { controller = "MapImageProxy", action = "GetProxyImage" });