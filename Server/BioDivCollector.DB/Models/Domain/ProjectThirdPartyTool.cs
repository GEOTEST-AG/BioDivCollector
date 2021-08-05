using System;
using System.Collections.Generic;
using System.Text;

namespace BioDivCollector.DB.Models.Domain
{
    public class ProjectThirdPartyTool
    {
        public int ProjectThirdPartyToolId { get; set; }
        public Project Project { get; set; }
        public ThirdPartyTool ThirdPartyTool { get; set; }

    }
}
