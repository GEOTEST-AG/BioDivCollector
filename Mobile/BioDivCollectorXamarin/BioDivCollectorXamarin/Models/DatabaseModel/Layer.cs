using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    public class Layer
    {
        /// <summary>
        /// Layer database definition
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int layerId { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string wmsLayer { get; set; }
        public string uuid { get; set; }

        public bool visible { get; set; }
        public double opacity { get; set; }
        public int order { get; set; }
        public bool fileBased { get; set; }

        [ForeignKey(typeof(Project))]
        public int project_fk { get; set; }

        public static async Task<bool> FileLayerExists(string title, int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var existingLayers = await conn.Table<Layer>().Where(Layer => Layer.project_fk == projectId).Where(Layer => Layer.title == title).Where(Layer => Layer.fileBased == true).FirstOrDefaultAsync();
                if (existingLayers != null)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static async Task<List<Layer>> FetchLayerListByProjectId(int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Layer>().Where(Layer => Layer.project_fk == projectId).ToListAsync();
        }

        public static async Task<Layer> FetchLayerByLayerId(int layerId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Layer>().Where(Layer => Layer.project_fk == layerId).FirstOrDefaultAsync();
        }

        public static async Task<List<Layer>> FetchLayerByName(string name)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Layer>().Where(Layer => Layer.title == name).ToListAsync();
        }

        public static async Task AddFileLayer(string title, string filename, string filePath, int projectId, int order)
        {
            bool layerExists = await FileLayerExists(title, projectId);

            if (!layerExists)
            {
                var conn = App.ActiveDatabaseConnection;
                try
                {
                    var newLayer = new Layer();
                    var layer = await conn.Table<Layer>().OrderBy(l => l.layerId).ToListAsync();
                    var min = layer.Min(l => l.layerId);
                    newLayer.layerId = min - 1;
                    newLayer.project_fk = projectId;
                    newLayer.fileBased = true;
                    newLayer.url = filePath;
                    newLayer.title = title;
                    newLayer.visible = false;
                    newLayer.opacity = 1;
                    newLayer.order = order;
                    newLayer.wmsLayer = filename;
                    await conn.InsertOrReplaceAsync(newLayer);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static async Task RemoveFileLayers(int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
                try
                {
                    await conn.Table<Layer>().DeleteAsync(layer => (layer.project_fk == projectId && layer.fileBased == true));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        }
    }
}
