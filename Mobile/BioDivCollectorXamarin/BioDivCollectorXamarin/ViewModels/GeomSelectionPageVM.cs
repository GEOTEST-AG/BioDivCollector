using System;
using System.Collections.ObjectModel;
using BioDivCollectorXamarin.Models.DatabaseModel;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.ViewModels
{
    public class GeomSelectionPageVM:BaseViewModel
    {
        private ObservableCollection<ReferenceGeometry> geometries;
        public ObservableCollection<ReferenceGeometry> Geometries
        {
            get { return geometries; }
            set
            {
                geometries = value;
                OnPropertyChanged();
            }
        }
        public int? Object_pk;

        /// <summary>
        /// Create a list of geometries to be displayed in the table view
        /// </summary>
        public GeomSelectionPageVM()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var geometryList = await ReferenceGeometry.GetAllGeometries();
                    var general = new ReferenceGeometry()
                    {
                        geometryName = "Allgemeine Beobachtung",
                        geometryId = null
                    };
                    geometryList.Insert(0, general);
                    geometryList.Sort((x, y) => x.geometryName.CompareTo(y.geometryName));

                    Geometries = new ObservableCollection<ReferenceGeometry>(geometryList);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
    }
}
