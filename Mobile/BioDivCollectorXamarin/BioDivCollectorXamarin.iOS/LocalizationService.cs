using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

[assembly: Dependency(typeof(BioDivCollectorXamarin.iOS.LocalizationService))]
namespace BioDivCollectorXamarin.iOS
{
    public class LocalizationService : ILocalize
    {
        public void SetLocale(string language)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(language);
        }
    }
}
