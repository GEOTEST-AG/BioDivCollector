using System.ComponentModel;

namespace BioDivCollector.DB.Models.Domain
{
    public class Status
    {
        public StatusEnum Id { get; set; }
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

        public Status(StatusEnum id, string description)
        {
            this.Id = id;
            this.Description = description;
        }
    }

    public enum StatusEnum
    {
        /// <summary>
        /// Newly created object: FOR SYNC ONLY
        /// </summary>
        [Description("Neu")]    
        created = -1,
        /// <summary>
        /// standard object in DB
        /// </summary>
        [Description("Unverändert")]
        unchanged = 1,
        /// <summary>
        /// Updated object: FOR SYNC ONLY
        /// </summary>
        [Description("Bearbeitet")]
        changed = 2,              
        /// <summary>
        /// deleted object in DB
        /// </summary>
        [Description("Gelöscht")]
        deleted = 3,
    }
}
