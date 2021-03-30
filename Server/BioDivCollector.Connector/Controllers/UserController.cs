using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BioDivCollector.Connector.Models.DTO;
using BioDivCollector.DB.Helpers;
using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BioDivCollector.Connector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[AllowAnonymous]
    public class UserController : ControllerBase
    {
        private readonly BioDivContext _context;
        private readonly ILogger _logger;

        public UserController(BioDivContext context, ILogger<ProjectController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/<UserController>
        /// <summary>
        /// Get user projects for EF role
        /// </summary>
        /// <returns>UserDTO</returns>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<UserDTO>> Get()
        {
            string userName = ((ClaimsIdentity)User.Identity).FindFirst("preferred_username").Value;

            string rolesJsonString = ((ClaimsIdentity)User.Identity).FindFirst("resource_access").Value;    //Roles from Token
            UserDTO userDto;

            if (userName != null && rolesJsonString != null)
            {
                User user = await _context.Users.FindAsync(userName);   //Check db for existing user

                if (user != null)
                {
                    JToken jToken = JToken.Parse(rolesJsonString);
                    JObject jsonObject = JObject.Parse(jToken.ToString());
                    JArray rolesJArray = (JArray)jsonObject["BioDivCollector"]["roles"];
                    List<string> rolesList = rolesJArray.Select(r => (string)r).ToList(); //List with roles strings

                    userDto = new UserDTO()
                    {
                        success = true,
                        userId = userName,
                        name = user.Name,
                        firstName = user.FirstName,
                        roles = rolesList,
                    };

                    if (rolesList.Contains(RoleEnum.EF.ToString()))    //if EF -> all his group projects
                    {
                        userDto.activeRole = RoleEnum.EF.ToString();

                        var projects = await ProjectManager.UserProjectsAsync(_context, user, RoleEnum.EF);
                        foreach (Project project in projects)
                        {
                            ProjectDTOSimple projectDto = new ProjectDTOSimple(project);
                            userDto.projects.Add(projectDto);
                        }
                    } //end of "EF"
                    else
                    {
                        userDto.success = false;
                        userDto.error = $"Role(s) not accepted by API";
                    }

                }
                else
                {
                    if (userName == null)
                    {
                        userDto = new UserDTO()
                        {
                            success = false,
                            error = $"Username not found in DB: {userName}"
                        };
                    }
                    else
                    {
                        userDto = new UserDTO()
                        {
                            success = false,
                            error = $"User roles not defined"
                        };
                    }
                }
            }
            else
            {
                userDto = new UserDTO()
                {
                    success = false,
                    error = "Username not found in Token"
                };
            }

            if (userDto.success)
            {
                _logger.LogInformation("USER JSON GET: user = {userName}",
                                       userName);
            }
            else
            {
                _logger.LogError("USER JSON GET: user = {userName} \n" +
                                 "\terror = {error}",
                                       userName, userDto.error);
            }

            return userDto;
        }

        // GET api/<UserController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

    }
}
