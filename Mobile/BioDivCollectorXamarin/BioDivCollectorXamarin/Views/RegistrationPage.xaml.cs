using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace BioDivCollectorXamarin.Views
{	
	public partial class RegistrationPage : ContentPage
	{	
		public RegistrationPage ()
		{
			InitializeComponent ();
		}

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
			MessagingCenter.Send<Application>(App.Current, "ReturnToLogin");
        }
    }
}

