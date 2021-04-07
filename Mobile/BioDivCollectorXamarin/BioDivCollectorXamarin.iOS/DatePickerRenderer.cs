using System;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using System.Collections.Generic;
using CustomDatePicker.iOS;

[assembly: ExportRenderer(typeof(BioDivCollectorXamarin.Controls.CustomDatePicker), typeof(CustomDatePickerRenderer))]
namespace CustomDatePicker.iOS
{
	public class CustomDatePickerRenderer : DatePickerRenderer
	{
		protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null && this.Control != null)
			{
				if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
				{
					UIDatePicker picker = (UIDatePicker)Control.InputView;
					picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;
				}

				this.AddClearButton();
				/*
				if (Device.Idiom == TargetIdiom.Tablet)
				{
					this.Control.Font = UIFont.SystemFontOfSize(25);
				}*/
			}

		}

		private void AddClearButton()
		{
			var originalToolbar = this.Control.InputAccessoryView as UIToolbar;

			if (originalToolbar != null && originalToolbar.Items.Length <= 2)
			{
				var clearButton = new UIBarButtonItem("Löschen", UIBarButtonItemStyle.Plain, ((sender, ev) =>
				{
					BioDivCollectorXamarin.Controls.CustomDatePicker baseDatePicker = this.Element as BioDivCollectorXamarin.Controls.CustomDatePicker;
					this.Element.Unfocus();
					this.Element.Date = DateTime.Now;
					baseDatePicker.CleanDate();

				}));

				var newItems = new List<UIBarButtonItem>();
				foreach (var item in originalToolbar.Items)
				{
					newItems.Add(item);
				}

				newItems.Insert(0, clearButton);

				originalToolbar.Items = newItems.ToArray();
				originalToolbar.SetNeedsDisplay();
			}

		}
	}
}