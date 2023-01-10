using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;
using NetTopologySuite.IO;
using Mapsui.Projection;
using Mapsui.Geometries;
using ProjNet.CoordinateSystems;
using System.Threading.Tasks;
using SQLiteNetExtensionsAsync.Extensions;


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

        private string _geometryName;
        public string geometryName
        {
            get { return _geometryName; }
            set
            {
                _geometryName = value;
            }
        }

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
        public static async Task<ReferenceGeometry> GetGeometry(int geomId)
        {
            ReferenceGeometry queriedGeom;
            var conn = App.ActiveDatabaseConnection;
            //queriedGeom = await conn.GetAsync<ReferenceGeometry>(geomId);
            queriedGeom = await conn.Table<ReferenceGeometry>().Where(g => g.Id == geomId).FirstOrDefaultAsync();
            return queriedGeom;
        }

        /// <summary>
        /// Get geometry by guID geometry id
        /// </summary>
        /// <param name="geomId"></param>
        /// <returns></returns>
        public static async Task<ReferenceGeometry> GetGeometry(string geometryId)
        {
            ReferenceGeometry queriedGeom;
            var conn = App.ActiveDatabaseConnection;
            queriedGeom = await conn.Table<ReferenceGeometry>().Where(geom => geom.geometryId == geometryId).FirstOrDefaultAsync();
            return queriedGeom;
        }

        /// <summary>
        /// Get all geometries for the current project
        /// </summary>
        /// <returns></returns>
        public static async Task<List<ReferenceGeometry>> GetAllGeometries()
        {
            var queriedGeoms = new List<ReferenceGeometry>();
            var conn = App.ActiveDatabaseConnection;
            var currentProj = await conn.Table<Project>().Where(p => p.projectId == App.CurrentProjectId).FirstOrDefaultAsync();
            queriedGeoms = await conn.Table<ReferenceGeometry>().Where(g => g.project_fk == currentProj.Id).ToListAsync();
            return queriedGeoms;
        }

        /// <summary>
        /// Get a list of only the names of the geometries for the current project
        /// </summary>
        /// <returns></returns>
        public static async Task<List<string>> GetAllGeometryNames()
        {
            var queriedGeoms = new List<ReferenceGeometry>();
            var project = Project.FetchCurrentProject();
            var conn = App.ActiveDatabaseConnection;
            queriedGeoms = await conn.Table<ReferenceGeometry>().Where(geom => geom.project_fk == project.Id).ToListAsync();
            var geomNames = new List<string>();
            foreach (var geom in queriedGeoms)
            {
                geomNames.Add(geom.geometryName);
            }
            return geomNames;
        }

        /// <summary>
        /// Get all geometries for a specific project
        /// </summary>
        /// <returns></returns>
        public static async Task<List<ReferenceGeometry>> GetAllGeometriesByProjectId(int projectId)
        {
            var geoms = new List<ReferenceGeometry>();
            var conn = App.ActiveDatabaseConnection;
            geoms = await conn.Table<ReferenceGeometry>().Where(geom => geom.project_fk == projectId).Where(r => r.status != 3).ToListAsync();
            return geoms;
        }


        /// <summary>
        /// Save a geometry to the database given a list of coordinates and a name
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="name"></param>
        public static async Task<string> SaveGeometry(List<Mapsui.Geometries.Point> pointList, string name)
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
            var proj = await Project.FetchCurrentProject();
            geom.project_fk = proj.Id;

            var conn = App.ActiveDatabaseConnection;
            await conn.InsertAsync(geom);

            var project = await conn.GetWithChildrenAsync<Project>(proj.Id);
            project.geometries = await conn.Table<ReferenceGeometry>().Where(g => g.project_fk == proj.Id).ToListAsync();
            await conn.UpdateWithChildrenAsync(project);

            return geom.geometryId;
        }


        /// <summary>
        /// Update a geometry in the database given a list of coordinates
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="name"></param>
        public static async Task UpdateGeometry(List<Mapsui.Geometries.Point> pointList, int GeomId)
        {
            var conn = App.ActiveDatabaseConnection;
                var geom = await conn.GetWithChildrenAsync<ReferenceGeometry>(GeomId);
                geom.geometry = DataDAO.CoordinatesToGeoJSON(pointList);
                geom.userName = App.CurrentUser.userId;
                geom.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
                geom.timestamp = DateTime.Now;
                geom.status = 2;
                await conn.UpdateAsync(geom);
        }

        /// <summary>
        /// Update the geometry with any changes made
        /// </summary>
        /// <param name="geom"></param>
        public static void SaveGeometry(ReferenceGeometry geom)
        {
            var conn = App.ActiveDatabaseConnection;
                conn.UpdateAsync(geom);
        }


        /// <summary>
        /// Delete a geometry from the database (set its status to deleted) given its database geometry id
        /// </summary>
        /// <param name="geomId"></param>
        public static async Task DeleteGeometry(int geomId)
        {
            try
            {
                ReferenceGeometry queriedGeom;
                var conn = App.ActiveDatabaseConnection;
                queriedGeom = await conn.GetWithChildrenAsync<ReferenceGeometry>(geomId);

                if (queriedGeom.status > -1)
                {
                    queriedGeom.status = 3;
                    queriedGeom.timestamp = DateTime.Now;

                    foreach (var rec in queriedGeom.records)
                    {
                        rec.status = 3;
                        rec.timestamp = DateTime.Now;
                        await conn.UpdateAsync(rec);
                    }

                    await conn.UpdateAsync(queriedGeom);
                }
                else
                {
                    await conn.DeleteAsync(queriedGeom, true);
                }
                MessagingCenter.Send<Xamarin.Forms.Application>(App.Current, "ResetFilter");
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not delete geometry" + e);
            }
        }

        public static Coordinate[] TransformWGS84ToCH1903(Coordinate[] coordinates)
        {
            CoordinateTransformationFactory ctFact = new CoordinateTransformationFactory();

            var csWgs84 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;
            const string epsg21781 = "PROJCS[\"CH1903 / LV03\", GEOGCS[\"CH1903\",DATUM[\"CH1903\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\", \"7004\"]],TOWGS84[674.4, 15.1, 405.3, 0, 0, 0, 0],AUTHORITY[\"EPSG\", \"6149\"]],PRIMEM[\"Greenwich\", 0,AUTHORITY[\"EPSG\", \"8901\"]],UNIT[\"degree\", 0.0174532925199433,AUTHORITY[\"EPSG\", \"9122\"]],AUTHORITY[\"EPSG\", \"4149\"]],PROJECTION[\"Hotine_Oblique_Mercator_Azimuth_Center\"],PARAMETER[\"latitude_of_center\", 46.95240555555556], PARAMETER[\"longitude_of_center\", 7.439583333333333],PARAMETER[\"azimuth\", 90], PARAMETER[\"rectified_grid_angle\", 90], PARAMETER[\"scale_factor\", 1],PARAMETER[\"false_easting\", 600000],PARAMETER[\"false_northing\", 200000],UNIT[\"metre\", 1,AUTHORITY[\"EPSG\", \"9001\"]],AXIS[\"Y\", EAST],AXIS[\"X\", NORTH],AUTHORITY[\"EPSG\", \"21781\"]]";
            var cs21781 = ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(epsg21781) as CoordinateSystem;

            ICoordinateTransformation trans = ctFact.CreateFromCoordinateSystems(csWgs84, cs21781);


            var pointsq = coordinates.Select(c => new double[] { c.X, c.Y }).ToList();

            Coordinate[] tpoints = trans.MathTransform.TransformList(pointsq).Select(c => new Coordinate(c[0], c[1])).ToArray();

            return tpoints;
        }

        public static double CalculateAreaOfPolygon(ReferenceGeometry geom)
        {
            var geometry = DataDAO.GeoJSON2Geometry(geom.geometry);
            var tpoints = ReferenceGeometry.TransformWGS84ToCH1903(geometry.Coordinates);

            var polygon = new Mapsui.Geometries.Polygon();
            polygon.ExteriorRing.Vertices = tpoints.Select(c => new Mapsui.Geometries.Point(c.X, c.Y)).ToArray();
            return polygon.Area;
        }

        public static double CalculateLengthOfLine(ReferenceGeometry geom)
        {
            var geometry = DataDAO.GeoJSON2Geometry(geom.geometry);
            var tpoints = ReferenceGeometry.TransformWGS84ToCH1903(geometry.Coordinates);

            var line = new Mapsui.Geometries.LineString();
            line.Vertices = tpoints.Select(c => new Mapsui.Geometries.Point(c.X, c.Y)).ToArray();
            return line.Length;
        }

        public static string FindGeometryTypeFromCoordinateList(List<Mapsui.Geometries.Point>coordList)
        {
            var start = coordList[0];
            var end = coordList[coordList.Count - 1];
            if (coordList.Count == 1)
            {
                return "Punkt";
            }
            else if (start.X == end.X && start.Y == end.Y)
            {
                return "Polygon";
            }
            else
            {
                return "Linie";
            }
        }

        public void ChangeGeometryName(string name)
        {
            this.geometryName = name;
            this.timestamp = DateTime.Now;
            if (this.status != -1)
            {
                this.status = 2;
            }
            ReferenceGeometry.SaveGeometry(this);
        }
    }

}
