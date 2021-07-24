using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    /// <summary>
    /// Geometry database table definition
    /// </summary>
    [Table("ReferenceGeometry")]
    public class ReferenceGeometry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string geometryId { get; set; }

        public string geometryName { get; set; }

        public string geometry { get; set; }

        public string userName { get; set; }
        public string fullUserName { get; set; }

        public DateTime timestamp { get; set; }
        public DateTime creationTime { get; set; }

        public int status { get; set; }
        public bool readOnly { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Record> records { get; set; } = new List<Record>();

        [ForeignKey(typeof(Project))]
        public int project_fk { get; set; }


        /// <summary>
        /// Get geometry by database table geometry id
        /// </summary>
        /// <param name="geomId"></param>
        /// <returns></returns>
        public static ReferenceGeometry GetGeometry(int geomId)
        {
            ReferenceGeometry queriedGeom;
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                queriedGeom = conn.Get<ReferenceGeometry>(geomId);
            }
            return queriedGeom;
        }

        /// <summary>
        /// Get all geometries for the current project
        /// </summary>
        /// <returns></returns>
        public static List<ReferenceGeometry> GetAllGeometries()
        {
            var queriedGeoms = new List<ReferenceGeometry>();
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var currentProj = conn.Table<Project>().Where(p => p.projectId == App.CurrentProjectId).FirstOrDefault();
                queriedGeoms = conn.Table<ReferenceGeometry>().Where(g => g.project_fk == currentProj.Id).ToList();

            }
            return queriedGeoms;
        }

        /// <summary>
        /// Get a list of only the names of the geometries for the current project
        /// </summary>
        /// <returns></returns>
        public static List<ReferenceGeometry> GetAllGeometryNames()
        {
            var queriedGeoms = new List<ReferenceGeometry>();
            var project = Project.FetchCurrentProject();
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                queriedGeoms = conn.Table<ReferenceGeometry>().Select(g => g).Where(geom => geom.project_fk == project.Id).ToList();
            }
            return queriedGeoms;
        }


        /// <summary>
        /// Save a geometry to the database given a list of coordinates and a name
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="name"></param>
        public static void SaveGeometry(List<Mapsui.Geometries.Point> pointList, string name)
        {
            var geom = new ReferenceGeometry();
            geom.geometryId = Guid.NewGuid().ToString();
            geom.geometryName = name;
            geom.geometry = DataDAO.CoordinatesToGeoJSON(pointList);
            geom.userName = App.CurrentUser.userId;
            geom.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
            geom.timestamp = DateTime.Now;
            geom.creationTime = DateTime.Now;
            geom.status = -1;
            geom.readOnly = false;
            var proj = Project.FetchCurrentProject();
            geom.project_fk = proj.Id;

            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                conn.Insert(geom);

                var project = conn.GetWithChildren<Project>(proj.Id);
                project.geometries = conn.Table<ReferenceGeometry>().Where(g => g.project_fk == proj.Id).ToList();
                conn.UpdateWithChildren(project);

            }
        }

        /// <summary>
        /// Update the geometry with any changes made
        /// </summary>
        /// <param name="geom"></param>
        public static void SaveGeometry(ReferenceGeometry geom)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                conn.Update(geom);
            }
        }


        /// <summary>
        /// Delete a geometry from the database (set its status to deleted) given its database geometry id
        /// </summary>
        /// <param name="geomId"></param>
        public static void DeleteGeometry(int geomId)
        {
            try
            {
                ReferenceGeometry queriedGeom;
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    queriedGeom = conn.GetWithChildren<ReferenceGeometry>(geomId);

                    if (queriedGeom.status > -1)
                    {
                        queriedGeom.status = 3;
                        queriedGeom.timestamp = DateTime.Now;

                        foreach (var rec in queriedGeom.records)
                        {
                            rec.status = 3;
                            rec.timestamp = DateTime.Now;
                            conn.Update(rec);
                        }

                        conn.Update(queriedGeom);
                    }
                    else
                    {
                        conn.Delete(queriedGeom,true);
                    }
                    MessagingCenter.Send<Application>(App.Current, "RefreshGeometries");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Could not delete geometry" + e);
            }
        }
    }

}
