using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;

namespace BioDivCollector.WFS.Services
{
    public interface IUserService
    {
        bool IsValidUser(string userName, string password, HttpContext httpContext);
    }

    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        IConfigurationRoot configuration;

        // inject database for user validation
        public UserService(ILogger<UserService> logger)
        {
            _logger = logger;
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<UserService>()
                .AddEnvironmentVariables()
                .Build();
        }

        public bool IsValidUser(string userName, string password, HttpContext httpContext)
        {
            _logger.LogInformation($"Validating user [{userName}]");

            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }


            var client = new RestClient(configuration.GetSection("Jwt").GetSection("Admin-Token-URL").Value);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", configuration.GetSection("Jwt").GetSection("Client").Value);
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_secret", configuration.GetSection("Jwt").GetSection("Key").Value);
            request.AddParameter("scope", "openid");
            request.AddParameter("username", userName);
            request.AddParameter("password", password);
            IRestResponse response = client.Execute(request);
            dynamic json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
            try
            {
                var jwtToken = new JwtSecurityToken(json.access_token.Value);

                var claims = jwtToken.Claims;
                var userIdentity = new ClaimsIdentity(claims, "NonEmptyAuthType", ClaimTypes.NameIdentifier, ClaimTypes.Role);

                // Add the keycloak roles to the user

                // flatten realm_access because Microsoft identity model doesn't support nested claims
                // by map it to Microsoft identity model, because automatic JWT bearer token mapping already processed here
                if (userIdentity.IsAuthenticated && userIdentity.HasClaim((claim) => claim.Type == "resource_access"))
                {
                    var realmAccessClaim = userIdentity.FindFirst((claim) => claim.Type == "resource_access");

                    dynamic realms = JsonConvert.DeserializeObject(realmAccessClaim.Value);
                    var rolesarray = realms.BioDivCollector.roles;
                    foreach (string role in realms.BioDivCollector.roles.ToObject<List<string>>())
                    {
                        userIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }

                }



                httpContext.User = new ClaimsPrincipal(userIdentity);



                return true;

            }
            catch (Exception e)
            {
                return false;
            }

        }
       
    }

    public class RolesObject
    {

    }

    
}
