using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public class Layer
    {
        public int LayerId { get; set; }

        /// <summary>
        /// available for all projects?
        /// </summary>
        [DisplayName("Für alle zugänglich")]
        public bool Public { get; set; }
        [DisplayName("Bezeichnung")]
        public string Title { get; set; }
        [DisplayName("URL")]
        public string Url { get; set; }
        [DisplayName("Benutzername")]
        public string Username { get; set; }
        [DisplayName("Passwort")]
        public string Password { get; set; }

        public List<ProjectLayer> LayerProjects { get; set; }
        public List<UserLayer> LayerUsers { get; set; }    //optional F14

        public List<UserHasProjectLayer> LayerUsedByProjectUser { get; set; }

        [DisplayName("WMS Layer")]
        public string WMSLayer { get; set; } 
        [NotMapped]
        public string OlCode { get
            {
                string OLCodeString = "new ol.layer.Image({ source: new ol.source.ImageWMS({ ratio: 1, url: '"+ Url.Substring(0, Url.IndexOf("?")) +"',          params: { 'FORMAT': 'image/png', 'VERSION': '1.1.1', STYLES: '', LAYERS: '"+ WMSLayer+"', } }),		opacity: 1      });";
                if (Username!=null)
                {
                    OLCodeString = "new ol.layer.Image({ source: new ol.source.ImageWMS({ ratio: 1, url: '/ProxyWMSSecure/"+ LayerId + "',          params: { 'FORMAT': 'image/png', 'VERSION': '1.1.1', STYLES: '', LAYERS: '" + WMSLayer + "', } }),		opacity: 1      });";
                }
                return OLCodeString;

            }} 

        public List<ChangeLogLayer> LayerChangeLogs { get; set; }
    }
}
