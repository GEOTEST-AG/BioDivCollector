using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BioDivCollector.DB.Models.Domain
{
    public class Record
    {
        /// <summary>
        /// used for BDC GUID
        /// </summary>
        [Column(Order = 1)]
        public Guid RecordId { get; set; }

        [NotMapped]
        public string BDCGuid
        {
            get
            {
                return "<<BDC><" + RecordId.ToString() + ">>";
            }
        }

        public Guid? GeometryId { get; set; }
        [Column(Order = 2)]
        public ReferenceGeometry Geometry { get; set; }

        [Column("projectid", Order = 3)]
        public Guid? ProjectGroupProjectId { get; set; }        //Nullable
        [Column("groupid", Order = 4)]
        public Guid? ProjectGroupGroupId { get; set; }          //Nullable
        public ProjectGroup ProjectGroup { get; set; }

        public int? FormId { get; set; }
        public Form Form { get; set; }

        public List<TextData> TextData { get; set; } = new List<TextData>();
        public List<NumericData> NumericData { get; set; } = new List<NumericData>();
        public List<BooleanData> BooleanData { get; set; } = new List<BooleanData>();

        public StatusEnum StatusId { get; set; } = StatusEnum.unchanged;
        [JsonIgnore]
        public virtual Status Status { get; set; }

        public List<ChangeLogRecord> RecordChangeLogs { get; set; } = new List<ChangeLogRecord>();

        /// <summary>
        /// Flag, if current user is allowed to make changes or not
        /// </summary>
        [NotMapped]
        public bool ReadOnly { get; set; }
    }
}
