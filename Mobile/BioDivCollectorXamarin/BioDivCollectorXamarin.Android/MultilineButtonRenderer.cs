using Android.Content;
using BioDivCollectorXamarin.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Button), typeof(MultilineButtonRenderer))]
namespace BioDivCollectorXamarin.Droid
{
    public class MultilineButtonRenderer : ButtonRenderer
    {
        public MultilineButtonRenderer(Context context)
            : base(context)
        {

        }
    }

}
