using System;
using System.IO;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using BioDivCollectorXamarin.Models;
using Java.IO;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

[assembly: Dependency(typeof(BioDivCollectorXamarin.Droid.AndroidDownloader))]
namespace BioDivCollectorXamarin.Droid
{
    [Activity(Label = "BioDiv", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize  | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private static int PERMISSION_REQUEST_CODE = 200;
        string permission = Manifest.Permission.Camera;
        internal static Context ActivityContext { get; private set; }
        internal static string ImageIdToSave { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            ActivityContext = this;

            base.OnCreate(savedInstanceState);
            NativeMedia.Platform.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            string dbName = "biodivcollector_database.sqlite";
            string folderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string fullPath = System.IO.Path.Combine(folderPath, dbName);
            string tilePath = DependencyService.Get<FileInterface>().GetMbTilesPath();

            LoadApplication(new App(fullPath,tilePath));

            if (ActivityCompat.CheckSelfPermission(this, permission) == Permission.Granted)
            {
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera, Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, PERMISSION_REQUEST_CODE);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            if (NativeMedia.Platform.CheckCanProcessResult(requestCode, resultCode, intent))
                NativeMedia.Platform.OnActivityResult(requestCode, resultCode, intent);

            base.OnActivityResult(requestCode, resultCode, intent);
        }

    }

}
