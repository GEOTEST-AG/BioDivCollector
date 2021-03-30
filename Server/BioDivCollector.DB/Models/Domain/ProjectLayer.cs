using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioDivCollector.DB.Models.Domain
{
    public class ProjectLayer
    {
        public int LayerId { get; set; }
        public Layer Layer { get; set; }

        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

        [NotMapped]
        public bool Visible { get; set; }

        [NotMapped]
        public double Transparency { get; set; }
        [NotMapped]
        public int Order { get; set; }
    }
}
