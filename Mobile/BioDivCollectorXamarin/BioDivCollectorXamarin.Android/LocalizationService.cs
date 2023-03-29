using System.Globalization;
using System.Threading;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

[assembly: Dependency(typeof(BioDivCollectorXamarin.Droid.LocalizationService))]
namespace BioDivCollectorXamarin.Droid
{
    public class LocalizationService : ILocalize
    {
        public void SetLocale(string language)
        {
            var userSelectedCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = userSelectedCulture;
        }
    }
}
