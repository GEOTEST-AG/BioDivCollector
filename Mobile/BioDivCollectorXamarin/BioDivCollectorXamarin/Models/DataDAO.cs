using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Models.LoginModel;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SQLite;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.Models
{
    public class DataDAO
    {

        /// <summary>
        /// Downloads the json from the connector for a particular project id. Includes authorisation.
        /// </summary>
        /// <param name="projectId">Specifies project</param>
        /// <param name="time">Specifies the earliest time from which the changes should be downloaded</param>
        public static void GetJsonStringForProject(string projectId, string time)
        {
            //Refresh token, then synchronise
            var auth = Authentication.AuthParams;
            auth.ShowErrors = false;
            auth.AllowCancel = false;

            auth.Completed += async (sender, eventArgs) =>
            {
                if (eventArgs.IsAuthenticated == true)
                {
                    Dictionary<String, String> props = eventArgs.Account.Properties;

                    Authentication.SaveTokens(props);

                    string url = "";
                    if (time != null && time != "")
                    {
                        url = App.ServerURL + "/api/Project/" + projectId + "/" + time;
                    }
                    else
                    {
                        url = App.ServerURL + "/api/Project/" + projectId;
                    }

                    var through = false;
                    try
                    {
                        var json = "";
                        using (HttpClient client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromSeconds(600); // 10 minutes
                            var token = Preferences.Get("AccessToken", "");
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                            //MessagingCenter.Send(new DataDAO(), "SyncMessage", "Waiting for data");
                            var response = await client.GetAsync(url);
                            var jsonbytes = await response.Content.ReadAsByteArrayAsync();
                            var utf8 = Encoding.UTF8;
                            json = utf8.GetString(jsonbytes, 0, jsonbytes.Length);

                            through = true; // Check that we got through all the code
                        }

                        string success = "false";
                        
                        if (json.ToLower() == "error downloading data")
                        {
                            MessagingCenter.Send(new Project(), "DataDownloadError", json);
                        }

                        if (json.ToLower() != "error downloading data" && json.ToLower() != "error parsing data")
                        {
                            
                            success = DataDAO.GetProjectDataFromJSON(json);
                            App.CurrentProjectId = projectId;
                            App.SetProject(projectId);
                            ShowSyncCompleteMessage(success);
                            MessagingCenter.Send(new Project(), "DataDownloadSuccess", success);
                        }

                        MessagingCenter.Send(Application.Current, "DownloadComplete",json);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        if (through == false)
                        {
                            MessagingCenter.Send(Application.Current, "DownloadComplete", "Error Downloading Data");
                            MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "");
                        }
                    }
                    
                }
                else
                {
                    MessagingCenter.Send<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "LoginUnsuccessful");
                }


            };

            auth.Error += (sender, eventArgs) =>
            {
                //Careful! This triggers on the iPhone even when login is successful
            };

            auth.RequestRefreshTokenAsync(Preferences.Get("RefreshToken", ""));
        }

        /// <summary>
        /// Synchronises the specified project: Uploads data which do not yet exist on the server, then downloads all changes since the last synchronisation.
        /// This includes authorisation
        /// </summary>
        /// <param name="projectId"></param>
        public async static void SynchroniseDataForProject(string projectId)
        {

            //Refresh token, then synchronise
            var auth = Authentication.AuthParams;
            auth.ShowErrors = false;
            auth.AllowCancel = false;

            auth.Completed += async (sender, eventArgs) =>
            {

                if (eventArgs.IsAuthenticated == true)
                {
                    Dictionary<String, String> props = eventArgs.Account.Properties;
                    Authentication.SaveTokens(props);

                    string time = "0000-01-01T00:00:00";
                    var lastSync = DateTime.Now;
                    DateTime.TryParse(time, out lastSync);
                    var project = Project.FetchProject(projectId);
                    if (project.lastSync != null)
                    {
                        lastSync = project.lastSync.ToUniversalTime();
                        time = lastSync.ToString("yyyy-MM-ddTHH:mm:ss" + "Z");
                    }
                    var json = DataDAO.PrepareJSONForUpload(lastSync); // Prepare data for upload

                    string url = App.ServerURL + "/api/Project/" + time + "?iamgod=true";

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromSeconds(6000); // 10 minutes
                            var token = Preferences.Get("AccessToken", "");
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                            MessagingCenter.Send(new DataDAO(), "SyncMessage", "Waiting for data");
                            var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));  //UPLOAD
                            var jsonbytes = await response.Content.ReadAsByteArrayAsync();
                            var utf8 = Encoding.UTF8;
                            var jsonResponse = utf8.GetString(jsonbytes, 0, jsonbytes.Length); //Response contains updates to data since last sync

                            var settings = new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                StringEscapeHandling = StringEscapeHandling.EscapeHtml
                            };
                            var returnedObject = JsonConvert.DeserializeObject<ProjectSyncDTO>(jsonResponse, settings);  //Deserialise response


                            if (returnedObject.success == true)
                            {
                                var photoUploadResponse = await PrepareBinaryRecordsForUpload(lastSync); //Upload binary objects
                                if (photoUploadResponse != String.Empty)
                                {
                                    await App.Current.MainPage.DisplayAlert("Foto Upload", photoUploadResponse, "OK");
                                }

                                DataDAO.ProcessJSON(returnedObject.projectUpdate); //Update database with downloaded data

                                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                                {
                                    project.lastSync = DateTime.Now;
                                    conn.Update(project);

                                    var error = returnedObject.error;
                                    var deletedRecords = returnedObject.records.deleted;
                                    var deletedRecords2 = returnedObject.geometries.geometryRecords.deleted;
                                    var deletedRecordsTotal = deletedRecords.Concat(deletedRecords2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                                    var skippedRecords = returnedObject.records.skipped;
                                    var skippedRecords2 = returnedObject.geometries.geometryRecords.skipped;
                                    var skippedRecordsTotal = skippedRecords.Concat(skippedRecords2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                                    var deletedGeometries = returnedObject.geometries.deleted;
                                    var skippedGeometries = returnedObject.geometries.skipped;

                                    if (error == null || error == String.Empty)
                                    {
                                        error = "Data successfully downloaded";
                                    }

                                    foreach (var deletedRecord in deletedRecordsTotal)
                                    {
                                        //Delete records from device which have been confirmed by the connector as 'deleted' in the central db
                                        var uid = deletedRecord.Key.ToString();
                                        var queriedRec = conn.Table<Record>().Where(r => r.recordId == uid).FirstOrDefault();
                                        conn.Delete(queriedRec,true); 
                                    }
                                    foreach (var deletedGeometry in deletedGeometries)
                                    {
                                        //Delete geometries from device which have been confirmed by the connector as 'deleted' in the central db
                                        var uid = deletedGeometry.Key.ToString();
                                        var queriedGeom = conn.Table<ReferenceGeometry>().Where(g => g.geometryId == uid).FirstOrDefault();
                                        conn.Delete(queriedGeom,true);
                                    }

                                    foreach (var skippedGeom in skippedGeometries)
                                    {
                                        if (!skippedGeom.Value.Contains("Changes were made to the associated records"))
                                        {
                                            error = error + System.Environment.NewLine;
                                            error = error + skippedGeom.Key.ToString() + ", " + skippedGeom.Value;
                                        }
                                    }

                                    foreach (var skippedRec in skippedRecordsTotal)
                                    {
                                        error = error + System.Environment.NewLine;
                                        error = error + skippedRec.Key.ToString() + ", " + skippedRec.Value;
                                    }

                                    ShowSyncCompleteMessage(error); //Show any errors in the sync confirmation message
                                }
                            }
                            else
                            {
                                MessagingCenter.Send(new Project(), "DataDownloadError", @"Error synchronising data");
                            }
                        }
                        MessagingCenter.Send(new Project(), "DataDownloadError", "Data successfully synchronised");
                        App.SetProject(projectId);
                        MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "");
                    }
                    catch (Exception e)
                    {
                        // Re Log in
                        Console.WriteLine(e);
                        MessagingCenter.Send(new Project(), "DataDownloadError", @"Error synchronising data");
                        App.Current.MainPage = Login.GetPageToView();
                    }
                }
                else
                {

                    MessagingCenter.Send(new Project(), "DataDownloadError", @"Login failed");
                }


            };

            auth.Error += (sender, eventArgs) =>
            {
                //Careful! This triggers on the iPhone even when login is successful
            };

            await auth.RequestRefreshTokenAsync(Preferences.Get("RefreshToken", ""));

        }

        /// <summary>
        /// Deserialise the json returned from the connector and update the database with the parameters read
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string GetProjectDataFromJSON(string json)
        {
            try
            {
                //Parse JSON
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                };
                var projectRoot = JsonConvert.DeserializeObject<Project>(json, settings);

                DataDAO.ProcessJSON((Project)projectRoot);

                return "Data successfully downloaded";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "Error parsing data" + e;
            }

        }


        /// <summary>
        /// Update the database with the deserialised json returned from the connector
        /// </summary>
        /// <param name="projectRoot"></param>
        public static void ProcessJSON(Project projectRoot)
        {
            var binaryDownloadList = new List<Tuple<string, int?>>();
            //Insert JSON into database
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {

                if (projectRoot != null)
                {

                    MessagingCenter.Send(new DataDAO(), "SyncMessage", "Creating project");
                    try
                    {
                        //Test to see if database schema exists
                        var projTableTest = conn.Table<Project>().Select(g => g).FirstOrDefault();
                        var projTableTest2 = conn.Table<ReferenceGeometry>().Select(g => g).FirstOrDefault();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        conn.CreateTable<Project>();
                        conn.CreateTable<ReferenceGeometry>();
                        conn.CreateTable<Record>();
                        conn.CreateTable<TextData>();
                        conn.CreateTable<NumericData>();
                        conn.CreateTable<BooleanData>();
                        conn.CreateTable<BinaryData>();
                        conn.CreateTable<Layer>();
                        conn.CreateTable<Form>();
                        conn.CreateTable<FormField>();
                        conn.CreateTable<FieldChoice>();
                    }

                    try
                    {
                        PerformStandardValueMigration(conn);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    try
                    {
                        //Add project
                        var projNew = projectRoot as Project;
                        projNew.lastSync = DateTime.Now;
                        var existingProject = conn.Table<Project>().Where(p => p.projectId == projNew.projectId).FirstOrDefault();
                        if (existingProject == null)
                        {
                            conn.Insert(projNew);
                        }
                        else
                        {
                            conn.Update(projNew);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        var project = Project.FetchProject(projectRoot.projectId);
                        foreach (var geom in projectRoot.geometries)
                        {
                            try
                            {
                                var existinggeom = geom;
                                geom.project_fk = project.Id;
                                 var exgeom = conn.Table<ReferenceGeometry>().Select(g => g).Where(ReferenceGeometry => ReferenceGeometry.geometryId == geom.geometryId).FirstOrDefault();

                                if (exgeom != null)
                                {
                                    existinggeom = conn.GetWithChildren<ReferenceGeometry>(exgeom.Id);
                                    var id = existinggeom.Id;
                                    existinggeom = geom;
                                    existinggeom.Id = id;
                                    existinggeom.status = geom.status;
                                    conn.Update(existinggeom);
                                }
                                else if (geom.status != 3)
                                {
                                    conn.Insert(geom);
                                }
                                //Geometry related records
                                foreach (var rec in geom.records)
                                {
                                    try
                                    {
                                        var existingrec = conn.Table<Record>().Select(g => g).Where(Record => Record.recordId == rec.recordId).FirstOrDefault();

                                        if (existingrec != null)
                                        {
                                            var id = existingrec.Id;
                                            existingrec = rec;
                                            existingrec.project_fk = project.Id;
                                            existingrec.Id = id;
                                            existingrec.status = rec.status;
                                            existingrec.geometry_fk = geom.Id;
                                            conn.Update(existingrec);
                                        }
                                        else if (rec.status != 3)
                                        {
                                            rec.project_fk = project.Id;
                                            conn.Insert(rec);
                                        }
                                        
                                        if (rec.status != 3)
                                        {
                                            try
                                            {
                                                //Add text records
                                                foreach (var txt in rec.texts)
                                                {
                                                    if (txt.title == null)
                                                    { txt.title = ""; }
                                                    try
                                                    {
                                                        conn.InsertOrReplace(txt);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                    }
                                                }
                                                //Add numeric records
                                                foreach (var num in rec.numerics)
                                                {
                                                    if (num.title == null)
                                                    { num.title = ""; }
                                                    try
                                                    {
                                                        conn.InsertOrReplace(num);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                    }
                                                }
                                                //Add boolean records
                                                foreach (var onebool in rec.booleans)
                                                {
                                                    if (onebool.title == null)
                                                    { onebool.title = ""; }
                                                    try
                                                    {
                                                        conn.InsertOrReplace(onebool);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                    }
                                                }
                                                //Add binary records
                                                foreach (var binary in rec.binaries)
                                                {
                                                    try
                                                    {
                                                        conn.InsertOrReplace(binary);
                                                        binaryDownloadList.Add(new Tuple<string, int?>(rec.recordId, binary.formFieldId));
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }

                                        rec.texts = rec.texts;
                                        rec.numerics = rec.numerics;
                                        rec.booleans = rec.booleans;
                                        conn.UpdateWithChildren(rec);
                                        Console.WriteLine("Added record children: " + DateTime.Now.ToLongTimeString());
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }

                                    conn.InsertOrReplaceAllWithChildren(geom.records);
                                    conn.UpdateWithChildren(existinggeom);
                                    Console.WriteLine("Added record: " + DateTime.Now.ToLongTimeString());
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        // Add project related records
                        foreach (var rec in projectRoot.records)
                        {
                            try
                            {
                                rec.project_fk = project.Id;
                                conn.InsertOrReplaceWithChildren(rec);
                                

                                if (rec.status != 3)
                                {
                                    try
                                    {
                                        //Add text records
                                        foreach (var txt in rec.texts)
                                        {
                                            if (txt.title == null)
                                            { txt.title = ""; }
                                            try
                                            {
                                                var existingtxt = conn.Table<TextData>().Select(g => g).Where(TextData => TextData.textId == txt.textId).FirstOrDefault();
                                                if (existingtxt != null)
                                                {
                                                    var id = existingtxt.Id;
                                                    existingtxt = txt;
                                                    existingtxt.Id = id;
                                                    conn.Update(existingtxt);
                                                }
                                                else
                                                {
                                                    conn.Insert(txt);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }
                                        //Add numeric records
                                        foreach (var num in rec.numerics)
                                        {
                                            if (num.title == null)
                                            { num.title = ""; }
                                            try
                                            {
                                                var existingnum = conn.Table<NumericData>().Select(g => g).Where(NumericData => NumericData.numericId == num.numericId).FirstOrDefault();
                                                if (existingnum != null)
                                                {
                                                    var id = existingnum.Id;
                                                    existingnum = num;
                                                    existingnum.Id = id;
                                                    conn.Update(existingnum);
                                                }
                                                else
                                                {
                                                    conn.Insert(num);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }
                                        //Add boolean records
                                        foreach (var onebool in rec.booleans)
                                        {
                                            if (onebool.title == null)
                                            { onebool.title = ""; }
                                            try
                                            {
                                                var existingbool = conn.Table<BooleanData>().Select(g => g).Where(BooleanData => BooleanData.booleanId == onebool.booleanId).FirstOrDefault();
                                                if (existingbool != null)
                                                {
                                                    var id = existingbool.Id;
                                                    existingbool = onebool;
                                                    existingbool.Id = id;
                                                    conn.Update(existingbool);
                                                }
                                                else
                                                {
                                                    conn.Insert(onebool);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }
                                        //Add binary records
                                        foreach (var bin in rec.binaries)
                                        {
                                            try
                                            {
                                                var existingbin = conn.Table<BinaryData>().Select(g => g).Where(BinData => BinData.binaryId == bin.binaryId).FirstOrDefault();
                                                if (existingbin != null)
                                                {
                                                    var id = existingbin.Id;
                                                    existingbin = bin;
                                                    existingbin.Id = id;
                                                    conn.Update(existingbin);
                                                }
                                                else
                                                {
                                                    conn.Insert(bin);
                                                }
                                                conn.InsertOrReplace(bin);
                                                binaryDownloadList.Add(new Tuple<string, int?>(rec.recordId, bin.formFieldId));
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
                                }

                                var queriedrec = rec;
                                queriedrec.texts = rec.texts;
                                queriedrec.numerics = rec.numerics;
                                queriedrec.booleans = rec.booleans;
                                queriedrec.binaries = rec.binaries;
                                conn.UpdateWithChildren(queriedrec);
                                Console.WriteLine("Added record children: " + DateTime.Now.ToLongTimeString());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        // Add project related forms
                        foreach (var form in projectRoot.forms)
                        {
                            try
                            {
                                form.project_fk = project.Id;
                                var existingform = conn.Table<Form>().Select(g => g).Where(Form => Form.formId == form.formId).Where(Form => Form.project_fk == project.Id).FirstOrDefault();
                                if (existingform != null)
                                {
                                    //Delete the full form and replace it
                                    var fullForm = conn.GetWithChildren<Form>(existingform.Id,true);
                                    conn.Delete(fullForm);
                                }
                                if (form.status < 3)
                                {
                                    try
                                    {
                                        conn.Insert(form);
                                    }
                                    catch (Exception e)
                                    {
                                        if (e.Message == "table Form has no column named status")
                                        {
                                            //MIGRATE DB - Add status
                                            try
                                            {
                                                conn.BeginTransaction();
                                                conn.Execute("ALTER TABLE Form ADD status int NOT NULL DEFAULT(0);");
                                                conn.Commit();
                                                conn.Insert(form);
                                            }
                                            catch (Exception e2)
                                            {
                                                App.Current.MainPage.DisplayAlert("Wir sind auf ein Problem gestossen", "Ihr Formular konnte nicht synchronisiert werden. Bitte kontaktieren Sie den Support.", "Ok");
                                            }
                                        }

                                    }
                                }

                                //Add form fields

                                foreach (var formfield in form.formFields)
                                {
                                    try
                                    {
                                        formfield.form_fk = form.Id;
                                        conn.Insert(formfield);

                                        //Add field choices
                                        foreach (var fieldChoice in formfield.fieldChoices)
                                        {
                                            try
                                            {
                                               fieldChoice.formField_fk = formfield.Id;
                                               conn.Insert(fieldChoice);
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }
                                        var queriedfield = conn.Table<FormField>().Select(g => g).Where(FormField => FormField.fieldId == formfield.fieldId).Where(FormField => FormField.form_fk == formfield.form_fk).FirstOrDefault();
                                        queriedfield.fieldChoices = formfield.fieldChoices;
                                        conn.UpdateWithChildren(queriedfield);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
                                }
                                var queriedform = conn.Table<Form>().Select(g => g).Where(Form => Form.formId == form.formId).Where(Form => Form.project_fk == project.Id).FirstOrDefault();
                                queriedform.formFields = form.formFields;
                                conn.UpdateWithChildren(queriedform);
                                Console.WriteLine("Added form: " + DateTime.Now.ToLongTimeString());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        //Add project related layers
                        try
                        {
                            //Delete existing layers (need to delete all layers to start, as we are not informed when a layer is removed from the project)
                            var existingLayers = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.project_fk == project.Id);
                            foreach (var existingLayer in existingLayers)
                            {
                                conn.Delete(existingLayer);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                        //add new layers
                        foreach (var layer in projectRoot.layers)
                        {
                            try
                            {
                                layer.project_fk = project.Id;
                                var existingLayer = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.layerId == layer.layerId).FirstOrDefault();
                                if (existingLayer != null)
                                {
                                    conn.Delete(existingLayer);
                                }
                                conn.Insert(layer);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        try
                        {
                            project.geometries = conn.Table<ReferenceGeometry>().Select(g => g).Where(g => g.project_fk == project.Id).ToList();
                            project.records = conn.Table<Record>().Select(g => g).Where(g => g.project_fk == project.Id).ToList();
                            project.forms = conn.Table<Form>().Select(g => g).Where(g => g.project_fk == project.Id).ToList();
                            project.layers = conn.Table<Layer>().Select(g => g).Where(g => g.project_fk == project.Id).ToList();
                            conn.UpdateWithChildren(project);
                            Console.WriteLine("Added records geometries and layers: " + DateTime.Now.ToLongTimeString());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }

            foreach (var tuple in binaryDownloadList)
            {
                Task.Run(async () => await BinaryData.DownloadBinaryData(tuple.Item1, tuple.Item2));
            }

            MessagingCenter.Send<Application, string>(Application.Current, "SyncMessage", "");
        }

        /// <summary>
        /// Add a standard value column to the form field table if it does not exist
        /// </summary>
        /// <param name="conn"></param>
        private static void PerformStandardValueMigration(SQLiteConnection conn)
        {
            //Check for column and migrate
            var tableInfo = conn.GetTableInfo("FormField");
            var columnExists = tableInfo.Any(x => x.Name.Equals("standardValue"));
            if (!columnExists)
            {
                var f = conn.CreateCommand("ALTER TABLE [FormField] ADD COLUMN standardValue NTEXT");
                var l = f.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Shape is a customised Geometry with title and id added so that it can be used in a list
        /// </summary>
        public class Shape
        {
            public string title { get; set; }
            public int geomId { get; set; }
            public Geometry shapeGeom { get; set; }
        }

        /// <summary>
        /// Queries the geometries for a project from the database and processes the into a list of Shape objects
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>List of shape objects</returns>
        public static List<Shape> getDataForMap(string projectId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var proj = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == projectId).FirstOrDefault();
                    var items = conn.Table<ReferenceGeometry>().Select(g => g).Where(ReferenceGeometry => ReferenceGeometry.project_fk == proj.Id).Where(ReferenceGeometry => ReferenceGeometry.status != 3);
                    var geoms = new List<Shape>();
                    foreach (ReferenceGeometry geom in items)
                    {
                        var geometry = GeoJSON2Geometry(geom.geometry);
                        if (geometry.IsValid)
                        {
                            var shape = new Shape
                            {
                                title = geom.geometryName,
                                geomId = geom.Id,
                                shapeGeom = geometry
                            };
                            geoms.Add(shape);
                        }

                    }
                    return geoms;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {

                }
            }
            return null;
        }

        /// <summary>
        /// Serialises Geometry objects to GeoJSON
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns>GeoJSON</returns>
        //https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON
        public static string Geometry2GeoJSON(Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            string geoJson;

            var serializer = GeoJsonSerializer.Create();
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, geometry);
                geoJson = stringWriter.ToString();
            }
            return geoJson;
        }

        /// <summary>
        /// Deserialises GeoJSON into a geometry object
        /// </summary>
        /// <param name="geoJson"></param>
        /// <returns>Geometry</returns>
        public static Geometry GeoJSON2Geometry(string geoJson)
        {
            if (string.IsNullOrWhiteSpace(geoJson))
                throw new ArgumentNullException(nameof(geoJson));

            Geometry geometry;

            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(geoJson))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                geometry = serializer.Deserialize<Geometry>(jsonReader);
            }
            return geometry;
        }

        /// <summary>
        /// Converts from Mapsui Geometry type to NetTopologySuite Geometry type, then converts to GeoJSON.
        /// Writes the point list to wkt format, the uses Geometry2GeoJSON to convert to geojson
        /// </summary>
        /// <param name="pointList"></param>
        /// <returns>GeoJSON</returns>
        public static string CoordinatesToGeoJSON(List<Mapsui.Geometries.Point> pointList)
        {
            var wkt = "";
            if (pointList.Count == 1)
            {
                var point = pointList[0];
                wkt = Mapsui.Geometries.WellKnownText.GeometryToWKT.Write(point);
            }
            else if (pointList[0] == pointList[pointList.Count - 1])
            {
                var polygon = new Mapsui.Geometries.Polygon();

                foreach (var coord in pointList)
                {
                    polygon.ExteriorRing.Vertices.Add(new Mapsui.Geometries.Point(coord.X, coord.Y));
                }
                wkt = Mapsui.Geometries.WellKnownText.GeometryToWKT.Write(polygon);
            }
            else
            {
                var line = new Mapsui.Geometries.LineString(pointList);
                wkt = Mapsui.Geometries.WellKnownText.GeometryToWKT.Write(line);
            }

            WKTReader reader = new WKTReader();
            NetTopologySuite.Geometries.Geometry geom = reader.Read(wkt);
            var geojson = DataDAO.Geometry2GeoJSON(geom);
            return geojson;
        }


        /// <summary>
        /// Queries a list of layers available for a particular project
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns>List of layers</returns>
        public static List<Layer> GetLayersForMap(string projectId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                try
                {
                    var proj = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == projectId).FirstOrDefault();
                    var layers = conn.Table<Layer>().Select(g => g).Where(Layer => Layer.project_fk == proj.Id).ToList();
                    return layers;
                }
                catch
                {
                    return new List<Layer>();
                }
            }
        }

        /// <summary>
        /// Queries all records from a project, filters the geometries and records by date, and serialises everything to json
        /// </summary>
        /// <param name="lastSync"></param>
        /// <returns>Json object</returns>
        public static string PrepareJSONForUpload(DateTime lastSync)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var proj = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == App.CurrentProjectId).FirstOrDefault();
                if (proj != null)
                {
                    var project = conn.GetWithChildren<Project>(proj.Id, true);
                    var geoms = project.geometries;
                    foreach (var geometry in geoms)
                    {
                        var records = geometry.records.Where(x => x.status != 1).Where(x => x.timestamp.ToUniversalTime() > lastSync.ToUniversalTime()).ToList();
                        geometry.records = records;
                    }

                    var geometries = (from g in geoms
                                      where (g.status != 1 || g.records.Count > 0)
                                      select g).ToList();
                    project.geometries = geometries;

                    var recs = project.records.Where(x => x.status != 1).Where(x => x.geometry_fk == null).ToList();
                    project.records = recs;

                    project.forms = null;
                    project.layers = null;

                    return JsonConvert.SerializeObject(project);
                }
                return String.Empty;
            }
        }

        public static async Task<string> PrepareBinaryRecordsForUpload(DateTime lastSync)
        {
            var responsetext = string.Empty;
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var proj = conn.Table<Project>().Select(g => g).Where(Project => Project.projectId == App.CurrentProjectId).FirstOrDefault();
                if (proj != null)
                {
                    var project = Project.FetchProjectWithChildren(proj.projectId);

                    var geomRecs = project.geometries.SelectMany(x => x.records).Where(x => x.status != 1).Where(x => x.timestamp.ToUniversalTime() > lastSync.ToUniversalTime()).ToList();

                    var projrecs = project.records.Where(x => x.status != 1).Where(x => x.geometry_fk == null).ToList();
                    var allrecs = geomRecs.Concat(projrecs);

                    var bins = allrecs.SelectMany(x => x.binaries).ToList();
                    var binIds = bins.Select(x => x.binaryId).ToList();
                    foreach (var binId in binIds)
                    {
                        try
                        {
                            var directory = DependencyService.Get<FileInterface>().GetImagePath();
                            string filepath = Path.Combine(directory, binId + ".jpg");
                            var binData = File.ReadAllBytes(filepath);

                            string url = App.ServerURL + "/api/Binary/" + binId;

                            using (HttpClient client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(6000); // 10 minutes
                                var token = Preferences.Get("AccessToken", "");
                                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                                MessagingCenter.Send(new DataDAO(), "SyncMessage", "Uploading Photos");


                                using (var multipartFormContent = new MultipartFormDataContent())
                                {
                                    //Load the file and set the file's Content-Type header
                                    var fileStreamContent = new StreamContent(File.OpenRead(filepath));
                                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpg");

                                    //Add the file
                                    multipartFormContent.Add(fileStreamContent, name: "file", fileName: binId + ".jpg");

                                    //Send it
                                    var response = await client.PostAsync(url, multipartFormContent);
                                    response.EnsureSuccessStatusCode();
                                    responsetext = await response.Content.ReadAsStringAsync();
                                }
                            }
                        }
                        catch
                        {

                        }

                    }
                }
            }
            return responsetext;
        }


        /// <summary>
        /// Show sync message
        /// </summary>
        /// <param name="message"></param>
        public static void ShowSyncCompleteMessage(string message)
        {
            if (message == "Data successfully downloaded")
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    App.Current.MainPage.DisplayAlert("Synchronisierung erfolgreich", "", "OK");
                });
            }
            else if (message.Contains("Data successfully downloaded"))
            {
                message.Replace("Data successfully downloaded", String.Empty);
                Device.BeginInvokeOnMainThread(() =>
                {
                    App.Current.MainPage.DisplayAlert("Synchronisierung erfolgreich", message, "OK");
                });
            }

            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    App.Current.MainPage.DisplayAlert("", message, "OK");
                });
            }
        }
    }

    /// <summary>
    /// Required project JSON structure for connector
    /// </summary>
    public class ProjectSyncDTO
    {
        public bool success { get; set; }
        public string error { get; set; }
        public Guid projectId { get; set; }
        public RecordsSyncDTO records { get; set; } = new RecordsSyncDTO();
        public GeometriesSyncDTO geometries { get; set; } = new GeometriesSyncDTO();
        public Project projectUpdate { get; set; }
    }

    /// <summary>
    /// Required record JSON structure for connector
    /// </summary>
    public class RecordsSyncDTO
    {
        public Dictionary<Guid, string> created { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> updated { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> deleted { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> skipped { get; set; } = new Dictionary<Guid, string>();
    }

    /// <summary>
    /// Required geometry JSON structure for connector
    /// </summary>
    public class GeometriesSyncDTO
    {
        public List<Guid> created { get; set; } = new List<Guid>();
        public Dictionary<Guid, string> updated { get; set; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> deleted { get; set; } = new Dictionary<Guid, string>();
        /// <summary>
        /// guid and reasonString for skipping
        /// </summary>
        public Dictionary<Guid, string> skipped { get; set; } = new Dictionary<Guid, string>();
        public RecordsSyncDTO geometryRecords { get; set; } = new RecordsSyncDTO();

    }

    
}
