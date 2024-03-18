using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BioDivCollector.DB.Models.Domain
{
    public class FormField
    {
        public int FormFieldId { get; set; }

        public FieldTypeEnum FieldTypeId { get; set; }
        [JsonIgnore]
        public virtual FieldType FieldType { get; set; }
        public List<FormFormField> FormFieldForms { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string StandardValue { get; set; }
        public string Source { get; set; }

        public List<FieldChoice> FieldChoices { get; set; }
        public List<HiddenFieldChoice> HiddenFieldChoices { get; set; }

        /// <summary>
        /// for sorting formfields in forms
        /// </summary>
        public int Order { get; set; }

        public bool Mandatory { get; set; }
        public bool UseInRecordTitle { get; set; }
        public bool Public { get; set; }

        public int? PublicMotherFormFieldFormFieldId { get; set; }
        public virtual FormField PublicMotherFormField { get; set; }
    }

}
