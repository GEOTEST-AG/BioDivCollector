using BioDivCollectorXamarin.Models.DatabaseModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

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
        /// Refresh the project list
        /// </summary>
        public ICommand RefreshCommand { get; }


        /// <summary>
        /// Pull to refresh parameters
        /// </summary>
        private bool isRefreshing = false;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        /// <summary>
        /// Create a list of forms to be displayed in the table view
        /// </summary>
        public FormSelectionPageVM()
        {
            RefreshCommand = new Command(ExecuteRefreshCommandAsync);
            GetForms();
        }

        private void GetForms()
        {
            Device.BeginInvokeOnMainThread(async () =>
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

        private void ExecuteRefreshCommandAsync(object obj)
        {
            IsRefreshing = true;

            if (App.IsConnected)
            {
                Task.Run(() =>
                {
                    App.CheckConnection();
                    if (App.IsConnected)
                    {
                        Models.LoginModel.Login.GetUserDetails();
                        GetForms();
                    }
                });
            }

            // Stop refreshing
            IsRefreshing = false;
        }
    }
}
