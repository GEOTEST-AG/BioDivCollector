using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace BioDivCollector.DB.Models.Domain
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string UserId { get; set; }

        public string Name { get; set; }
        public string FirstName { get; set; }
        [EmailAddress]
        public string Email { get; set; }

        public List<GroupUser> UserGroups { get; internal set; }
        //public List<UserRole> UserRoles { get; internal set; }
        public List<UserLayer> UserLayers { get; internal set; }    //optional F14
        public List<UserHasProjectLayer> UserHasProjectLayers { get; internal set; }  

        public StatusEnum StatusId { get; set; }       
        [JsonIgnore]
        public virtual Status Status { get; set; }

        public override string ToString()
        {
            return $"{FirstName} {Name}";
        }
    }
}
