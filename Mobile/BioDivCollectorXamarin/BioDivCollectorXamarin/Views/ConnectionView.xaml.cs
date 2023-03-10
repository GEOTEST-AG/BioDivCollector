using BioDivCollectorXamarin.ViewModels;
using Xamarin.Forms.Xaml;

namespace BioDivCollectorXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]


    public partial class ConnectionView
    {
        ConnectionVM ViewModel;
        public ConnectionView()
        {
            InitializeComponent();
            ViewModel = new ConnectionVM(); //Add the view model so that it reacts to connectivity changes through the baseviemodel
            BindingContext = ViewModel;
        }

    }
}
