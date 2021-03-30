using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.Connector.Controllers
{
    [Route("api/Uptime")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UptimeController : ControllerBase
    {
        private readonly BioDivContext _context;
        private readonly ILogger _logger;

        public UptimeController(BioDivContext context, ILogger<ProjectController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class AuthorizationFilterAttribute : Attribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                var apiKey = context.HttpContext.Request.Headers["Authorization"];

                if (apiKey.Any())
                {
                    var subStrings = apiKey.ToString().Split(" ");

                    if (subStrings.Length > 0 && subStrings[0] == "C72642FC-C871-4144-AA70-FA298EBAF9B2")
                    {
                        //do nothing
                    }
                    else
                    {
                        context.Result = new NotFoundResult();
                    }
                }
                else
                {
                    context.Result = new NotFoundResult();
                }
            }
        }

        /// <summary>
        /// Check if connector is up and running (protected)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [AuthorizationFilter]
        public async Task<ObjectResult> GetUptime()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    _logger.LogInformation("Uptime Check: OK!");
                    return Ok("ok");
                }
                else
                {
                    _logger.LogError("Uptime Check: DB connection failed!");
                    return BadRequest("error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uptime Check: DB connection failed! See exception for more details.");
                return BadRequest("error");
            }

        }
    }
}
