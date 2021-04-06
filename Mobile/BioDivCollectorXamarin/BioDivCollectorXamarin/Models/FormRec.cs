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
        public string Timestamp { get; set; }
        public string Title { get; set; }
        public string FormType { get; set; }
        public int FormId { get; set; }
        public int RecId { get; set; }
        public string User { get; set; }
        public string GeometryName { get; set; }
        public int GeomId { get; set; }
    }
}
