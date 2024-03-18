using BioDivCollector.Connector.Services;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using BioDivCollector.PluginContract.Helpers;
using McMaster.NETCore.Plugins;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;

namespace BioDivCollector.Connector
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
            //services.AddControllers();
            services.AddControllers()
                .AddNewtonsoftJson(
                    options =>
                        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    );

            services.AddAuthorization();

            services.AddDbContext<BioDivContext>();
            services.AddTransient<IStorageService, LocalStorageService>();
            services.AddMvc().AddControllersAsServices();

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });

            //string openidConfigUrl = $"https://id.geotest.ch/auth/realms/BioDivCollector/.well-known/openid-configuration";    //and Configuration["JWT:Issuer"] https://id.geotest.ch/auth/realms/BioDivCollector
            string openidConfigUrl = Configuration["JWT:Url"] + "/auth/realms/" + Configuration["JWT:Realm"] + "/.well-known/openid-configuration";

            //Check crucial URLs
            List<string> testUrls = new List<string>() { openidConfigUrl, Configuration["JWT:Issuer"] };
            using (var httpClient = new HttpClient())
            {
                foreach (string testUrl in testUrls)
                {
                    int currentRetry = 0;
                    while (true)
                    {
                        try
                        {
                            var result = (httpClient.GetAsync(testUrl)).Result;
                            if (result.IsSuccessStatusCode)
                            {
                                break;
                            }
                            else
                            {
                                currentRetry++;
                            }
                            if (currentRetry > 10)
                            {
                                throw new Exception(testUrl + " not found");
                            }
                        }
                        catch (Exception ex)
                        {
                            currentRetry++;

                            if (currentRetry > 10)
                            {
                                //logger.LogError($"URL CHECK ON STARTUP: {testUrl} failed after 10 retries.");
                                Debug.WriteLine($"URL CHECK ON STARTUP: {testUrl} failed after 10 retries.");
                                throw ex;
                            }
                        }
                        int retrySeconds = 5 * currentRetry * currentRetry;
                        //logger.LogWarning($"URL CHECK ON STARTUP: {testUrl} failed. Retry in {retrySeconds}s...");
                        Debug.WriteLine($"URL CHECK ON STARTUP: {testUrl} failed. Retry in {retrySeconds}s...");
                        Thread.Sleep(TimeSpan.FromSeconds(retrySeconds));
                    }
                }
            }

            //https://developer.okta.com/blog/2018/03/23/token-authentication-aspnetcore-complete-guide
            //https://jasonwatmore.com/post/2019/10/16/aspnet-core-3-role-based-authorization-tutorial-with-example-api

            var openidConfiguration = OpenIdConnectConfigurationRetriever.GetAsync(openidConfigUrl, CancellationToken.None);
            SecurityKey signingKey = openidConfiguration.Result.SigningKeys.First();

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true; //<<<<<<<<<<<<<<<<<<<<<<< check
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Clock skew compensates for server time drift.
                    // We recommend 5 minutes or less:
                    ClockSkew = TimeSpan.FromMinutes(5),
                    // Specify the key used to sign the token:
                    IssuerSigningKey = signingKey,
                    ValidateIssuerSigningKey = true,                                    //needed?
                    RequireSignedTokens = true,
                    // Ensure the token hasn't expired:
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    // Ensure the token audience matches our audience value (default true):
                    ValidateAudience = true,
                    ValidAudience = Configuration["JWT:Audience"],
                    // Ensure the token was issued by a trusted authorization server (default true):
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["JWT:Issuer"]
                };
            });

            //https://stackoverflow.com/a/62864495/8024533
            //https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-3.1&tabs=visual-studio
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "BDC Connector API",
                    Description = "BioDivCollector Connector Documentation",
                    Contact = new OpenApiContact
                    {
                        Name = "Christian Baumann",
                        Email = "christian.baumann@geotest.ch",
                        Url = new Uri("https://www.geotest.ch")
                    },
                    //License = new OpenApiLicense
                    //{
                    //    Name = "MIT",
                    //    Url = new Uri("")
                    //}
                });

                s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                s.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                s.IncludeXmlComments(xmlPath);
            });

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
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !typeof(IReferenceGeometryPlugin).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    // This assumes the implementation of IPlugin has a parameterless constructor
                    try
                    {
                        var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                        generalPlugins.Add(plugin);
                    }
                    catch (Exception e)
                    { }
                }
            }
            services.Add(new ServiceDescriptor(typeof(ReferenceGeometryExtension), new ReferenceGeometryExtension(referenceGeometryPlugins)));
            services.Add(new ServiceDescriptor(typeof(GeneralPluginExtension), new GeneralPluginExtension(generalPlugins)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            // Config logging to file, using serilog
            //default outputTemplate = "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}"
            var path = AppDomain.CurrentDomain.BaseDirectory;
            loggerFactory.AddFile($"{path}\\Logs\\Log.txt",
                outputTemplate: "{Timestamp:o} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}");

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "BioDivCollector Connector API V1");
                //c.RoutePrefix = "BioDiv/swagger";
                c.DefaultModelsExpandDepth(-1);
            });

            //https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-5.0 
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseExceptionHandler("/error-development");

                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                ////IdentityModelEventSource.ShowPII = true;        //TODO: REMOVE FROM PRODUCTION ENV! chb: used for debug 7.1.2021
                app.UseExceptionHandler("/error");
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints
                    .MapControllers()
                    .RequireAuthorization();    //Default: user has to be authenticated
            });
        }
    }
}
