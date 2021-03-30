using System.ComponentModel;

namespace BioDivCollector.DB.Models.Domain
{
    /// <summary>
    /// Projektstatus a.k.a. Bearbeitungsstatus
    /// </summary>
    public class ProjectStatus
    {
        public ProjectStatusEnum Id { get; set; }
        public string Description
        {
            get
            {
                var field = Id.GetType().GetField(Id.ToString());
                var attributes = field.GetCustomAttributes(false);

                // Description is in a hidden Attribute class called DisplayAttribute
                // Not to be confused with DisplayNameAttribute
                dynamic displayAttribute = null;

                if (attributes.Length > 0)
                {
                    displayAttribute = attributes[0];
                }

                // return description
                return displayAttribute?.Description ?? "Keine Beschreibung";

            }
            set { }
        }

        public ProjectStatus(ProjectStatusEnum id, string description)
        {
            this.Id = id;
            this.Description = description;
        }
    }

    public enum ProjectStatusEnum
    {
        [Description("Neues Projekt")]
        Projekt_neu = 1,
        [Description("Projekt bereit")]
        Projekt_bereit = 2,
        [Description("Projekt gültig")] //= abgeschlossen
        Projekt_gueltig = 3

    }

}
