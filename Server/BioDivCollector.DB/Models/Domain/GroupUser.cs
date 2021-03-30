using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public sealed class GroupUser
    {
        public string UserId { get; set; }
        public Guid GroupId { get; set; }

        public User User { get; set; }
        public Group Group { get; set; }


    }
}
