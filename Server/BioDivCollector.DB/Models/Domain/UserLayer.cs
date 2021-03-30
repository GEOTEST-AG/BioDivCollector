using System;

namespace BioDivCollector.DB.Models.Domain
{
    public class UserLayer
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public int LayerId { get; set; }
        public Layer Layer { get; set; }

    }
}
