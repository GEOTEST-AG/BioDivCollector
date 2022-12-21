using BioDivCollectorXamarin.Models.DatabaseModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BioDivCollectorXamarin.ViewModels
{
    public class FormSelectionPageVM:BaseViewModel
    {
        private ObservableCollection<Form> forms;
        public ObservableCollection<Form> Forms
        {
            get { return forms; }
            set
            {
                forms = value;
                OnPropertyChanged();
            }
        }
        public int? Object_pk;

        /// <summary>
        /// Create a list of forms to be displayed in the table view
        /// </summary>
        public FormSelectionPageVM()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var formsList = await Form.FetchFormsForProject();
                    Forms = new ObservableCollection<Form>(formsList);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
    }
}
