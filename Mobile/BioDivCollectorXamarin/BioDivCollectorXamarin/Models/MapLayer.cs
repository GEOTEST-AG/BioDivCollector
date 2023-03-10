using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Models.DatabaseModel;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models
{
    public class MapLayer : ObservableClass
    {

        public int LayerId { get; set; }
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
                    Layer existingLayer = new Layer();
                    try
                    {
                        Task.Run(async () =>
                        {
                            existingLayer = await Layer.GetExistingLayer(LayerId, Opacity);
                            if (existingLayer != null)
                            {
                                existingLayer.visible = enabled;
                                var conn = App.ActiveDatabaseConnection;
                                await conn.UpdateAsync(existingLayer);
                            }
                        });
                    }
                    catch
                    {

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
                    var project = App.CurrentProject;
                    if (prevZ != layerZ && value > 0 && prevZ > 0)
                    {
                        var dic = new Dictionary<string, int>();
                        dic.Add("oldZ", prevZ);
                        dic.Add("newZ", value);

                        if (Name != null)
                        {
                            Task.Run(async () =>
                            {
                                var conn = App.ActiveDatabaseConnection;

                                    var layerInPostionAlready = await conn.Table<Layer>().Where(Layer => Layer.order == value).Where(Layer => Layer.project_fk == project.Id).FirstOrDefaultAsync();
                                    if (layerInPostionAlready != null)
                                    {
                                        var layerToMove = await conn.Table<Layer>().Where(Layer => Layer.title == Name).Where(Layer => Layer.order == prevZ).Where(Layer => Layer.project_fk == project.Id).FirstOrDefaultAsync();
                                        if (layerToMove != null)
                                        {
                                            layerInPostionAlready.order = prevZ;
                                            layerToMove.order = value;
                                            await conn.UpdateAsync(layerToMove);
                                            await conn.UpdateAsync(layerInPostionAlready);
                                        }
                                    }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                MessagingCenter.Send<MapLayer>(this, "LayerOrderChanged");
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
                Task.Run(async () =>
                {
                    if (Name != null)
                    {
                        try
                        {
                            var conn = App.ActiveDatabaseConnection;
                            var existingLayer = await conn.Table<Layer>().Where(Layer => Layer.title == Name).Where(Layer => Layer.visible == enabled).FirstOrDefaultAsync();
                            if (existingLayer != null)
                            {
                                existingLayer.opacity = opacity;
                                await conn.UpdateAsync(existingLayer);
                            }
                            OnPropertyChanged("Opacity");
                            OnPropertyChanged("MapLayers");
                        }
                        catch (Exception e)
                        {

                        }
                    }
                });
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

        public string ID { get; set; }

        public string WmsLayer { get; set; }

        public string URL { get; set; }

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

        public MapLayer(int localLayerId, bool enabled, int z, Mapsui.Layers.ILayer mapsuiLayer)
        {
            LayerId = localLayerId;
            Enabled = enabled;
            LayerZ = z;
            Name = mapsuiLayer.Name;
            Opacity = mapsuiLayer.Opacity;
            MapsuiLayer = mapsuiLayer;
            LocalStorage = 0;
            ID = null;
            WmsLayer = null;
            URL = null;
        }

        public MapLayer()
        {

        }

    }
}
