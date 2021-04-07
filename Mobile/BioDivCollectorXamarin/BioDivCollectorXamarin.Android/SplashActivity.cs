
using Android.App;
using Android.Content;
using Android.OS;

namespace BioDivCollectorXamarin.Droid
{
    [Activity(Theme = "@style/MainTheme.Splash", Label = "BioDiv", MainLauncher = true, NoHistory = true)]
public class SplashActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        static readonly string TAG = "X:" + typeof(SplashActivity).Name;

        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
        }

        // Launches the startup task
        protected override void OnResume()
        {
            base.OnResume();
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
    }
}