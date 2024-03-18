using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{

    /// <summary>
    /// Hide not visible FieldChoices - helps to make public field choices more geneneral
    /// </summary>
    public class HiddenFieldChoice
    {
        public int HiddenFieldChoiceId { get; set; }
        public FormField FormField { get; set; }
        public FieldChoice FieldChoice { get; set; }
    }
}
