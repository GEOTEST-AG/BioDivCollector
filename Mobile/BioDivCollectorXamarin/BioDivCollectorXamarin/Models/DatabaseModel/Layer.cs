using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static bool FileLayerExists(string title, int projectId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var existingLayers = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.project_fk == projectId).Where(Layer => Layer.title == title).Where(Layer => Layer.fileBased == true).FirstOrDefault();
                    if (existingLayers != null)
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public static void AddFileLayer(string title, string filename, string filePath, int projectId, int order)
        {
            if (!FileLayerExists(title, projectId))
            {
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    try
                    {
                        var newLayer = new Layer();
                        var min = conn.Table<Layer>().Select(l => l).OrderBy(l => l.layerId).Min(l => l.layerId);
                        newLayer.layerId = min - 1;
                        newLayer.project_fk = projectId;
                        newLayer.fileBased = true;
                        newLayer.url = filePath;
                        newLayer.title = title;
                        newLayer.visible = false;
                        newLayer.opacity = 1;
                        newLayer.order = order;
                        newLayer.wmsLayer = filename;
                        conn.Insert(newLayer);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public static void RemoveFileLayers(int projectId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    conn.Table<Layer>().Delete(layer => (layer.project_fk == projectId && layer.fileBased == true));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
