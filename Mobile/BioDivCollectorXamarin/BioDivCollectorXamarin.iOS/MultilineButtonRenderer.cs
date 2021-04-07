using System;
using BioDivCollectorXamarin.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Button), typeof(MultilineButtonRenderer))]
namespace BioDivCollectorXamarin.iOS
{
    public class MultilineButtonRenderer : ButtonRenderer
    {
        /// <summary>
        /// Allow multi-line buttons in iOS
        /// </summary>
        /// <param name="e"></param>
        protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.TitleLabel.LineBreakMode = UIKit.UILineBreakMode.WordWrap;
                Control.TitleLabel.TextAlignment = UITextAlignment.Center;
            }
                
        }
    }

    public class CustomDatePickerRenderer : DatePickerRenderer
    {

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<DatePicker> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null && this.Control != null)
            {
                try
                {
                    if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
                    {
                        UIDatePicker picker = (UIDatePicker)Control.InputView;
                        picker.PreferredDatePickerStyle = UIDatePickerStyle.Compact;
                    }

                }
                catch
                {
                }
            }
        }
    }

    public class CustomTimePickerRenderer : TimePickerRenderer
    {

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<TimePicker> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null && this.Control != null)
            {
                try
                {
                    if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
                    {
                        UIDatePicker picker = (UIDatePicker)Control.InputView;
                        picker.PreferredDatePickerStyle = UIDatePickerStyle.Compact;
                    }

                }
                catch
                {
                }
            }
        }
    }
}