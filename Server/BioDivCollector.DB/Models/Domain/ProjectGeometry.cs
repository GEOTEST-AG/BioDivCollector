using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public sealed class ProjectGeometry
    {
        public Guid ProjectId { get; set; }
        public Guid GeometryId { get; set; }

        public Project Project { get; set; }
        public ReferenceGeometry Geometry { get; set; }
    }
}
