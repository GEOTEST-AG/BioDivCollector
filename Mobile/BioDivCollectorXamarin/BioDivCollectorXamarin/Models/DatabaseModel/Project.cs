using BioDivCollectorXamarin.Models.LoginModel;
using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using Xamarin.Forms;
using System.Threading.Tasks;
using Xamarin.Essentials;
using SQLiteNetExtensionsAsync.Extensions;

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    [Table("Project")]
    public class Project : ObservableClass
    {

        /// <summary>
        /// Project database definition
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string projectId { get; set; }


        public DateTime startDateTime { get; set; }
        public string projectName { get; set; }
        public string description { get; set; }
        public string projectNumber { get; set; }
        public string id_Extern { get; set; }
        public DateTime lastSync { get; set; }

        public int projectStatusId { get; set; }

        public string projectManager { get; set; }
        public string projectConfigurator { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<ReferenceGeometry> geometries { get; set; } = new List<ReferenceGeometry>();
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Record> records { get; set; } = new List<Record>();
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Form> forms { get; set; } = new List<Form>();
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Layer> layers { get; set; } = new List<Layer>();
        //  -ProjectLayer
        //  -UserLayer



        /// <summary>
        /// Create a project object
        /// </summary>
        public Project()
        {

        }

        /// <summary>
        /// Check if the project already exists in the sqlite database
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>Whether it exists</returns>
        public static async Task<bool> LocalProjectExists(string projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var projs = await conn.Table<Project>().Where(g => g.projectId != null).ToListAsync();

                foreach (var proj in projs)
                {
                    if (proj != null && proj.projectId == projectId)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
        /// <summary>
        /// Fetch a project from the database given its database id
        /// </summary>
        /// <param name="project_pk"></param>
        /// <returns>The project</returns>
        public static async Task<Project> FetchProject(string project_pk)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var projTableTest = await conn.Table<Project>().ToListAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await conn.CreateTableAsync<Project>();
            }

            try
            {
                var proj = await conn.Table<Project>().Where(Project => Project.projectId == project_pk.ToLower()).FirstOrDefaultAsync();
                return proj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Fetch the current project defined in the App.cs file
        /// </summary>
        /// <returns>The project</returns>
        public static async Task<Project> FetchCurrentProject()
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var projTableTest = await conn.Table<Project>().ToListAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await conn.CreateTableAsync<Project>();
            }

            try
            {
                var currentProjectId = Preferences.Get("currentProject", "");
                var proj = await conn.Table<Project>().Where(Project => Project.projectId == currentProjectId).FirstOrDefaultAsync();
                return proj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Fetch the project with all of the corresponding geometries and observations
        /// </summary>
        /// <param name="project_pk"></param>
        /// <returns>The whole project</returns>
        public static async Task<Project> FetchProjectWithChildren(string project_pk)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var projTableTest = await conn.Table<Project>().ToListAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await conn.CreateTableAsync<Project>();
            }

            try
            {
                var temp = await Project.FetchProject(project_pk);
                var proj = await conn.GetWithChildrenAsync<Project>(temp.Id, recursive: true);
                return proj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Count how many geometries are associated with a given project
        /// </summary>
        /// <param name="projId"></param>
        /// <returns>Geometry count</returns>
        public static async Task<int> FetchNumberOfGeometriesForProject(int projId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var geoms = await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == projId).CountAsync();
                return geoms;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }

        /// <summary>
        /// Count how many records (observations) are associated with a given project
        /// </summary>
        /// <param name="projId"></param>
        /// <returns>Record count</returns>
        public static async Task<int> FetchNumberOfRecordsForProject(int projId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var recs = await conn.Table<Record>().Where(Record => Record.project_fk == projId).CountAsync();

                return recs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }

        /// <summary>
        /// Delete the project given the simple project (that returned by the user call to the connector api) definition
        /// </summary>
        /// <param name="project"></param>
        /// <returns>Delete successful</returns>
        public static async Task<bool> DeleteProject(ProjectSimple project)
        {
            try
            {
                Project existingProject = await FetchProjectWithChildren(project.projectId);
                if (existingProject != null)
                {
                    var conn = App.ActiveDatabaseConnection;
                    await conn.DeleteAsync(existingProject, true);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        /// <summary>
        /// Delete a project given the project database entry
        /// </summary>
        /// <param name="project"></param>
        /// <returns>Delete successful</returns>
        public static async Task<bool> DeleteProject(string projectId)
        {
            try
            {
                Project existingProject = await FetchProjectWithChildren(projectId);
                var success = false;
                if (existingProject != null)
                {
                    var conn = App.ActiveDatabaseConnection;
                    await conn.DeleteAsync(existingProject, true);
                    success = true;
                }
                return success;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        /// <summary>
        /// Download a project from the connector given the project GUID
        /// </summary>
        /// <param name="projectId"></param>
        public static async Task DownloadProjectData(string projectId)
        {
                MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "Synchronisiert ...");
                //Project proj = await Project.FetchProject(projectId);

#if __IOS__
// iOS-specific code
            DataDAO.GetJsonStringForProject(projectId.ToUpper(), null);
#else
                // Android-specific code
                await DataDAO.GetJsonStringForProject(projectId, null);
#endif

        }

        /// <summary>
        /// Synchronise the project with the connector given the project GUID
        /// </summary>
        /// <param name="projectId"></param>
        public static async Task SynchroniseProjectData(string projectId)
        {
            MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "Synchronisiert ...");
            //Project proj = await Project.FetchProject(projectId);
            await DataDAO.SynchroniseDataForProject(projectId);
        }

        /// <summary>
        /// Check if the project has changes which need synchronising
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>Whether it has unsaved changes</returns>
        public static async Task<bool> ProjectHasUnsavedChanges(string projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                Project proj = await Project.FetchProject(projectId);
                var recs = await conn.Table<Record>().Where(Record => Record.project_fk == proj.Id).ToListAsync();
                foreach (Record rec in recs)
                {
                    if (rec.status == 2)
                    {
                        return true;
                    }
                }
                var geoms = await conn.Table<ReferenceGeometry>().Where(ReferenceGeometry => ReferenceGeometry.project_fk == proj.Id).ToListAsync();
                foreach (var geom in geoms)
                {
                    if (geom.status == 2)
                    {
                        return true;
                    }
                    var geomRecs = await conn.Table<Record>().Where(Record => Record.geometry_fk == geom.Id).Where(Record => Record.status == 2).ToListAsync();
                    if (geomRecs.Count > 0)
                    { return true; }
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
