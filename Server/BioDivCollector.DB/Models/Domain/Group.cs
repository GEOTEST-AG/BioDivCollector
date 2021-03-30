using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using BioDivCollector.DB.Helpers;

namespace BioDivCollector.DB.Models.Domain
{
    public class Group
    {
        public Guid GroupId { get; set; }

        [DisplayName("Name der Gruppe")]
        [GroupNameUniqueValidation]
        public string GroupName { get; set; }
        /// <summary>
        /// Externe Gruppen-ID der jeweiligen Gruppe
        /// </summary>
        [DisplayName("Externe Gruppen-ID")]
        public string ID_Extern { get; set; }

        [DisplayName("Status der Gruppendaten")]
        public GroupStatusEnum GroupStatusId { get; set; }
        [JsonIgnore]
        [DisplayName("Status der Gruppendaten")]
        public virtual GroupStatus GroupStatus { get; set; }

        public StatusEnum StatusId { get; set; }
        [JsonIgnore]
        public virtual Status Status { get; set; }

        public string CreatorId { get; set; }
        [DisplayName("Ersteller der Gruppe")]
        public User Creator { get; set; }

        public List<ProjectGroup> GroupProjects { get; internal set; }   //TODO: check n:n?
        public List<GroupUser> GroupUsers { get; internal set; }

        public List<ChangeLogGroup> GroupChangeLogs { get; set; }
    }
}
