using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Xamarin.Forms;
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
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                var success = conn.Insert(rec);
                if (success == 1)
                {
                    if (rec.geometry_fk == null)
                    {
                        proj.records = conn.Table<Record>().Select(n => n).Where(Record => Record.project_fk == proj.Id).ToList();
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
                using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
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
        /// Delete a record from the database given its database id
        /// </summary>
        /// <param name="recordId"></param>
        public static void DeleteRecord(int recordId)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
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
                    
                    MessagingCenter.Send<Application>(App.Current, "RefreshRecords");
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
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                var rec = Record.FetchRecord(recId);
                rec.timestamp = DateTime.Now;
                rec.fullUserName = App.CurrentUser.firstName + " " + App.CurrentUser.name;
                rec.userName = App.CurrentUser.userId;
                if (rec.status != -1)
                {
                    rec.status = 2;
                }
                conn.Update(rec);
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
}
