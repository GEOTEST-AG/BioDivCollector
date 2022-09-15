namespace BioDivCollector.DB.Models.Domain
{
    public class FieldType
    {
        public FieldTypeEnum Id { get; set; }
        public string Description { get; set; }

        public FieldType(FieldTypeEnum id, string description)
        {
            this.Id = id;
            this.Description = description;
        }
    }

    public enum FieldTypeEnum
    {
        //TODO: Define types!
        //Es stehen unterschiedliche Datentypen zur Verfügung, wie zum Beispiel Text, Zahl, Ja/Nein, Auswahl, Auswahl mit Freitext, Datum, GUID

        Text = 11,

        Number = 21,

        Boolean = 31,

        DateTime = 41,  //needed? or saved as text?

        Choice = 51,

        Guid = 61,       //needed? or saved as text?

        Header = 71,

        Binary = 81,
    }
}
