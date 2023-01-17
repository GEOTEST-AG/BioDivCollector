using Android.Content;
using Android.Content.Res;
using Android.OS;
using BioDivCollectorXamarin.Controls;
using BioDivCollectorXamarin.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace BioDivCollectorXamarin.Droid
{
    class CustomEntryRenderer : EntryRenderer
    {
        public CustomEntryRenderer(Context context) : base(context)
        {

        }

        /// <summary>
        /// Adjust the background colour of a text entry
        /// </summary>
        /// <param name="e"></param>
        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (Control == null || e.NewElement == null) return;

            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                OSAppTheme currentTheme = Application.Current.RequestedTheme;
                if (currentTheme == OSAppTheme.Dark)
                {
                    Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.White);
                }
                else
                { 
                    Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Black);
                }
                Application.Current.RequestedThemeChanged += (s, a) =>
                {
                    OSAppTheme currentTheme = Application.Current.RequestedTheme;
                    if (currentTheme == OSAppTheme.Dark)
                    {
                        Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.White);
                    }
                    else
                    {
                        Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Black);
                    }
                };
            }
            else
            {

            }
        }
    }
}
