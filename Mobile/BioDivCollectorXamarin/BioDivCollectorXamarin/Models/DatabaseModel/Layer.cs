using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    public class Layer
    {
        /// <summary>
        /// Layer database definition
        /// </summary>
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public int layerId { get; set; }
            public string title { get; set; }
            public string url { get; set; }
            public string wmsLayer { get; set; }
            public string uuid { get; set; }

            public bool visible { get; set; }
            public double opacity { get; set; }
            public int order { get; set; }

            [ForeignKey(typeof(Project))]
            public int project_fk { get; set; }
    }
}
