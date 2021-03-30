using System;

namespace BioDivCollector.DB.Models.Domain
{
    public class UserHasProjectLayer
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

        public int LayerId { get; set; }
        public Layer Layer { get; set; }

        public bool Visible { get; set; }
        public double Transparency { get; set; }
        /// <summary>
        /// for sorting Layers for project of user 
        /// </summary>
        public int Order { get; set; }


    }
}
