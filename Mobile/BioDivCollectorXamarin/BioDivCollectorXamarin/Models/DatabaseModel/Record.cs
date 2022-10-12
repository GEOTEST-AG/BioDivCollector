using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;
using ForeignKeyAttribute = SQLiteNetExtensions.Attributes.ForeignKeyAttribute;
using TableAttribute = SQLite.TableAttribute;

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    [Table("Record")]
    public class Record
    {
        /// <summary>
        /// Record database definition
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string recordId { get; set; }

        [ForeignKey(typeof(Form))]
        public int formId { get; set; }

        public string userName { get; set; }
        public string fullUserName { get; set; }
        public DateTime timestamp { get; set; }
        public DateTime creationTime { get; set; }
        public int status { get; set; }

        public bool readOnly { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<TextData> texts { get; set; } = new List<TextData>();
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<NumericData> numerics { get; set; } = new List<NumericData>();
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<BooleanData> booleans { get; set; } = new List<BooleanData>();
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<BinaryData> binaries { get; set; } = new List<BinaryData>();

        [ForeignKey(typeof(Project))]
        public int project_fk { get; set; }

        [ForeignKey(typeof(ReferenceGeometry))]
        public int? geometry_fk { get; set; }


        /// <summary>
        /// Create a new record given the form type and possible associated geometry
        /// </summary>
        /// <param name="formId"></param>
        /// <param name="geomId"></param>
        /// <returns>The created record</returns>
        public static Record CreateRecord(int formId, int? geomId)
        {
            Project proj = Project.FetchProjectWithChildren(App.CurrentProjectId);
            Record rec = new Record();
            rec.project_fk = proj.Id;
            rec.geometry_fk = geomId;
            rec.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
            rec.userName = App.CurrentUser.userId;
            rec.creationTime = DateTime.Now;
            rec.timestamp = DateTime.Now;
            rec.formId = formId;
            rec.recordId = Guid.NewGuid().ToString();
            rec.status = -1;

            //Add record to db.
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var success = conn.Insert(rec);
                if (success == 1)
                {
                    if (rec.geometry_fk == null)
                    {
                        proj.records = conn.Table<Record>().Select(n => n).Where(Record => Record.project_fk == proj.Id).Where(Record => Record.geometry_fk == null).ToList();
                        conn.UpdateWithChildren(proj);
                    }
                    else
                    {
                        var recs = conn.Table<Record>().Select(n => n).Where(Record => Record.geometry_fk == geomId).ToList();
                        var geom = ReferenceGeometry.GetGeometry((int)geomId);
                        geom.records = recs;
                        conn.UpdateWithChildren(geom);
                    }
                    return rec;
                }
            }
            return null;
        }

        /// <summary>
        /// Fetch a record from the database given its database id
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static Record FetchRecord(int recordId)
        {
            try
            {
                var record = new Record();
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    record = conn.Table<Record>().Select(n => n).Where(Record => Record.Id == recordId).FirstOrDefault();
                }
                return record;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save data");
            }
            return null;

        }

        /// <summary>
        /// Fetch a record from the database given its database id
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static bool FetchIfRecordHasOnlyEmptyChildren(int recordId)
        {
            try
            {
                var record = new Record();
                int t = 0;
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    record = conn.Table<Record>().Select(n => n).Where(Record => Record.Id == recordId).FirstOrDefault();
                    t += conn.Table<TextData>().Select(n => n).Where(TextData => TextData.record_fk == recordId).Where(TextData => TextData.value != "").Count();
                    t += conn.Table<NumericData>().Select(n => n).Where(NumericData => NumericData.record_fk == recordId).Where(NumericData => NumericData.value != 0).Count();
                    t += conn.Table<BinaryData>().Select(n => n).Where(BinaryData => BinaryData.record_fk == recordId).Count();
                }
                return t == 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save data");
            }
            return true;

        }

        /// <summary>
        /// Delete a record from the database given its database id
        /// </summary>
        /// <param name="recordId"></param>
        public static void DeleteRecord(int recordId)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    var record = conn.Table<Record>().Select(n => n).Where(Record => Record.Id == recordId).FirstOrDefault();
                    if (record.status > -1)
                    {
                        record.status = 3;
                        record.timestamp = DateTime.Now;
                        conn.Update(record);
                    }
                    else
                    {
                        conn.Delete(record);
                    }
                    MessagingCenter.Send<Application>(Application.Current, "RefreshRecords");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save data");
            }
        }

        /// <summary>
        /// Update the latest change time in a record given its database id
        /// </summary>
        /// <param name="recId"></param>
        public static void UpdateRecord(int recId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var rec = Record.FetchRecord(recId);
                var binaries = conn.Table<BinaryData>().Where(r => r.record_fk == rec.Id).ToList();
                rec.binaries = binaries;
                var texts = conn.Table<TextData>().Where(r => r.record_fk == rec.Id).ToList();
                rec.texts = texts;
                var numerics = conn.Table<NumericData>().Where(r => r.record_fk == rec.Id).ToList();
                rec.numerics = numerics;
                var booleans = conn.Table<BooleanData>().Where(r => r.record_fk == rec.Id).ToList();
                rec.booleans = booleans;
                rec.timestamp = DateTime.Now;
                rec.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
                rec.userName = App.CurrentUser.userId;
                if (rec.status != -1)
                {
                    rec.status = 2;
                }
                conn.UpdateWithChildren(rec);
            }
        }
    }

    /// <summary>
    /// Text data type parameter database definition
    /// </summary>
    [Table("TextData")]
    public class TextData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string textId { get; set; }
        public string title { get; set; }
        public string value { get; set; }

        public int? formFieldId { get; set; }
        public int? fieldChoiceId { get; set; }

        [ForeignKey(typeof(Record))]
        public int record_fk { get; set; }
    }

    /// <summary>
    /// Numeric data type parameter database definition
    /// </summary>
    [Table("NumericData")]
    public class NumericData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string numericId { get; set; }
        public string title { get; set; }
        public double? value { get; set; }

        public int? formFieldId { get; set; }

        [ForeignKey(typeof(Record))]
        public int record_fk { get; set; }
    }

    /// <summary>
    /// Boolean data type parameter database definition
    /// </summary>
    [Table("BooleanData")]
    public class BooleanData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string booleanId { get; set; }
        public string title { get; set; }
        public bool? value { get; set; }

        public int? formFieldId { get; set; }

        [ForeignKey(typeof(Record))]
        public int record_fk { get; set; }
    }

    /// <summary>
    /// Binary data type parameter database definition
    /// </summary>
    [Table("BinaryData")]
    public class BinaryData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string binaryId { get; set; }
        public int? formFieldId { get; set; }

        [ForeignKey(typeof(Record))]
        public int record_fk { get; set; }

        public BinaryData()
        {
            binaryId = Guid.NewGuid().ToString();
        }

        public async static Task<BinaryData> FetchBinaryData(string binaryId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var binDat = await Task.Run(async () =>
                {
                    var binDatAsync = conn.Table<BinaryData>().Select(n => n).Where(bin => bin.binaryId == binaryId).FirstOrDefault();
                    if (binDatAsync == null)
                    {
                        return new BinaryData();
                    }
                    return binDatAsync;
                });
                return binDat;
            }
        }

        public async static Task<bool> DownloadBinaryData(string recordId, int? formFieldId)
        {
            Record rec;
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    rec = conn.Table<Record>().Where(rec => rec.recordId == recordId).FirstOrDefault();
                }
                var binaryIds = GetBinaryDataIds(rec.Id, formFieldId);

                foreach (var binaryId in binaryIds)
                {
                    string url = App.ServerURL + "/api/Binary/" + binaryId;

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromSeconds(6000); // 10 minutes
                            var token = Preferences.Get("AccessToken","");
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                            MessagingCenter.Send(new DataDAO(), "SyncMessage", "Waiting for data");
                            var response = await client.GetAsync(url);  //UPLOAD
                            if (response.IsSuccessStatusCode)
                            {
                                var jsonbytes = await response.Content.ReadAsByteArrayAsync();
                                SaveData(jsonbytes, binaryId);
                            }
                            else if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    try
                                    {
                                        await App.Current.MainPage.DisplayAlert("Bild Downloadfehler", response.ToString(), "OK");
                                    }
                                    catch
                                    {

                                    }
                                });
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }
            return true;
        }

        public static List<string> GetBinaryDataIds(int recordId, int? formFieldId)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    var binarys = conn.Table<BinaryData>().Where(r => r.record_fk == recordId).Where(r2 => r2.formFieldId == formFieldId).ToList();
                    var binaryIds = new List<string>();
                    if (binarys == null || binarys == new List<BinaryData>())
                    {
                        BinaryData binDat = new BinaryData();
                        binaryIds = new List<string>() { binDat.binaryId }; //Give the new ID back to the current code
                        binDat.record_fk = recordId;
                        binDat.formFieldId = formFieldId;
                        conn.InsertOrReplace(binDat);
                    }
                    else
                    {
                        binaryIds = conn.Table<BinaryData>().Where(r => r.record_fk == recordId).Where(r2 => r2.formFieldId == formFieldId).Select(r3 => r3.binaryId).ToList();
                    }
                    return binaryIds;
                }

            }
            catch
            {
                //GetBinaryDataIds(recordId, formFieldId);
            }

            return null;
        }


        public static async void SaveData(byte[] jsonbytes, string binaryId)
        {
            //Save the data
            try
            {
                DependencyService.Get<CameraInterface>().SaveToFile(jsonbytes, binaryId);
            }
            catch
            {
                await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
            }
        }


        public static async Task<bool> DeleteBinaryFilesForProject(string projectId)
        {
            try
            {
                //List<string> binaryIds;
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    var binarys = conn.Query<BinaryData>("select binaryId from BinaryData where record_fk in (select recordId from Record where geometry_fk in (select geometryId from ReferenceGeometry where project_fk = ?))", projectId);
                    foreach (var bin in binarys)
                    {
                        DeleteBinaryFile(bin.binaryId);
                        conn.Delete(bin);
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

        public static async Task<bool> DeleteBinary(string binaryId)
        {
            try
            {
                //List<string> binaryIds;
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    var binaries = conn.Table<BinaryData>().Where(x => x.binaryId == binaryId).ToList();
                    foreach (var bin in binaries)
                    {
                        DeleteBinaryFile(bin.binaryId);
                        conn.Delete(bin);
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

        public static void DeleteBinaryFile(string binaryId)
        {
            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filepath = Path.Combine(directory, binaryId + ".jpg");
            File.Delete(filepath);
        }
    }
}
