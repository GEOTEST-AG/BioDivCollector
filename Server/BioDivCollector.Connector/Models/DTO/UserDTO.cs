using BioDivCollector.DB.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.Connector.Models.DTO
{
    //UserController

    public class UserDTO
    {
        public bool success { get; set; }
        public string error { get; set; }

        public string userId { get; set; }
        public string name { get; set; }
        public string firstName { get; set; }

        public List<string> roles { get; set; } = new List<string>();
        public string activeRole { get; set; }
        //public List<GroupDTO> groups { get; set; } = new List<GroupDTO>();
        public List<ProjectDTOSimple> projects { get; set; } = new List<ProjectDTOSimple>();
    }

    //public class GroupDTO
    //{
    //    public Guid groupId { get; set; }
    //    public string groupName { get; set; }
    //    //public int groupUsersCount { get; set; }

    //    public string id_Extern { get; set; }
    //    public GroupStatusEnum groupStatusId { get; set; }

    //    public List<ProjectDTOSimple> projects { get; set; } = new List<ProjectDTOSimple>();

    //    //TODO: add changelog information?
    //}

    /// <summary>
    /// DTO for project used by userController
    /// </summary>
    public class ProjectDTOSimple
    {
        public Guid projectId { get; set; }
        public string projectName { get; set; }
        public string description { get; set; }
        public string projectNumber { get; set; }
        public string id_Extern { get; set; }
        public ProjectStatusEnum projectStatusId { get; set; } 

        public string projectManager { get; set; }
        public string projectConfigurator { get; set; }

        //public int recordsCount { get; set; }
        //public int geometriesCount { get; set; }

        //TODO: add changelog information?

        public ProjectDTOSimple(Project project)
        {
            projectId = project.ProjectId;
            projectName = project.ProjectName;
            description = project.Description;
            projectNumber = project.ProjectNumber;
            id_Extern = project.ID_Extern;
            projectStatusId = project.ProjectStatusId;
            projectManager = project.ProjectManager?.ToString();
            projectConfigurator = project.ProjectConfigurator?.ToString();
        }
    }
}
