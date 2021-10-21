using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace BioDivCollector.DB.Models.Domain
{
    public class Project 
    {
        /// <summary>
        /// used for BDC GUID
        /// </summary>
        public Guid ProjectId { get; set; }
        [NotMapped]
        public string BDCGuid
        {
            get
            {
                return "<<BDC><" + ProjectId.ToString() + ">>";
            }
        }
        /// <summary>
        /// Eindeutige Kurzbezeichnung
        /// </summary>
        [DisplayName("Name des Projektes")]
        public string ProjectName { get; set; }
        [DisplayName("Kurze Beschreibung")]
        public string Description { get; set; }
        [DisplayName("Projektnummer (extern)")]
        public string ProjectNumber { get; set; }                  //TODO: check if needed

        [DisplayName("Projektleiter")]
        public User ProjectManager { get; set; }
        [DisplayName("Projektkonfigurator")]
        public User ProjectConfigurator{ get; set; }

        /// <summary>
        /// Externe Projekt-ID des jeweiligen Datenherrn
        /// </summary>
        [DisplayName("Projekt ID extern")]
        public string ID_Extern { get; set; }
        /// <summary>
        /// Daten als Open Government Data (OGD) deklariert (gültige Werte: Ja / Nein)
        /// </summary>
        [DisplayName("Open Government Data (OGD)")]
        public bool OGD { get; set; }

        /// <summary>
        /// Projekt hat Artendaten von welchen externen Tools
        /// </summary>
        [DisplayName("Projektdaten mit GUID-Bezug zu Daten in folgenden Anwendungen")]
        public List<ProjectThirdPartyTool> ProjectThirdPartyTools { get; set; }
        [NotMapped]
        public string ProjectThirdPartyToolsString { get; set; }

        /// <summary>
        /// Aktueller Status des Projekts (gültige Werte siehe Geschäftsorganisationskonzept) = Bearbeitungsstatus
        /// </summary>
        public ProjectStatusEnum ProjectStatusId { get; set; }
        [JsonIgnore]
        [DisplayName("Projektstatus")]
        public virtual ProjectStatus ProjectStatus { get; set; }

        //public List<Record> Records { get; internal set; } = new List<Record>();
        //public List<ReferenceGeometry> Geometries { get; internal set; } = new List<ReferenceGeometry>();
        public List<ProjectGroup> ProjectGroups { get; internal set; }
        public List<ProjectLayer> ProjectLayers { get; internal set; }
        public List<ProjectForm> ProjectForms { get; internal set; }

        public StatusEnum StatusId { get; set; }              
        [JsonIgnore]
        public virtual Status Status { get; set; }

        public List<ChangeLogProject> ProjectChangeLogs { get; set; }

        
    }

  
}
