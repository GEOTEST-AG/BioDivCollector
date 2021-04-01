using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BioDivCollector.PluginContract;
using BioDivCollector.WebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace BioDivCollector.SamplePlugin
{
    public class StatisticsController : Controller, IPlugin
    {
        public IConfiguration Configuration { get; }


        public StatisticsController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IActionResult LoginStatistic()
        {
            UsersController uc = new UsersController(Configuration);
            string access_token = uc.GetAdminAccessToken();

            if (access_token != null)
            {
                var client = new RestClient("https://id.biodivcollector.ch/auth/admin/realms/BioDivCollector/events?type=LOGIN&client=BioDivCollector");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + access_token);
                IRestResponse response = client.Execute(request);
                List<LoginEvents> loginEvents = JsonSerializer.Deserialize<List<LoginEvents>>(response.Content);

                return View(loginEvents);
            }

            return RedirectToAction("NotAllowed", "Home");
        }

        public string GetName()
        {
            return "Statistics";
        }
    }

    public class LoginEvents
    {
        public long time { get; set; }
        public string ipAddress { get; set; }
        public Details details { get; set; }

        public DateTime LoginTime()
        {
            // Java timestamp is milliseconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(time).ToLocalTime();
            return dtDateTime;
        }

    }

    public class Details
    {
        public string username { get; set; }
        public string auth_method { get; set; }
        public string redirect_uri { get; set; }
    }
}

