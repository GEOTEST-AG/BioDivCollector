using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public class ChangeLog
    {
        public long ChangeLogId { get; set; }
        public DateTimeOffset ChangeDate { get; set; }

        public string UserId { get; set; }
        [Required]
        public User User { get; set; }
        [Required]
        public string Log { get; set; }

        //public Layer Layer { get; set; }    //int
        //public Form Form { get; set; }      //int
        //public Group Group { get; set; }    //guid
        //public Project Project { get; set; }  //guid
        //public ReferenceGeometry Geometry { get; set; }   //guid
        //public Record Record { get; set; }  //guid
    }

    public class ChangeLogProject
    {
        public long ChangeLogId { get; set; }
        public ChangeLog ChangeLog { get; set; }

        public Guid ProjectId { get; set; }
        public Project Project { get; set; }
    }

    public class ChangeLogGeometry
    {
        public long ChangeLogId { get; set; }
        public ChangeLog ChangeLog { get; set; }

        public Guid GeometryId { get; set; }
        public ReferenceGeometry Geometry { get; set; }
    }

    public class ChangeLogRecord
    {
        public long ChangeLogId { get; set; }
        public ChangeLog ChangeLog { get; set; }

        public Guid RecordId { get; set; }
        public Record Record { get; set; }
    }

    public class ChangeLogGroup
    {
        public long ChangeLogId { get; set; }
        public ChangeLog ChangeLog { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; }
    }

    public class ChangeLogLayer
    {
        public long ChangeLogId { get; set; }
        public ChangeLog ChangeLog { get; set; }

        public int LayerId { get; set; }
        public Layer Layer { get; set; }
    }

    public class ChangeLogForm
    {
        public long ChangeLogId { get; set; }
        public ChangeLog ChangeLog { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; }
    }
}
