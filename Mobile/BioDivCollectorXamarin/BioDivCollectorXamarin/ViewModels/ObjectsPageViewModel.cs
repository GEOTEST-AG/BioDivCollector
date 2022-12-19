using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Models.DatabaseModel;
using SQLite;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class ObjectsPageVM:BaseViewModel
    {
        /// <summary>
        /// List of Geometries
        /// </summary>
        public ObservableCollection<ReferenceGeometry> Objects { get; set; }

        /// <summary>
        /// List of Records
        /// </summary>
        public ObservableCollection<FormRec> FormRecs { get; set; }

        /// <summary>
        /// A button command to add a new record
        /// </summary>
        public AddRecordButtonCommand AddRecordButtonCommand { get; set; }

        /// <summary>
        /// A button command for copying the BDC GUID
        /// </summary>
        public BDCGUIDGeometryCommand CopyBDCGUIDCommand { get; set; }

        /// <summary>
        /// A button command for deleting geometries
        /// </summary>
        public GeometryDeleteCommand GeometryDeleteCommand { get; set; }

        /// <summary>
        /// Initialisation
        /// </summary>
        public ObjectsPageVM()
        {
            AddRecordButtonCommand = new AddRecordButtonCommand(this);
            Objects = new ObservableCollection<ReferenceGeometry>();
            FormRecs = new ObservableCollection<FormRec>();
            CopyBDCGUIDCommand = new BDCGUIDGeometryCommand(this);
            GeometryDeleteCommand = new GeometryDeleteCommand(this);

            MessagingCenter.Subscribe<Application>(App.Current, "RefreshGeometries", (sender) =>
            {
                UpdateGeometries();
            });

            UpdateGeometries();
        }

        /// <summary>
        /// Refresh the list of geometries
        /// </summary>
        private async void UpdateGeometries()
        {
            if (App.CurrentProjectId != null && App.CurrentProjectId != String.Empty)
            {
                var conn = App.ActiveDatabaseConnection;
                    var project = new Project();
                    try
                    {
                        //project = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == App.CurrentProjectId).FirstOrDefault();
                        project = await Project.FetchProject(App.CurrentProjectId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        if (project.Id != 0)
                        {
                            try
                            {
                                var objectList = await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == project.Id).Where(ReferenceGeometry => ReferenceGeometry.status < 3).OrderBy(ReferenceGeometry => ReferenceGeometry.geometryName).ToListAsync();
                                Objects = new ObservableCollection<ReferenceGeometry>(objectList);
                                OnPropertyChanged("Objects");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
            }
        }
    }

    /// <summary>
    /// The button command for copying the BDC GUID
    /// </summary>
    public class BDCGUIDGeometryCommand : ICommand
    {

        public ObjectsPageVM ObjectsPageViewModel { get; set; }

        public BDCGUIDGeometryCommand(ObjectsPageVM objectsPageVM)
        {
            ObjectsPageViewModel = objectsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            var geom = parameter as ReferenceGeometry;
            var extId = geom.geometryId;
            var bguid = "<<BDC><" + extId + ">>";
            await Clipboard.SetTextAsync(bguid);
            await App.Current.MainPage.DisplayAlert("BDC GUID kopiert", "", "OK");
        }

    }

    /// <summary>
    /// The button command for deleting a geometry
    /// </summary>
    public class GeometryDeleteCommand : ICommand
    {

        public ObjectsPageVM ObjectsPageViewModel { get; set; }
        public RecordsPageVM RecordsPageViewModel { get; set; }

        public GeometryDeleteCommand(ObjectsPageVM objectsPageVM)
        {
            ObjectsPageViewModel = objectsPageVM;
        }

        public GeometryDeleteCommand(RecordsPageVM recordsPageVM)
        {
            RecordsPageViewModel = recordsPageVM;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie diese Geometrie vom Gerät entfernen?", "Abbrechen", "Entfernen");
            if (response == "Entfernen")
            {
                var geom = parameter as ReferenceGeometry;
                await ReferenceGeometry.DeleteGeometry(geom.Id);
            }
        }

    }
}