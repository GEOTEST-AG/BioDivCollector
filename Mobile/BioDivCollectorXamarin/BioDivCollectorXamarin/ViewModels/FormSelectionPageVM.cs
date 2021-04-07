using BioDivCollectorXamarin.Models.DatabaseModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BioDivCollectorXamarin.ViewModels
{
    public class FormSelectionPageVM
    {
        public List<Form> Forms { get; set; }
        public int? Object_pk;

        /// <summary>
        /// Create a list of forms to be displayed in the table view
        /// </summary>
        public FormSelectionPageVM()
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                try
                {
                    var proj = conn.Table<Project>().Where(p => p.projectId == App.CurrentProjectId).FirstOrDefault();
                    Forms = conn.Table<Form>().Where(f => f.project_fk == proj.Id).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

    }
}
