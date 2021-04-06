using System;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.Models.DatabaseModel;
using Xamarin.Forms;

namespace BioDivCollectorXamarin
{
    class ProjectListDataTemplateSelector : Xamarin.Forms.DataTemplateSelector
    {
        public Xamarin.Forms.DataTemplate ExistingProject { get; set; }
        public Xamarin.Forms.DataTemplate OnlineProject { get; set; }
        public Xamarin.Forms.DataTemplate CurrentProject { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            try
            {
                var proj = (ProjectSimple)item;
                var projId = proj.projectId;
                string projectId = projId.ToString();
                if (projectId != null && projectId == App.CurrentProjectId)
                {
                    return CurrentProject;
                }
                bool exists = Project.LocalProjectExists(projectId);
                if (exists)
                {
                    return ExistingProject;
                }
                return OnlineProject;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return OnlineProject;
            }

        }
    }
}
