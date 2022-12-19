using System;
using BioDivCollectorXamarin.Models.LoginModel;
using BioDivCollectorXamarin.Models.DatabaseModel;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace BioDivCollectorXamarin
{
    class ProjectListDataTemplateSelector : Xamarin.Forms.DataTemplateSelector
    {
        public Xamarin.Forms.DataTemplate ExistingProject { get; set; }
        public Xamarin.Forms.DataTemplate OnlineProject { get; set; }
        public Xamarin.Forms.DataTemplate CurrentProject { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Task.Run(async () =>
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
                    bool exists = await Project.LocalProjectExists(projectId);
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
            });
            return new DataTemplate();
        }
    }
}
