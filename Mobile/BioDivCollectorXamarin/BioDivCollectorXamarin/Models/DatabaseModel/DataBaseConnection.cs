using SQLite;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FeldAppX.Models.DatabaseModel
{
    public static class Constants
    {
        public const string DatabaseFilename = "feldapp_database.sqlite";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                string basePath = String.Empty;
                if (Device.RuntimePlatform == "iOS")
                {
                    basePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "..", "Library");
                }
                else if (Device.RuntimePlatform == "Android")
                {
                    basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                }
                return Path.Combine(basePath, DatabaseFilename);
            }
        }
    }

    public class DatabaseConnection : SQLiteAsyncConnection
    {

        public static readonly AsyncLazy<DatabaseConnection> Instance = new AsyncLazy<DatabaseConnection>(() =>
        {
            var connectionString = new SQLiteConnectionString(Constants.DatabasePath);
            var instance = new DatabaseConnection(connectionString);
            //CreateTableResult result = await Database.CreateTableAsync<T>();
            return instance;
        });

        public DatabaseConnection(SQLiteConnectionString connectionString) : base(connectionString)
        {
        }
    }

    public class AsyncLazy<T>
    {
        readonly Lazy<Task<T>> instance;

        public AsyncLazy(Func<T> factory)
        {
            instance = new Lazy<Task<T>>(() => Task.Run(factory));
        }

        public AsyncLazy(Func<Task<T>> factory)
        {
            instance = new Lazy<Task<T>>(() => Task.Run(factory));
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return instance.Value.GetAwaiter();
        }
    }
}
