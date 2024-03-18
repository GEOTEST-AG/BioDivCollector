using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public class ThirdPartyTool
    {
        public int ThirdPartyToolId { get; set; }
        public string Name { get; set; }
        public virtual List<ProjectThirdPartyTool> ThirdPartyToolProjects { get; set; }
    }
}
