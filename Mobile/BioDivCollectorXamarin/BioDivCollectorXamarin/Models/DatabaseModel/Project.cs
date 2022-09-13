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

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    [Table ("Project")]
    public class Project:ObservableClass
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
        public static bool LocalProjectExists(string projectId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var projs = conn.Table<Project>().Select(g => g.projectId);

                    foreach (var proj in projs)
                    {
                        if (proj != null && proj == projectId)
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
        }
        /// <summary>
        /// Fetch a project from the database given its database id
        /// </summary>
        /// <param name="project_pk"></param>
        /// <returns>The project</returns>
        public static Project FetchProject(string project_pk)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var projTableTest = conn.Table<Project>().ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    conn.CreateTable<DatabaseModel.Project>();
                }

                try
                {
                    var proj = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == project_pk.ToLower()).FirstOrDefault();
                    return proj;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


            }
            return null;
        }

        /// <summary>
        /// Fetch the current project defined in the App.cs file
        /// </summary>
        /// <returns>The project</returns>
        public static Project FetchCurrentProject()
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var projTableTest = conn.Table<Project>().ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    conn.CreateTable<DatabaseModel.Project>();
                }

                try
                {
                    var currentProjectId = Preferences.Get("currentProject", "");
                    var proj = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == currentProjectId).FirstOrDefault();
                    return proj;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


            }
            return null;
        }

        /// <summary>
        /// Fetch the project with all of the corresponding geometries and observations
        /// </summary>
        /// <param name="project_pk"></param>
        /// <returns>The whole project</returns>
        public static Project FetchProjectWithChildren(string project_pk)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var projTableTest = conn.Table<Project>().ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    conn.CreateTable<DatabaseModel.Project>();
                }

                try
                {
                    var temp = Project.FetchProject(project_pk);
                    var proj = conn.GetWithChildren<Project>(temp.Id, recursive: true);
                    return proj;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


            }
            return null;
        }

        /// <summary>
        /// Count how many geometries are associated with a given project
        /// </summary>
        /// <param name="projId"></param>
        /// <returns>Geometry count</returns>
        public static int FetchNumberOfGeometriesForProject(int projId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var geoms = conn.Table<ReferenceGeometry>().Select(g => g).Where(ReferenceGeometry => ReferenceGeometry.project_fk == projId).Count();
                    return geoms;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return 0;
        }

        /// <summary>
        /// Count how many records (observations) are associated with a given project
        /// </summary>
        /// <param name="projId"></param>
        /// <returns>Record count</returns>
        public static int FetchNumberOfRecordsForProject(int projId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {

                try
                {
                    var recs = conn.Table<Record>().Select(g => g).Where(Record => Record.project_fk == projId).Count();

                    return recs;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return 0;
        }

        /// <summary>
        /// Delete the project given the simple project (that returned by the user call to the connector api) definition
        /// </summary>
        /// <param name="project"></param>
        /// <returns>Delete successful</returns>
        public static bool DeleteProject(ProjectSimple project)
        {
           try
            {
                Project existingProject = FetchProjectWithChildren(project.projectId);
                if (existingProject != null)
                {
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {
                        conn.Delete(existingProject,true);
                    }
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
        public static bool DeleteProject(string projectId)
        {
            try
            {
                Project existingProject = FetchProjectWithChildren(projectId);
                var success = false;
                if (existingProject != null)
                {
                    using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                    {
                        conn.Delete(existingProject,true);
                        success = true;
                    }
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
            await Task.Run(() =>
            {
                MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "Synchronisiert ...");
                Project proj = Project.FetchProject(projectId);

#if __IOS__
// iOS-specific code
            DataDAO.GetJsonStringForProject(projectId.ToUpper(), null);
#else
                // Android-specific code
                DataDAO.GetJsonStringForProject(projectId, null);
#endif
            });

        }

        /// <summary>
        /// Synchronise the project with the connector given the project GUID
        /// </summary>
        /// <param name="projectId"></param>
        public static void SynchroniseProjectData(string projectId)
        {
            MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "Synchronisiert ...");
            Project proj = Project.FetchProject(projectId);
            DataDAO.SynchroniseDataForProject(projectId);
        }
        
        /// <summary>
        /// Check if the project has changes which need synchronising
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>Whether it has unsaved changes</returns>
        public static bool ProjectHasUnsavedChanges(string projectId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {

                try
                {
                    Project proj = Project.FetchProject(projectId);
                    var recs = conn.Table<Record>().Select(g => g).Where(Record => Record.project_fk == proj.Id);
                    foreach (Record rec in recs)
                    {
                        if (rec.status == 2)
                        {
                            return true;
                        }
                    }
                    var geoms = conn.Table<ReferenceGeometry>().Select(g => g).Where(ReferenceGeometry => ReferenceGeometry.project_fk == proj.Id).ToList();
                    foreach (var geom in geoms)
                    {
                        if (geom.status == 2)
                        {
                            return true;
                        }
                        var geomRecs = conn.Table<Record>().Select(g => g).Where(Record => Record.geometry_fk == geom.Id).Where(Record => Record.status == 2).ToList();
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
}
