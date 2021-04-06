using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using BioDivCollectorXamarin.Models;
using BioDivCollectorXamarin.Droid;
using BioDivCollectorXamarin.Controls;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.OS;
using Android.Content.Res;

[assembly: ExportRenderer(typeof(CustomPicker), typeof(CustomPickerRenderer))]
namespace BioDivCollectorXamarin.Droid
{
    class CustomPickerRenderer : Xamarin.Forms.Platform.Android.AppCompat.PickerRenderer
    {
        public CustomPickerRenderer(Context context) : base(context)
        {
            AutoPackage = false;
        }

        /// <summary>
        /// Change the background colour of a picker
        /// </summary>
        /// <param name="e"></param>
        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            if (Control == null || e.NewElement == null) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Gray);
            else
                Control.Background.SetColorFilter(Android.Graphics.Color.Gray, PorterDuff.Mode.SrcAtop);
        }
    }
}