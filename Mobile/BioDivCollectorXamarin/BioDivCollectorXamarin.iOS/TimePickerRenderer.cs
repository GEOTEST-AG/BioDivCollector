using System;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using UIKit;
using System.Collections.Generic;
using BioDivCollectorXamarin.iOS;

[assembly: ExportRenderer(typeof(BioDivCollectorXamarin.Controls.CustomTimePicker), typeof(CustomTimePickerRenderer))]
namespace CustomTimePicker.iOS
{
	public class CustomTimePickerRenderer : TimePickerRenderer
	{

		protected override void OnElementChanged(ElementChangedEventArgs<TimePicker> e)
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

				if (Device.Idiom == TargetIdiom.Tablet)
				{
					this.Control.Font = UIFont.SystemFontOfSize(25);
				}
			}

		}

		private void AddClearButton()
		{
			var originalToolbar = this.Control.InputAccessoryView as UIToolbar;

			if (originalToolbar != null && originalToolbar.Items.Length <= 2)
			{
				var clearButton = new UIBarButtonItem("Löschen", UIBarButtonItemStyle.Plain, ((sender, ev) =>
				{
					BioDivCollectorXamarin.Controls.CustomTimePicker baseTimePicker = this.Element as BioDivCollectorXamarin.Controls.CustomTimePicker;
					this.Element.Unfocus();
					this.Element.Time = DateTime.Now.TimeOfDay;
					baseTimePicker.CleanDate();

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