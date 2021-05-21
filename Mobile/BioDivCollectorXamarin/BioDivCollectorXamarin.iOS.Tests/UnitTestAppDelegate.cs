using System;
using System.Linq;
using System.Collections.Generic;

using Foundation;
using UIKit;
using MonoTouch.NUnit.UI;
using System.IO;

namespace BioDivCollectorXamarin.iOS.Tests
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("UnitTestAppDelegate")]
    public partial class UnitTestAppDelegate : UIApplicationDelegate
    {
        // class-level declarations
        UIWindow window;
        TouchRunner runner;
        public string DatabaseLocation;
        string dbName = "biodivcollector_database.sqlite";
        string folderPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "..", "Library");


        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            DatabaseLocation = Path.Combine(folderPath, dbName);
            string tilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // create a new window instance based on the screen size
            window = new UIWindow(UIScreen.MainScreen.Bounds);
            runner = new TouchRunner(window);

            // register every tests included in the main application/assembly
            runner.Add(System.Reflection.Assembly.GetExecutingAssembly());

            window.RootViewController = new UINavigationController(runner.GetViewController());

            // make the window visible
            window.MakeKeyAndVisible();

            return true;
        }
    }
}
