namespace BioDivCollector.DB.Models.Domain
{
    public class FieldChoice
    {
        public int FieldChoiceId { get; set; }

        public FormField FormField { get; set; }

        public string Text { get; set; }

        public int Order { get; set; }
      
    }

}
