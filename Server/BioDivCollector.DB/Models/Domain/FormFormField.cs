namespace BioDivCollector.DB.Models.Domain
{
    public class FormFormField
    {
        public int FormFieldId { get; set; }
        public FormField FormField { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; }

    }
}
