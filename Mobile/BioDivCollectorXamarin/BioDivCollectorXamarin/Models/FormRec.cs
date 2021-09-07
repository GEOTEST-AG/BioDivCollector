using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollectorXamarin.Models
{
    /// <summary>
    /// An object created to allow the list of records to be filtered
    /// </summary>
    public class FormRec
    {
        public string String1 { get; set; }
        public string String2 { get; set; }
        public int FormId { get; set; }
        public int RecId { get; set; }
        public int GeomId { get; set; }
    }
}
