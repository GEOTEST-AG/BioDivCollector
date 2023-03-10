using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Attributes;

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

        /// <summary>
        /// Checks if the layer exists
        /// </summary>
        /// <param name="title"></param>
        /// <param name="projectId"></param>
        /// <returns>Returns True/False</returns>
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

        /// <summary>
        /// Gets a list of layers by projectId
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>Returns a List of Layers</returns>
        public static async Task<List<Layer>> FetchLayerListByProjectId(int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Layer>().Where(Layer => Layer.project_fk == projectId).ToListAsync();
        }

        /// <summary>
        /// Gets a layer by layerId
        /// </summary>
        /// <param name="layerId"></param>
        /// <returns>Returns a layer</returns>
        public static async Task<Layer> FetchLayerByLayerId(int layerId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Layer>().Where(Layer => Layer.project_fk == layerId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a layer by layerId and opacity
        /// </summary>
        /// <param name="layerId"></param>
        /// <param name="opacity"></param>
        /// <returns>Returns a Layer</returns>
        public static async Task<Layer> GetExistingLayer(int layerId, double opacity)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Layer>().Where(Layer => Layer.Id == layerId).Where(Layer => Layer.opacity == opacity).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a list of layer by the title of layer
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns a list of layers</returns>
        public static async Task<List<Layer>> FetchLayerByName(string name)
        {
            var conn = App.ActiveDatabaseConnection;
            var test = await conn.Table<Layer>().Where(Layer => Layer.title == name).ToListAsync();
            return test;
        }

        /// <summary>
        /// Adds a new layer to the database
        /// </summary>
        /// <param name="title"></param>
        /// <param name="filename"></param>
        /// <param name="filePath"></param>
        /// <param name="projectId"></param>
        /// <param name="order"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes a layer form the database
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public static async Task RemoveFileLayers(int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                await conn.Table<Layer>().DeleteAsync(layer => (layer.project_fk == projectId && layer.fileBased == true));

                //reorder the online layers to compensate for the lack of offline layers
                var onlineLayers = await conn.Table<Layer>().Where(layer => layer.project_fk == projectId).OrderBy(layer => layer.order).ToListAsync();
                int i = 1;
                foreach (var layer in onlineLayers)
                {
                    layer.order = i;
                    await conn.UpdateAsync(layer);
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Queries a list of layers available for a particular project
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>List of layers</returns>
        public static async Task<List<Layer>> GetLayersForMap(string projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var proj = await conn.Table<Project>().Where(Project => Project.projectId == projectId).FirstOrDefaultAsync();
                var layers = await conn.Table<Layer>().Where(Layer => Layer.project_fk == proj.Id).ToListAsync();
                return layers;
            }
            catch
            {
                return new List<Layer>();
            }
        }
    }
}
