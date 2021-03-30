using System;
using System.Collections.Generic;

namespace BioDivCollector.Connector.Models.DTO
{
    public class ProjectSyncDTO
    {
        public bool success { get; set; }
        public string error { get; set; }

        public Guid projectId { get; set; }

        public RecordsSyncDTO records { get; set; } = new RecordsSyncDTO();

        public GeometriesSyncDTO geometries { get; set; } = new GeometriesSyncDTO();

        public ProjectDTO projectUpdate { get; set; }
    }

    public class RecordsSyncDTO
    {
        public Dictionary<Guid, string> created { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> updated { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> deleted { get; set; } = new Dictionary<Guid, string>();

        public Dictionary<Guid, string> skipped { get; set; } = new Dictionary<Guid, string>();

    }

    public class GeometriesSyncDTO
    {
        public List<Guid> created { get; set; } = new List<Guid>();
        public Dictionary<Guid, string> updated { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> deleted { get; set; } = new Dictionary<Guid, string>();
        /// <summary>
        /// guid and reasonString for skipping
        /// </summary>
        public Dictionary<Guid, string> skipped { get; set; } = new Dictionary<Guid, string>();

        public RecordsSyncDTO geometryRecords { get; set; } = new RecordsSyncDTO();

    }
}
