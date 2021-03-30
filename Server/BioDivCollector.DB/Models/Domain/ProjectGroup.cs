using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public sealed class ProjectGroup
    {
        public Guid ProjectId { get; set; }

        public Guid GroupId { get; set; }

        public Project Project { get; set; }
        public Group Group { get; set; }

        // 20210109 chs: Gruppenstatus pro Projekt 
        public GroupStatusEnum GroupStatusId { get; set; }
        public GroupStatus GroupStatus { get; set; }

        public List<ReferenceGeometry> Geometries { get; set; }
        public List<Record> Records { get; set; }

        /// <summary>
        /// Flag for read only groups
        /// </summary>
        public bool ReadOnly { get; set; }
    }
}
