using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace BioDivCollector.DB.Models.Domain
{
    /// <summary>
    /// Bezugsgeometrie
    /// </summary>
    public class ReferenceGeometry 
    {
        /// <summary>
        /// used for BDC GUID
        /// </summary>
        [Key]
        [Column(Order = 1)]
        public Guid GeometryId { get; set; }

        /// <summary>
        /// Beschreibung: Eindeutiger Eintrag zur späteren Identifikation
        /// der Geometrieobjekte in einem Projekt, z.B: Name oder Nummer
        /// des Kartierobjekts o.ä. – Standardformat:(Kartierobjekt)-(fortlaufende Nummer)
        /// </summary>
        public string GeometryName { get; set; }

        [Column("projectid", Order = 2)]
        public Guid ProjectGroupProjectId { get; set; }
        [Column("groupid", Order = 3)]
        public Guid ProjectGroupGroupId { get; set; }
        public ProjectGroup ProjectGroup { get; set; }

        public List<Record> Records { get; set; } = new List<Record>();

        public Point Point { get; set; }
        public LineString Line { get; set; }
        public Polygon Polygon { get; set; }

        public StatusEnum StatusId { get; set; } = StatusEnum.unchanged;       
        [JsonIgnore]
        public virtual Status Status { get; set; }

        public List<ChangeLogGeometry> GeometryChangeLogs { get; set; } = new List<ChangeLogGeometry>();

        /// <summary>
        /// Flag, if current user is allowed to make changes or not
        /// </summary>
        [NotMapped]
        public bool ReadOnly { get; set; }
    }
}
