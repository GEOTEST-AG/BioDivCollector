using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public class ProjectForm
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; }
    }
}
