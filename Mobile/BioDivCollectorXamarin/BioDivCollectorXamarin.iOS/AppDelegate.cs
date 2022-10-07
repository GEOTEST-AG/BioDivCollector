using System;
using System.IO;
using System.Linq;
using Foundation;
using UIKit;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

[assembly: Dependency(typeof(BioDivCollectorXamarin.iOS.IosDownloader))]
namespace BioDivCollectorXamarin.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            NativeMedia.Platform.Init(GetTopViewController);
            new Syncfusion.SfAutoComplete.XForms.iOS.SfAutoCompleteRenderer();
            Syncfusion.SfImageEditor.XForms.iOS.SfImageEditorRenderer.Init();
            string dbName = "biodivcollector_database.sqlite";
            string folderPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "..", "Library");
            string fullPath = Path.Combine(folderPath, dbName);
            string tilePath = DependencyService.Get<FileInterface>().GetMbTilesPath();
            LoadApplication(new App(fullPath,tilePath));

            return base.FinishedLaunching(app, options);
        }

        public UIViewController GetTopViewController()
        {
            var vc = UIApplication.SharedApplication.KeyWindow.RootViewController;

            if (vc is UINavigationController navController)
                vc = navController.ViewControllers.Last();

            return vc;
        }
    }
}
