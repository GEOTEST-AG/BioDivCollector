using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Controls
{
	/// <summary>
    /// Adds id and mandatory parameters to a standard text entry
    /// </summary>
    public class CustomEntry:Entry
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }
    }

	/// <summary>
	/// Adds id and mandatory parameters to a standard text editor
	/// </summary>
	public class CustomEditor : Editor
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }
    }

	/// <summary>
	/// Adds id and mandatory parameters to a standard checkbox
	/// </summary>
	public class CustomCheckBox : CheckBox
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }
    }

	/// <summary>
	/// Adds id and mandatory parameters to a standard picker/dropdown
	/// </summary>
	public class CustomPicker : Picker
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }
    }

	/// <summary>
	/// Adds id and mandatory parameters to a standard date picker and allows it to be nullable
	/// </summary>
	public class CustomDatePicker : DatePicker
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }

		public string _originalFormat = null;

		public static readonly BindableProperty PlaceHolderProperty =
			BindableProperty.Create(nameof(PlaceHolder), typeof(string), typeof(CustomDatePicker), "/ . / . /");

		public string PlaceHolder
		{
			get { return (string)GetValue(PlaceHolderProperty); }
			set
			{
				SetValue(PlaceHolderProperty, value);
			}
		}


		public static readonly BindableProperty NullableDateProperty =
		BindableProperty.Create(nameof(NullableDate), typeof(DateTime?), typeof(CustomDatePicker), null, defaultBindingMode: BindingMode.TwoWay);

		public DateTime? NullableDate
		{
			get {
				return (DateTime?)GetValue(NullableDateProperty); }
			set { SetValue(NullableDateProperty, value); UpdateDate(); }
		}

		private void UpdateDate()
		{
			if (NullableDate != null)
			{
				if (_originalFormat != null)
				{
					Format = _originalFormat;
				}
			}
			else
			{
				Format = PlaceHolder;

			}

		}
		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			if (BindingContext != null)
			{
				_originalFormat = Format;
				UpdateDate();
			}
		}

		protected override void OnPropertyChanged(string propertyName = null)
		{

			base.OnPropertyChanged(propertyName);
			var dateString = Date.ToString("d");
			var dateStringNow = DateTime.Now.ToString("d");
			var timeString = Date.ToString("t");
			var timeStringNow = DateTime.Now.ToString("t");
			if (propertyName == DateProperty.PropertyName || (propertyName == IsFocusedProperty.PropertyName && !IsFocused && dateString == dateStringNow))
			{
				AssignValue();
			}
			

			if (propertyName == NullableDateProperty.PropertyName && NullableDate.HasValue)
			{
				Date = NullableDate.Value;
				if (Date.ToString(_originalFormat) == DateTime.Now.ToString(_originalFormat))
				{
					//this code was done because when date selected is the actual date the"DateProperty" does not raise  
					UpdateDate();
				}
			}
		}


		public void CleanDate()
		{
			NullableDate = null;
			UpdateDate();
		}
		public void AssignValue()
		{
			NullableDate = Date;
			UpdateDate();

		}
	}

	/// <summary>
	/// Adds id and mandatory parameters to a standard time picker and allows it to be nullable
	/// </summary>
	public class CustomTimePicker : TimePicker
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }

		
		public string _originalFormat = null;

		public static readonly BindableProperty PlaceHolderProperty =
			BindableProperty.Create(nameof(PlaceHolder), typeof(string), typeof(CustomTimePicker), ".. : ..");

		public string PlaceHolder
		{
			get { return (string)GetValue(PlaceHolderProperty); }
			set
			{
				SetValue(PlaceHolderProperty, value);
			}
		}


		public static readonly BindableProperty NullableDateProperty =
		BindableProperty.Create(nameof(NullableDate), typeof(TimeSpan?), typeof(CustomTimePicker), null, defaultBindingMode: BindingMode.TwoWay);

		public TimeSpan? NullableDate
		{
			get { return (TimeSpan?)GetValue(NullableDateProperty); }
			set { SetValue(NullableDateProperty, value); UpdateDate(); }
		}

		private void UpdateDate()
		{
			if (NullableDate != null)
			{
				if (_originalFormat != null)
				{
					Format = _originalFormat;
				}
			}
			else
			{
				Format = PlaceHolder;
			}
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			if (BindingContext != null)
			{
				_originalFormat = Format;
				UpdateDate();
			}
		}

		protected override void OnPropertyChanged(string propertyName = null)
		{
			
			base.OnPropertyChanged(propertyName);

			if (propertyName == TimeProperty.PropertyName || (propertyName == IsFocusedProperty.PropertyName && !IsFocused && ((Time.Hours.ToString() + ":" + Time.Minutes.ToString()) == (DateTime.Now.TimeOfDay.Hours.ToString() + ":" + DateTime.Now.TimeOfDay.Minutes.ToString()))))
			{
				AssignValue();
			}

			if (propertyName == NullableDateProperty.PropertyName && NullableDate.HasValue)
			{
				Time = NullableDate.Value;
				var now = DateTime.Now.TimeOfDay.Hours.ToString() + ":" + DateTime.Now.TimeOfDay.Minutes.ToString();
				var newTime = Time.Hours.ToString() + ":" + Time.Minutes.ToString();
				if (newTime == now)
				{
					//this code was done because when date selected is the actual date the"DateProperty" does not raise  
					UpdateDate();
				}
			}
		}

		public void CleanDate()
		{
			NullableDate = null;
			UpdateDate();
		}
		public void AssignValue()
		{
			NullableDate = Time;
			UpdateDate();

		}
		
	}

	/// <summary>
	/// Adds id and mandatory parameters to a standard stack layout
	/// </summary>
	public class CustomStackLayout : StackLayout
    {
        public int? ValueId { get; set; }
        public int? TypeId { get; set; }
        public bool Mandatory { get; set; }
    }
}
