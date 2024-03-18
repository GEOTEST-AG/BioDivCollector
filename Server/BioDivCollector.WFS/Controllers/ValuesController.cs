﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using BioDivCollector.DB.Models.Domain;
using BioDivCollector.WFS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BioDivCollector.WFS.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {

        BioDivContext db = new BioDivContext();

        private readonly ILogger<ValuesController> _logger;

        public ValuesController(ILogger<ValuesController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// API allows anonymous
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<int> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 3).Select(x => rng.Next(0, 100));
        }

        /// <summary>
        /// API requires JWT auth
        /// </summary>
        /// <returns></returns>
        [HttpGet("jwt")]
        [Authorize]
        public IEnumerable<int> JwtAuth()
        {
            var username = User.Identity.Name;
            _logger.LogInformation($"User [{username}] is visiting jwt auth with token {1}");
            var rng = new Random();
            return Enumerable.Range(1, 10).Select(x => rng.Next(0, 100));
        }

        /// <summary>
        /// API requires Basic auth
        /// </summary>
        /// <returns></returns>
        [HttpGet("basic")]
        [BasicAuth] // You can optionally provide a specific realm --> [BasicAuth("my-realm")]
        public IEnumerable<int> BasicAuth()
        {
            ClaimsIdentity identity = (ClaimsIdentity)User.Identity;
            string username = identity.FindFirst("preferred_username").Value;
            //User u = db.Users.Find(username);

            var rng = new Random();
            return Enumerable.Range(1, 10).Select(x => rng.Next(0, 100));
        }

        [HttpGet("basic-logout")]
        [BasicAuth]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult BasicAuthLogout()
        {
            _logger.LogInformation("basic auth logout");
            // NOTE: there's no good way to log out basic authentication. This method is a hack.
            HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"My Realm\"";
            return new UnauthorizedResult();
        }



    }
}
