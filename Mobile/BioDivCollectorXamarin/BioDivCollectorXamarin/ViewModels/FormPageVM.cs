using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Controls;
using SQLite;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Threading.Tasks;
using Syncfusion.SfAutoComplete.XForms;

namespace BioDivCollectorXamarin.ViewModels
{
    class FormPageVM : BaseViewModel
    {

        public List<View> Assets = new List<View>();
        public int RecId;
        private bool NewRecord;
        public Record queriedrec { get; set; }
        public Form formType { get; set; }
        private string BDCGUIDtext;
        private List<ReferenceGeometry> Geoms;
        private CustomAutoComplete AssociatedGeometry;
        public bool ReadOnly = false;
        private Dictionary<int, bool> Validation = new Dictionary<int, bool>();

        //Commands
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command GUIDCommand { get; }
        public Command DeleteCommand { get; }

        /// <summary>
        /// On creating the view controller for a specific recordId, the form is checked to see which parameters are required, and the relevant input fields are queued up for adding to the page.
        /// The relevant data is then extracted from the database for the specific field, and added as the initial value for that field.
        /// </summary>
        /// <param name="recId">recordID</param>
        public FormPageVM(int? recId, int formId, int? geomId)
        {

            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                //Get the record and its corresponding variable values
                if (recId != null)
                {
                    queriedrec = conn.GetWithChildren<Record>(recId);
                    ReadOnly = queriedrec.readOnly;
                    RecId = (int)recId;
                }
                else
                {
                    queriedrec = Record.CreateRecord(formId, geomId);
                    RecId = queriedrec.Id;
                    NewRecord = true;
                }
                
                var fontSize = 16;
                var txts = queriedrec.texts;
                var nums = queriedrec.numerics;
                var bools = queriedrec.booleans;
                //Compile the GUID
                BDCGUIDtext = "<<BDC><" + queriedrec.recordId + ">>";

                var formTemp = conn.Table<Form>().Where(Form => Form.formId == formId).FirstOrDefault();
                formType = conn.GetWithChildren<Form>(formTemp.Id);
                foreach (var formField in formType.formFields.OrderBy(f => f.order))
                {
                    if (formField.typeId == 71)
                    {
                        var header = new Label();
                        if (formField.title != null && formField.title != String.Empty)
                        {
                            header.Text = formField.title;
                        }
                        else
                        {
                            header.Text = formField.description;
                        }
                        header.HeightRequest = 50;
                        header.FontAttributes = FontAttributes.Bold;
                        header.VerticalTextAlignment = TextAlignment.End;
                        header.Margin = new Thickness(0, 10, 0, 0);
                        header.TextColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                        header.FontSize = 24;
                        Assets.Add(header);
                    }

                    else

                    {
                        var label = new Label();
                        if (formField.title != null && formField.title != String.Empty)
                        {
                            label.Text = formField.title;
                        }
                        else
                        {
                            label.Text = formField.description;
                        }
                        if (formField.mandatory)
                        {
                            label.Text = label.Text + " *";
                        }
                        label.FontAttributes = FontAttributes.Bold;
                        label.Margin = new Thickness(0, 10, 0, 0);
                        label.SetAppThemeColor(Label.TextColorProperty, Color.Black, Color.White);
                        Assets.Add(label);

                        if (formField.typeId == 11 || formField.typeId == 61)
                        {
                            try
                            {
                                var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.formFieldId == formField.fieldId).FirstOrDefault();
                                var textField = new CustomEntry();

                                if (text == null)
                                {
                                    //CreateNew
                                    var txt = new TextData { textId = Guid.NewGuid().ToString(), title = String.Empty, value = null, formFieldId = formField.fieldId, record_fk = RecId };
                                    conn.Insert(txt);
                                    txts.Add(txt);
                                    queriedrec.texts = txts;
                                    text = txt;
                                }
                                var localReadOnly = ReadOnly;
                                if (formField.standardValue!=null && formField.standardValue.Substring(0,1) == "=") { localReadOnly = true; }
                                textField.Text = Form.DetermineText(queriedrec, text, formField.standardValue);
                                textField.Keyboard = Keyboard.Text;
                                textField.Placeholder = formField.description;
                                textField.ClearButtonVisibility = ClearButtonVisibility.WhileEditing;
                                textField.ReturnType = ReturnType.Done;
                                textField.Margin = new Thickness(0, 0, 0, 10);
                                textField.ValueId = text.Id;
                                textField.HeightRequest = 40;
                                textField.FontSize = fontSize;
                                textField.TypeId = formField.typeId;
                                textField.TextChanged += TextFieldChanged;
                                textField.IsEnabled = !localReadOnly;
                                textField.Mandatory = formField.mandatory;
                                textField.PlaceholderColor = Color.Gray;
                                textField.SetAppThemeColor(CustomEntry.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                                textField.SetAppThemeColor(CustomEntry.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
                                var empty = String.IsNullOrEmpty(textField.Text);
                                if (formField.mandatory) { Validation.Add((int)textField.ValueId, !empty); }
                                if (localReadOnly)
                                {
                                    textField.SetAppThemeColor(Label.BackgroundColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                                }
                                Assets.Add(textField);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not create field" + e);
                            }

                        }
                        else if (formField.typeId == 41)
                        {
                            try
                            {
                                var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                                var dateField = new CustomDatePicker();
                                var timeField = new CustomTimePicker();
                                var nowButton = new Button();
                                var clearButton = new Button();

                                var stack = new CustomStackLayout()
                                {
                                    Orientation = StackOrientation.Horizontal,
                                    Children =
                                {
                                    dateField, timeField, nowButton, clearButton
                                }
                                };
                                stack.WidthRequest = 350;
                                stack.HorizontalOptions = LayoutOptions.Start;

                                if (text == null)
                                {
                                    //CreateNew
                                    var txt = new TextData { textId = Guid.NewGuid().ToString(), title = String.Empty, value = String.Empty, formFieldId = formField.fieldId, record_fk = RecId };
                                    conn.Insert(txt);
                                    txts.Add(txt);
                                    queriedrec.texts = txts;
                                    text = txt;
                                }

                                try
                                {
                                    var dt = DateTime.ParseExact(text.value, "yyyy-MM-ddTHH:mm:sszzz", null);

                                    if (text.value != null && text.value != String.Empty)
                                    {
                                        dateField.NullableDate = dt.Date;
                                        timeField.NullableDate = new TimeSpan(dt.TimeOfDay.Hours, dt.TimeOfDay.Minutes, 0);
                                    }
                                }
                                catch (Exception exp)
                                {
                                    Console.WriteLine(exp);
                                }

                                dateField.Margin = new Thickness(0, 0, 0, 10);
                                dateField.ValueId = text.Id;
                                dateField.TypeId = formField.typeId;
                                dateField.Format = "dd MMMM yyyy";
                                dateField.IsEnabled = !ReadOnly;
                                dateField.FontSize = fontSize;
                                dateField.WidthRequest = 170;
                                dateField.HeightRequest = 40;
                                dateField.Mandatory = formField.mandatory;
                                dateField.VerticalOptions = LayoutOptions.StartAndExpand;
                                dateField.SetAppThemeColor(CustomEntry.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                                dateField.SetAppThemeColor(CustomEntry.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);

                                dateField.PropertyChanged += DateFieldChanged;
                                var empty = (dateField.NullableDate == null);
                                if (formField.mandatory) { Validation.Add((int)dateField.ValueId, !empty); }
                                if (ReadOnly)
                                {
                                    dateField.SetAppThemeColor(Label.BackgroundColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                                }
                                timeField.Margin = new Thickness(0, 0, 0, 0);
                                timeField.ValueId = text.Id;
                                timeField.TypeId = formField.typeId;
                                timeField.Format = "HH:mm";
                                timeField.IsEnabled = !ReadOnly;
                                timeField.FontSize = fontSize;
                                timeField.WidthRequest = 70;
                                timeField.HeightRequest = 40;
                                timeField.VerticalOptions = LayoutOptions.StartAndExpand;
                                timeField.Mandatory = formField.mandatory;
                                timeField.SetAppThemeColor(CustomEntry.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                                timeField.SetAppThemeColor(CustomEntry.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);

                                timeField.PropertyChanged += TimeFieldChanged;
                                if (ReadOnly)
                                {
                                    timeField.SetAppThemeColor(Label.BackgroundColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                                }


                                Dictionary<String, Object> dic = new Dictionary<string, object>();
                                dic.Add("text", text);
                                dic.Add("date", dateField);
                                dic.Add("time", timeField);

                                var nowCommand = new Command(FillOutDate);

                                nowButton.Text = "JETZT";
                                nowButton.TextTransform = TextTransform.Uppercase;
                                nowButton.FontSize = 12;
                                nowButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["TransparentButtonStyle"];
                                nowButton.Command = nowCommand;
                                nowButton.CommandParameter = dic;
                                nowButton.Margin = new Thickness(10, 0, 0, 0);
                                nowButton.WidthRequest = 40;
                                nowButton.HeightRequest = 40;
                                nowButton.VerticalOptions = LayoutOptions.StartAndExpand;
                                nowButton.IsVisible = !ReadOnly;

                                var clearCommand = new Command(ClearDate);

                                clearButton.Text = "ⓧ";
                                clearButton.FontSize = 20;
                                clearButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["TransparentButtonStyle"];
                                clearButton.Command = clearCommand;
                                clearButton.CommandParameter = dic;
                                clearButton.Margin = new Thickness(10, 0, 10, 0);
                                clearButton.WidthRequest = 30;
                                clearButton.HeightRequest = 40;
                                clearButton.VerticalOptions = LayoutOptions.StartAndExpand;
                                clearButton.IsVisible = !ReadOnly;

                                stack.Margin = new Thickness(0, 0, 0, 10);
                                stack.ValueId = text.Id;
                                stack.TypeId = formField.typeId;

                                Assets.Add(stack);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not create field" + e);
                            }

                        }
                        else if (formField.typeId == 51)
                        {
                            try
                            {
                                var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                                var dropField = new CustomAutoComplete();
                                if (text == null)
                                {
                                    //CreateNew
                                    var txt = new TextData { textId = Guid.NewGuid().ToString(), title = String.Empty, value = String.Empty, formFieldId = formField.fieldId, record_fk = RecId };
                                    conn.Insert(txt);
                                    txts.Add(txt);
                                    queriedrec.texts = txts;
                                    text = txt;
                                }
                                dropField.SetAppThemeColor(SfAutoComplete.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                                dropField.SetAppThemeColor(SfAutoComplete.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
                                dropField.SetAppThemeColor(SfAutoComplete.BorderColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));

                                List<FieldChoice> fieldChoices = Form.FetchFormChoicesForDropdown(formField.Id);

                                dropField.AutoCompleteSource = fieldChoices.Select(choice => choice.text).ToList();
                                dropField.ItemsSource = fieldChoices;
                                dropField.SelectedItem = new Binding("text");

                                dropField.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                                dropField.TextHighlightMode = OccurrenceMode.MultipleOccurrence;
                                dropField.SuggestionMode = SuggestionMode.Contains;
                                dropField.DropDownCornerRadius = 10;
                                dropField.HeightRequest = 40;
                                dropField.EnableAutoSize = true;
                                dropField.MultiSelectMode = MultiSelectMode.None;
                                dropField.ShowSuggestionsOnFocus = true;
                                dropField.IsSelectedItemsVisibleInDropDown = false;
                                if (Device.RuntimePlatform == Device.Android)
                                {
                                    dropField.BorderColor = Color.Black;
                                }
                                dropField.TextSize = fontSize;
                                dropField.DropDownBackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGrey"];
                                dropField.DropDownTextColor = Color.White;
                                dropField.HighlightedTextColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                                dropField.DropDownItemHeight = 50;
                                dropField.MaximumSuggestion = 50;
                                dropField.EnableSelectionIndicator = true;
                                dropField.LoadMoreText = "WEITERE ERGEBNISSE";
                                dropField.MaximumDropDownHeight = 150;
                                dropField.WatermarkColor = Color.Gray;

                                dropField.ValueId = text.Id;
                                dropField.TypeId = formField.Id;
                                dropField.Watermark = formField.description;
                                dropField.IsEnabled = !ReadOnly;
                                dropField.Mandatory = formField.mandatory;
                                if (formField.mandatory) { Validation.Add((int)dropField.ValueId, dropField.SelectedItem != null || (text.value != null && text.value != String.Empty)); }
                                if (ReadOnly)
                                {
                                    dropField.SetAppThemeColor(Label.BackgroundColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                                }
                                dropField.SelectionChanged += DidSelectFromChoices;
                                if (text.fieldChoiceId != null)
                                {
                                    var selectedChoiceIndex = fieldChoices.FindIndex(a => a.choiceId == text.fieldChoiceId);
                                    dropField.SelectedIndex = selectedChoiceIndex;
                                }
                                dropField.Margin = new Thickness(0, 0, 0, 10);

                                Assets.Add(dropField);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not create field" + e);
                            }

                        }
                        else if (formField.typeId == 21)
                        {
                            try
                            {
                                var num = conn.Table<NumericData>().Select(n => n).Where(NumericData => NumericData.record_fk == RecId).Where(NumericData => NumericData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                                var textField = new CustomEntry();
                                if (num == null)
                                {
                                    //CreateNew
                                    var nm = new NumericData { numericId = Guid.NewGuid().ToString(), title = String.Empty, value = null, formFieldId = formField.fieldId, record_fk = RecId };
                                    conn.Insert(nm);
                                    nums.Add(nm);
                                    queriedrec.texts = txts;
                                    num = nm;

                                }

                                textField = new CustomEntry { Text = ((double)num.value).ToString("F", CultureInfo.CreateSpecificCulture("de-CH")) };

                                textField.SetAppThemeColor(CustomEntry.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                                textField.SetAppThemeColor(CustomEntry.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);

                                textField.Keyboard = Keyboard.Numeric;
                                textField.ClearButtonVisibility = ClearButtonVisibility.WhileEditing;
                                textField.ReturnType = ReturnType.Done;
                                textField.Margin = new Thickness(0, 0, 0, 10);
                                textField.ValueId = num.Id;
                                textField.TypeId = formField.typeId;
                                textField.HeightRequest = 40;
                                textField.FontSize = fontSize;
                                textField.IsEnabled = !ReadOnly;
                                textField.Mandatory = formField.mandatory;
                                textField.PlaceholderColor = Color.Gray;
                                var empty = String.IsNullOrEmpty(textField.Text);
                                if (formField.mandatory) { Validation.Add((int)textField.ValueId, !empty); }
                                if (ReadOnly)
                                {
                                    textField.SetAppThemeColor(Label.BackgroundColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                                }
                                textField.TextChanged += NumericFieldChanged;
                                Assets.Add(textField);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not create field" + e);
                            }

                        }
                        else if (formField.typeId == 31)
                        {
                            try
                            {
                                var boolValue = conn.Table<BooleanData>().Select(n => n).Where(BooleanData => BooleanData.record_fk == RecId).Where(BooleanData => BooleanData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                                var checkBox = new CustomCheckBox();
                                if (boolValue == null)
                                {
                                    //CreateNew
                                    boolValue = new BooleanData { booleanId = Guid.NewGuid().ToString(), title = String.Empty, value = false, formFieldId = formField.fieldId, record_fk = RecId };
                                    conn.Insert(boolValue);
                                    bools.Add(boolValue);
                                    queriedrec.booleans = bools;
                                }
                                if (boolValue != null)
                                {
                                    checkBox.IsChecked = (bool)boolValue.value;
                                }
                                checkBox.Margin = new Thickness(0, 0, 0, 10);
                                checkBox.ValueId = boolValue.Id;
                                checkBox.TypeId = formField.typeId;
                                checkBox.IsEnabled = !ReadOnly;
                                checkBox.Color = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                                if (ReadOnly) { checkBox.Color = Color.LightGray; }
                                checkBox.CheckedChanged += BooleanFieldChanged;
                                Assets.Add(checkBox);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not create field" + e);
                            }

                        }

                    }
                }

                    

                var geomlabel = new Label();
                geomlabel.Text = "Zugeordnete Geometrie";
                geomlabel.FontAttributes = FontAttributes.Bold;
                geomlabel.Margin = new Thickness(0, 10, 0, 0);
                Assets.Add(geomlabel);

                AssociatedGeometry = new CustomAutoComplete();
                Geoms = ReferenceGeometry.GetAllGeometries().Where(g => g.status < 3).OrderBy(g => g.geometryName).ToList();
                foreach (var gm in Geoms)
                {
                    if (gm.geometryName == null)
                    {
                        gm.geometryName = String.Empty; //Avoid a crash on android from null strings
                    }
                }
                var general = new ReferenceGeometry() { geometryName = "Allgemeine Beobachtung" };
                var refGeoms = Geoms;
                refGeoms.Insert(0, general);
                AssociatedGeometry.AutoCompleteSource = refGeoms.Select(c => c.geometryName).ToList();
                AssociatedGeometry.ItemsSource = refGeoms;
                AssociatedGeometry.SelectedItem = new Binding ("geometryName");
                AssociatedGeometry.TypeId = -999;

                AssociatedGeometry.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                AssociatedGeometry.TextHighlightMode = OccurrenceMode.MultipleOccurrence;
                AssociatedGeometry.SuggestionMode = SuggestionMode.Contains;
                AssociatedGeometry.DropDownCornerRadius = 10;
                AssociatedGeometry.HeightRequest = 40;
                AssociatedGeometry.TextSize = fontSize;
                AssociatedGeometry.EnableAutoSize = true;
                AssociatedGeometry.MultiSelectMode = MultiSelectMode.None;
                AssociatedGeometry.ShowSuggestionsOnFocus = true;
                AssociatedGeometry.IsSelectedItemsVisibleInDropDown = false;
                AssociatedGeometry.DropDownBackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGrey"];
                AssociatedGeometry.DropDownTextColor = Color.White;
                AssociatedGeometry.HighlightedTextColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                AssociatedGeometry.DropDownItemHeight = 50;
                AssociatedGeometry.MaximumSuggestion = 50;
                AssociatedGeometry.EnableSelectionIndicator = true;
                AssociatedGeometry.LoadMoreText = "WEITERE ERGEBNISSE";
                AssociatedGeometry.MaximumDropDownHeight = 150;
                AssociatedGeometry.SetAppThemeColor(SfAutoComplete.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                AssociatedGeometry.SetAppThemeColor(SfAutoComplete.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
                AssociatedGeometry.SetAppThemeColor(SfAutoComplete.BorderColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));


                if (queriedrec.geometry_fk != null)
                {
                    var geom = ReferenceGeometry.GetGeometry((int)queriedrec.geometry_fk);
                    var selectedGeomIndex = Geoms.FindIndex(a => a.Id == geom.Id);
                    AssociatedGeometry.SelectedIndex = selectedGeomIndex++;
                }
                else
                {
                    AssociatedGeometry.SelectedIndex = 0;
                }
                AssociatedGeometry.Margin = new Thickness(0, 0, 0, 10);
                AssociatedGeometry.IsEnabled = !ReadOnly;
                if (ReadOnly)
                {
                    AssociatedGeometry.SetAppThemeColor(Label.BackgroundColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                }
                if (Device.RuntimePlatform == Device.Android)
                {
                    AssociatedGeometry.BorderColor = Color.Black;
                }
                AssociatedGeometry.SelectionChanged += DidSelectNewGeometry;

                Assets.Add(AssociatedGeometry);
            }

            var DeleteButton = new Button();
            DeleteCommand = new Command(OnDelete,ValidateDelete);
            DeleteButton.ImageSource = "delete.png";
            DeleteButton.Command = DeleteCommand;
            DeleteButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["DangerButtonStyle"];
            DeleteButton.Margin = new Thickness(0, 10);
            DeleteButton.CornerRadius = 10;
            DeleteButton.TextTransform = TextTransform.Uppercase;
            DeleteButton.WidthRequest = 50;
            var GUIDButton = new Button();
            GUIDCommand = new Command(CopyGUID);
            GUIDButton.Text = "GUID";
            GUIDButton.BackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
            GUIDButton.Command = GUIDCommand;
            GUIDButton.HorizontalOptions = LayoutOptions.FillAndExpand;
            GUIDButton.TextColor = Color.White;
            GUIDButton.Margin = new Thickness(0, 10);
            GUIDButton.CornerRadius = 10;
            GUIDButton.TextTransform = TextTransform.Uppercase;
            GUIDButton.WidthRequest = 250;
            var buttonLayout = new FlexLayout
            {
                Children =
                {
                    DeleteButton, GUIDButton
                }
            };
            buttonLayout.Direction = FlexDirection.Row;
            buttonLayout.JustifyContent = FlexJustify.SpaceAround;
            buttonLayout.HorizontalOptions = LayoutOptions.FillAndExpand;
            buttonLayout.AlignContent = FlexAlignContent.SpaceAround;

            Assets.Add(buttonLayout);

            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            this.PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();

        }

        /// <summary>
        /// Carry out tasks on screen appearing
        /// </summary>
        public void OnAppearing()
        {

        }



        /// <summary>
        /// Check if the save button can be pressed: check whether all of the mandatory components have been filled out
        /// </summary>
        /// <returns>Valid/invalid</returns>
        private bool ValidateSave()
        {
            if (ReadOnly) { return false; }
            var flattenList = Validation.Select(x => x.Value).ToList();
            if (flattenList.Contains(false))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the delete button can be pressed: check if the form is read-only
        /// </summary>
        /// <returns>Valid/invalid</returns>
        private bool ValidateDelete()
        {
            if (ReadOnly) { return false; }
            return true;
        }




        /// <summary>
        /// Cancel, lose any changes and return to the record list
        /// </summary>
        private void OnCancel()
        {
            if (NewRecord) { Record.DeleteRecord(RecId); } //Delete any temporary record
            // This will pop the current page off the navigation stack
            MessagingCenter.Send<FormPageVM>(this, "NavigateBack");
        }

        /// <summary>
        /// Save any changes made to the record and return to the record list
        /// </summary>
        private async void OnSave()
        {
            UpdateAssociatedGeometry(AssociatedGeometry);
            Form.SaveValuesFromFormFields(Assets, RecId);

            MessagingCenter.Send<Xamarin.Forms.Application>(App.Current, "RefreshGeometries");
            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("//Records");
        }

        /// <summary>
        /// Delete this record from the device (after user confirmation)
        /// </summary>
        private async void OnDelete()
        {
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie diese Beobachtung vom Gerät entfernen?", "Abbrechen", "Entfernen");
                if (response == "Entfernen")
                {
                    var rec = conn.GetWithChildren<Record>(RecId);
                    Record.DeleteRecord(rec.Id);
                    await Shell.Current.GoToAsync("//Records");
                }
            }
        }

        /// <summary>
        /// Update the geometry associated with the record with that selected from the picker
        /// </summary>
        /// <param name="choice"></param>
        private async void UpdateAssociatedGeometry(SfAutoComplete choice)
        {
            var proj = Project.FetchCurrentProject();
            if (choice.SelectedIndex > 0)
            {
                var source = (List<ReferenceGeometry>)choice.ItemsSource;
                var geom = source[(int)choice.SelectedIndex] as ReferenceGeometry;
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    queriedrec.geometry_fk = geom.Id;
                    queriedrec.project_fk = proj.Id;
                    if (queriedrec.status != -1)
                    {
                        queriedrec.status = 2;
                    }
                    conn.Update(queriedrec);
                }
            }
            else
            {
                using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                {
                    queriedrec.geometry_fk = null;
                    queriedrec.project_fk = proj.Id;
                    if (queriedrec.status != -1)
                    {
                        queriedrec.status = 2;
                    }
                    conn.Update(queriedrec);
                }
            }
        }

        /// <summary>
        /// Copy the BioDiv GUID to the clipboard and show a confirmation
        /// </summary>
        public async void CopyGUID()
        {
            await Clipboard.SetTextAsync(BDCGUIDtext);
            Device.BeginInvokeOnMainThread(async () =>
            {
                await App.Current.MainPage.DisplayAlert("BDC GUID kopiert", "", "OK");
            });
        }

        /// <summary>
        /// Fill out the date field with today's date
        /// </summary>
        private void FillOutDate(object parameter)
        {
            var dic = (Dictionary<String, Object>)parameter;
            dic.TryGetValue("text", out var textData);
            TextData text = (TextData)textData;
            dic.TryGetValue("date", out var dateObj);
            CustomDatePicker dateField = (CustomDatePicker)dateObj;
            dic.TryGetValue("time", out var timeObj);
            CustomTimePicker timeField = (CustomTimePicker)timeObj;
            var dt = DateTime.Now;
            dateField.NullableDate = dt.Date;
            timeField.NullableDate = new TimeSpan(dt.TimeOfDay.Hours, dt.TimeOfDay.Minutes, 0);
        }


        /// <summary>
        /// Clears the date field
        /// </summary>
        private void ClearDate(object parameter)
        {
            var dic = (Dictionary<String,Object>)parameter;
            dic.TryGetValue("text", out var textData);
            TextData text = (TextData)textData;
            dic.TryGetValue("date", out var dateObj);
            CustomDatePicker dateField = (CustomDatePicker)dateObj;
            dic.TryGetValue("time", out var timeObj);
            CustomTimePicker timeField = (CustomTimePicker)timeObj;
            dateField.NullableDate = null;
            timeField.NullableDate = null;
            DateTime? date = (DateTime?)dateField.NullableDate;
            int valueId = (int)dateField.ValueId;
            if (dateField.Mandatory)
            {
                var empty = (date == null);
                Validation[valueId] = !empty;
                SaveCommand.ChangeCanExecute();
            }

            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                text.value = String.Empty;
            }
        }


        /// <summary>
        /// When text field entries change, we can trigger a reevaluation of the page here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextFieldChanged(object sender, EventArgs e)
        {
            var textField = (CustomEntry)sender;
            int valueId = (int)textField.ValueId;
            if (textField.Mandatory)
            {
                var empty = String.IsNullOrEmpty(textField.Text);
                Validation[valueId] = !empty;
                SaveCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// When numeric field entries change, we can trigger a reevaluation of the page here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumericFieldChanged(object sender, EventArgs e)
        {
            var textField = (CustomEntry)sender;
            int valueId = (int)textField.ValueId;
            if (textField.Mandatory)
            {
                var empty = String.IsNullOrEmpty(textField.Text);
                Validation[valueId] = !empty;
                SaveCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// When date field entries change, we can trigger a reevaluation of the page here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DateFieldChanged(object sender, EventArgs e)
        {
            var dateField = (CustomDatePicker)sender;
            DateTime? date = (DateTime?)dateField.NullableDate;
            int valueId = (int)dateField.ValueId;
            if (dateField.Mandatory)
            {
                var empty = (date==null);
                Validation[valueId] = !empty;
                SaveCommand.ChangeCanExecute();
            }

            if (date != null)
            {
                CustomStackLayout stack = (CustomStackLayout)dateField.Parent;
                foreach (var el in stack.Children)
                {
                    if (el is CustomTimePicker)
                    {
                        CustomTimePicker tp = (CustomTimePicker)el;
                        if (tp.NullableDate == null)
                        {
                            tp.NullableDate = new TimeSpan(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// When time field entries change, we can trigger a reevaluation of the page here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeFieldChanged(object sender, EventArgs e)
        {
            var timeField = (CustomTimePicker)sender;
            TimeSpan? time = (TimeSpan?)timeField.NullableDate;
            int valueId = (int)timeField.ValueId;
            if (timeField.Mandatory)
            {
                var empty = (time == null);
                Validation[valueId] = !empty;
                SaveCommand.ChangeCanExecute();
            }
            if (time != null)
            {
                CustomStackLayout stack = (CustomStackLayout)timeField.Parent;
                foreach (var el in stack.Children)
                {
                    if (el is CustomDatePicker)
                    {
                        CustomDatePicker dp = (CustomDatePicker)el;
                        if (dp.NullableDate == null)
                        {
                            dp.NullableDate = DateTime.Now;
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// When boolean field entries change, we can trigger a reevaluation of the page here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BooleanFieldChanged(object sender, EventArgs e)
        {
            //SaveCommand.ChangeCanExecute();
        }

        /// <summary>
        /// Save the chosen value whenever a value is chosen from a picker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DidSelectFromChoices(object sender, EventArgs e)
        {
            var choice = sender as CustomAutoComplete;
            using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
            {
                var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.Id == choice.ValueId).FirstOrDefault();
                var choiceString = choice.SelectedItem.ToString();
                text.value = choiceString;
                var chosen = conn.Table<FieldChoice>().Select(t => t).Where(mychoice => mychoice.formField_fk == choice.TypeId).Where(mychoice => mychoice.text == choiceString).FirstOrDefault();

                if (chosen != null)
                {
                    text.fieldChoiceId = chosen.choiceId;
                    //conn.Update(text);
                }
                try
                {
                    if (choice.Mandatory)
                    {
                        var empty = String.IsNullOrEmpty(choiceString);
                        Validation[(int)choice.ValueId] = !empty;
                        SaveCommand.ChangeCanExecute();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }

        }

        /// <summary>
        /// Here we can add any changes for when we change the geometry associated with a record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DidSelectNewGeometry(object sender, EventArgs e)
        {

        }
    }
}
