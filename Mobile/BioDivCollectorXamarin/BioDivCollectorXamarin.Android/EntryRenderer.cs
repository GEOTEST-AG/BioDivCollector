using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Droid;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.OS;
using Android.Content.Res;
using BioDivCollectorXamarin.Controls;

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
                Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Black);
            }
            else
            {

            }
        }
    }
}
