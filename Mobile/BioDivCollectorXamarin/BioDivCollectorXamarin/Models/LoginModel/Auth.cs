using System;
namespace BioDivCollectorXamarin.Models.LoginModel
{
    /// <summary>
    /// The class used to parse the Auth.xml file
    /// </summary>
    public class Auth
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
        public Uri AuthorizeUrl { get; set; }
        public Uri RedirectUrl { get; set; }
        public Uri AccessTokenUrl { get; set; }

        public Auth()
        {
            
        }
    }
}
