using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BioDivCollector.DB.Models.Domain
{
    public class GroupStatus
    {
        public GroupStatusEnum Id { get; set; }
        public string Description { get {
                var field = Id.GetType().GetField(Id.ToString());
                var attributes = field.GetCustomAttributes(false);

                // Description is in a hidden Attribute class called DisplayAttribute
                // Not to be confused with DisplayNameAttribute
                dynamic displayAttribute = null;

                if (attributes.Length>0)
                {
                    displayAttribute = attributes[0];
                }

                // return description
                return displayAttribute?.Description ?? "Keine Beschreibung";

            }
            set { } }

        public GroupStatus(GroupStatusEnum id, string description)
        {
            this.Id = id;
            this.Description = description;
        }
    }
    public enum GroupStatusEnum
    {
        [Description("Gruppe(n) neu")]
        Gruppe_neu = 1,
        [Description("Gruppe(n) bereit")]
        Gruppe_bereit = 2,
        [Description("Gruppendaten erfasst")]
        Gruppendaten_erfasst = 3,

        [Description("Gruppendaten fehlerhaft")]
        Gruppendaten_fehlerhaft = 9,

        [Description("Gruppendaten gültig")]
        Gruppendaten_gueltig = 4,
    }
}
