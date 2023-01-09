using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace BioDivCollectorXamarin.Views
{
    public partial class LayersInfoPage : ContentPage
    {
        public LayersInfoPage()
        {
            InitializeComponent();
        }

        void CloseButton_Clicked(System.Object sender, System.EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("..");
            });
        }
    }
}

