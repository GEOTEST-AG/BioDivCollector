using BioDivCollectorXamarin.Models.DatabaseModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xamarin.Essentials;

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
            try
            {
                Forms = Form.FetchFormsForProject();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}
