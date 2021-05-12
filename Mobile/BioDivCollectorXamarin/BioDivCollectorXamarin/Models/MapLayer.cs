using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.ViewModels;
using SQLite;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models
{
    public class MapLayer : ObservableClass
    {
        /// <summary>
        /// Set whether the map layer is shown in the map
        /// </summary>
        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                if (Name != null)
                {
                    using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
                    {
                        var existingLayer = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.title == Name).Where(Layer => Layer.opacity == Opacity).FirstOrDefault();
                        if (existingLayer != null)
                        {
                            existingLayer.visible = enabled;
                            conn.Update(existingLayer);
                        }
                    }
                    OnPropertyChanged("Enabled");
                    OnPropertyChanged("MapLayers");
                }
            }
        }

        /// <summary>
        /// Vertical stack height of the layer on the map
        /// </summary>
        private int layerZ;
        public int LayerZ
        {
            get { return layerZ; }
            set
            {
                var prevZ = layerZ;
                layerZ = value;

                try
                {
                    var project = Project.FetchCurrentProject();
                    if (prevZ != layerZ && value > 0 && prevZ > 0)
                    {
                        var dic = new Dictionary<string, int>();
                        dic.Add("oldZ", prevZ);
                        dic.Add("newZ", value);

                        if (Name != null)
                        {
                            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
                            {
                                var layerInPostionAlready = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.order == value).Where(Layer => Layer.project_fk == project.Id).FirstOrDefault();
                                if (layerInPostionAlready != null)
                                {
                                    var layerToMove = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.title == Name).Where(Layer => Layer.order == prevZ).Where(Layer => Layer.project_fk == project.Id).FirstOrDefault();
                                    if (layerToMove != null)
                                    {
                                        layerInPostionAlready.order = prevZ;
                                        layerToMove.order = value;
                                        conn.Update(layerToMove);
                                        conn.Update(layerInPostionAlready);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        
        /// <summary>
        /// Opacity of the map layer
        /// </summary>
        private double opacity;
        public double Opacity
        {
            get { return opacity; }
            set
            {
                opacity = value;
                if (Name != null)
                {
                    using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
                    {
                        var existingLayer = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.title == Name).Where(Layer => Layer.visible == enabled).FirstOrDefault();
                        if (existingLayer != null)
                        {
                            existingLayer.opacity = opacity;
                            conn.Update(existingLayer);
                        }
                    }
                    OnPropertyChanged("Opacity");
                    OnPropertyChanged("MapLayers");
                }

            }
        }

        /// <summary>
        /// Name of the map layer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Amount of memory used by local storage of this map layer
        /// </summary>
        public long LocalStorage { get; set; }

        /// <summary>
        /// A string representing the local storage value, but converted to the appropriate unit (Byte, MB, GB)
        /// </summary>
        private string localStorageString;
        public string LocalStorageString
        {
            get {
                double locStore = (double)LocalStorage;
                return MapModel.GetStorageStringForSize(locStore);
            }
        }

        /// <summary>
        /// The layer used by Mapsui
        /// </summary>
        private Mapsui.Layers.ILayer mapsuiLayer;
        public Mapsui.Layers.ILayer MapsuiLayer { get; set; }

        public MapLayer(bool enabled, int z, Mapsui.Layers.ILayer mapsuiLayer)
        {
            Enabled = enabled;
            LayerZ = z;
            Name = mapsuiLayer.Name;
            Opacity = mapsuiLayer.Opacity;
            MapsuiLayer = mapsuiLayer;
            LocalStorage = 0;
        }

        public MapLayer()
        {

        }
    }
}
