using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensionsAsync.Extensions;
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
        //[PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [PrimaryKey]
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
        public static async Task<Record> CreateRecord(int formId, int? geomId)
        {
            Project proj = await Project.FetchProjectWithChildren(App.CurrentProjectId);
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
            var conn = App.ActiveDatabaseConnection;
            var success = await conn.InsertAsync(rec);
            if (success == 1)
            {
                if (rec.geometry_fk == null || rec.geometry_fk == 0)
                {
                    proj.records = await conn.Table<Record>().Where(Record => Record.project_fk == proj.Id).Where(Record => Record.geometry_fk == null).ToListAsync();
                    await conn.UpdateWithChildrenAsync(proj);
                }
                else
                {
                    var recs = await conn.Table<Record>().Where(Record => Record.geometry_fk == geomId).ToListAsync();
                    var geom = await ReferenceGeometry.GetGeometry((int)geomId);
                    geom.records = recs;
                    await conn.UpdateWithChildrenAsync(geom);
                }
                return rec;
            }
            return null;
        }

        /// <summary>
        /// Fetch a record from the database given its database id
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        //public static async Task<Record> FetchRecord(string recordId)
        //{
        //    try
        //    {
        //        var record = new Record();
        //        var conn = App.ActiveDatabaseConnection;
        //        record = await conn.Table<Record>().Where(Record => Record.recordId == recordId).FirstOrDefaultAsync();
        //        return record;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Could not save data");
        //    }
        //    return null;
        //}

        /// <summary>
        /// Get record by guID record id
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static async Task<Record> FetchRecord(string recordId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<Record>().Where(r => r.recordId == recordId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get record by Geometry ID
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static async Task<List<Record>> FetchRecordByGeomId(int geomId)
        {
            var recordList = new List<Record>();
            var conn = App.ActiveDatabaseConnection;
            recordList = await conn.Table<Record>().Where(Record => Record.geometry_fk == geomId).Where(Record => Record.status != 3).ToListAsync();
            return recordList;
        }

        /// <summary>
        /// Get record by Project ID
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static async Task<List<Record>> FetchRecordByProjectId(int projectId)
        {
            var recordList = new List<Record>();
            var conn = App.ActiveDatabaseConnection;
            recordList = await conn.Table<Record>().Where(g => g.project_fk == projectId).Where(g => g.geometry_fk == null || g.geometry_fk == 0).ToListAsync();
            return recordList;
        }

        /// <summary>
        /// Fetch a record from the database given its database id
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static async Task<bool> FetchIfRecordHasOnlyEmptyChildren(string recordId)
        {
            try
            {
                var record = new Record();
                int t = 0;
                var conn = App.ActiveDatabaseConnection;
                //record = await conn.Table<Record>().Where(Record => Record.recordId == recordId).FirstOrDefaultAsync();
                t += await conn.Table<TextData>().Where(TextData => TextData.record_fk == recordId).Where(TextData => TextData.value != "").CountAsync();
                t += await conn.Table<NumericData>().Where(NumericData => NumericData.record_fk == recordId).Where(NumericData => NumericData.value != 0).CountAsync();
                t += await conn.Table<BinaryData>().Where(BinaryData => BinaryData.record_fk == recordId).CountAsync();
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
        public static async Task DeleteRecord(string recordId)
        {
            try
            {
                var conn = App.ActiveDatabaseConnection;
                var record = await conn.Table<Record>().Where(Record => Record.recordId == recordId).FirstOrDefaultAsync();
                if (record != null && record.status > -1)
                {
                    record.status = 3;
                    record.timestamp = DateTime.Now;
                    await conn.UpdateAsync(record);
                }
                else
                {
                    await conn.DeleteAsync(record);
                }

                MessagingCenter.Send<Xamarin.Forms.Application>(App.Current, "RefreshRecords");
            }
            catch (Exception e)
            {

            }
        }

        /// <summary>
        /// Update the latest change time in a record given its database id
        /// </summary>
        /// <param name="recId"></param>
        public static async Task UpdateRecord(string recId)
        {
            var conn = App.ActiveDatabaseConnection;
            var rec = await FetchRecord(recId);
            var binaries = await conn.Table<BinaryData>().Where(r => r.record_fk == rec.recordId).ToListAsync();
            rec.binaries = binaries;
            var texts = await conn.Table<TextData>().Where(r => r.record_fk == rec.recordId).ToListAsync();
            rec.texts = texts;
            var numerics = await conn.Table<NumericData>().Where(r => r.record_fk == rec.recordId).ToListAsync();
            rec.numerics = numerics;
            var booleans = await conn.Table<BooleanData>().Where(r => r.record_fk == rec.recordId).ToListAsync();
            rec.booleans = booleans;
            rec.timestamp = DateTime.Now;
            rec.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
            rec.userName = App.CurrentUser.userId;
            if (rec.status != -1)
            {
                rec.status = 2;
            }
            await conn.UpdateWithChildrenAsync(rec);

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
        //public int record_fk { get; set; }
        public string record_fk { get; set; }

        public static async Task<TextData> FetchTextData(string textId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<TextData>().Where(TextData => TextData.textId == textId).FirstOrDefaultAsync();
        }

        public static async Task<List<TextData>> FetchTextDataByRecordId(string recordId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<TextData>().Where(TextData => TextData.record_fk == recordId).ToListAsync();
        }

        public static async Task<List<TextData>> FetchTextDataByFormFieldId(int formFieldId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<TextData>().Where(TextData => TextData.formFieldId == formFieldId).ToListAsync();
        }
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
        //public int record_fk { get; set; }
        public string record_fk { get; set; }

        public static async Task<NumericData> FetchNumericDataById(string numericId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<NumericData>().Where(NumericData => NumericData.numericId == numericId).FirstOrDefaultAsync();
        }

        public static async Task<List<NumericData>> FetchNumericDataByRecordId(string recordId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<NumericData>().Where(NumericData => NumericData.record_fk == recordId).ToListAsync();
        }
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
        //public int record_fk { get; set; }
        public string record_fk { get; set; }

        public static async Task<BooleanData> FetchBooleanData(string booleanId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<BooleanData>().Where(BooleanData => BooleanData.booleanId == booleanId).FirstOrDefaultAsync();
        }

        public static async Task<List<BooleanData>> FetchBooleanDataByRecordId(string recordId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<BooleanData>().Where(BooleanData => BooleanData.record_fk == recordId).ToListAsync();
        }
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
        //public int record_fk { get; set; }
        public string record_fk { get; set; }

        public BinaryData()
        {
            binaryId = Guid.NewGuid().ToString();
        }

        public async static Task<BinaryData> FetchBinaryData(string binaryId)
        {
            var conn = App.ActiveDatabaseConnection;
            var binDatAsync = await conn.Table<BinaryData>().Where(bin => bin.binaryId == binaryId).FirstOrDefaultAsync();
            return binDatAsync;
        }

        public async static Task<List<BinaryData>> FetchBinaryDataByRecordId(string recordId)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<BinaryData>().Where(bin => bin.record_fk == recordId).ToListAsync();
        }

        public async Task SaveBinaryRecord()
        {
            var conn = App.ActiveDatabaseConnection;
            await conn.InsertAsync(this);
            await Record.UpdateRecord(this.record_fk);
        }

        public async static Task<bool> DownloadBinaryData(string recordId, int? formFieldId)
        {
            //var conn = App.ActiveDatabaseConnection;
            //Record rec = await conn.Table<Record>().Where(rec => rec.recordId == recordId).FirstOrDefaultAsync();
            var binaryIds = await GetBinaryDataIds(recordId, formFieldId);

            foreach (var binaryId in binaryIds)
            {
                try
                {
                    string url = App.ServerURL + "/api/Binary/" + binaryId;

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromSeconds(6000); // 10 minutes
                            var token = Preferences.Get("AccessToken", "");
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
                catch
                {

                }
            }
            return true;
        }

        public static async Task<List<string>> GetBinaryDataIds(string recordId, int? formFieldId)
        {
            try
            {
                var conn = App.ActiveDatabaseConnection;
                var binarys = await conn.Table<BinaryData>().Where(r => r.record_fk == recordId).Where(r2 => r2.formFieldId == formFieldId).ToListAsync();
                var binaryIds = new List<string>();

                binaryIds.AddRange(binarys.Select(f => f.binaryId));


                /*if (binarys == null || binarys == new List<BinaryData>())
                {
                    BinaryData binDat = new BinaryData();
                    binaryIds = new List<string>() { binDat.binaryId }; //Give the new ID back to the current code
                    binDat.record_fk = recordId;
                    binDat.formFieldId = formFieldId;
                    await conn.InsertOrReplaceAsync(binDat);
                }*/
                //else
                //{
                //    binaryIds = conn.Table<BinaryData>().Where(r => r.record_fk == recordId).Where(r2 => r2.formFieldId == formFieldId).Where(r3 => r3.binaryId).ToList();
                //}
                return binaryIds;

            }
            catch
            {
                await GetBinaryDataIds(recordId, formFieldId);
            }

            return null;
        }


        public static void SaveData(byte[] jsonbytes, string binaryId)
        {
            //Save the data
            try
            {
                DependencyService.Get<CameraInterface>().SaveToFile(jsonbytes, binaryId);
            }
            catch
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current.MainPage.DisplayAlert("Das Foto konnte nicht als Datei gespeichert werden", String.Empty, "OK");
                });
            }
        }

        public static void SaveData(Stream stream, string binaryId)
        {
            var directory = DependencyService.Get<FileInterface>().GetImagePath();
            string filepath = Path.Combine(directory, binaryId + ".jpg");

            if (stream.Length > 0)
            {
                // Create a FileStream object to write a stream to a file
                using (FileStream fileStream = System.IO.File.Create(filepath, (int)stream.Length))
                {

                    // Fill the bytes[] array with the stream data
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, (int)bytesInStream.Length);

                    // Use FileStream object to write to the specified file
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                }
            }
        }


        public static async Task<bool> DeleteBinaryFilesForProject(string projectId)
        {
            try
            {
                //List<string> binaryIds;
                var conn = App.ActiveDatabaseConnection;
                var binarys = await conn.QueryAsync<BinaryData>("select binaryId from BinaryData where record_fk in (select recordId from Record where geometry_fk in (select geometryId from ReferenceGeometry where project_fk = ?))", projectId);
                foreach (var bin in binarys)
                {
                    DeleteBinaryFile(bin.binaryId);
                    await conn.DeleteAsync(bin);
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
                var conn = App.ActiveDatabaseConnection;
                var binaries = await conn.Table<BinaryData>().Where(x => x.binaryId == binaryId).ToListAsync();
                foreach (var bin in binaries)
                {
                    DeleteBinaryFile(bin.binaryId);
                    await conn.DeleteAsync(bin);
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
            try
            {
                var directory = DependencyService.Get<FileInterface>().GetImagePath();
                string filepath = Path.Combine(directory, binaryId + ".jpg");
                File.Delete(filepath);
            }
            catch { }
        }
    }
}
