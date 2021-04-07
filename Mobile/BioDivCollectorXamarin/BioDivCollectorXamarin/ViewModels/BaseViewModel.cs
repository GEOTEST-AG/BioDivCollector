using Xamarin.Essentials;

namespace BioDivCollectorXamarin.ViewModels
{
    public class BaseViewModel : ObservableClass
    {
        
        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private bool isNotConnected = false;
        public bool IsNotConnected
        {
            get { return isNotConnected; }
            set { SetProperty(ref isNotConnected, value); }
        }

        private bool isConnected;
        public bool IsConnected
        {
            get { return !IsNotConnected; }
        }

        /// <summary>
        /// Observe connectivity
        /// </summary>
        public BaseViewModel()
        {
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            CheckConnection();
        }

        ~BaseViewModel()
        {
            //Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        /// <summary>
        /// When connectivity changes update the IsConnected and IsNotConnected parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            CheckConnection();
        }

        /// <summary>
        /// Update the IsConnected and IsNotConnected parameters
        /// </summary>
        private void CheckConnection()
        {
            if (Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet)
            {
                IsNotConnected = false;
            }
            else
            {
                IsNotConnected = true;
            }
        }
    }
}
