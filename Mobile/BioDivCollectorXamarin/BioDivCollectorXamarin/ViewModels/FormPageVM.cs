﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Controls;
using BioDivCollectorXamarin.Models.DatabaseModel;
using BioDivCollectorXamarin.Views;
using SQLiteNetExtensionsAsync.Extensions;
using Syncfusion.SfAutoComplete.XForms;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.ViewModels
{
    class FormPageVM : BaseViewModel
    {
        public INavigation Navigation { get; set; }

        public List<View> Assets { get; set; }
        
        private bool dataFormFinished;
        public bool DataFormFinished
        {
            get { return dataFormFinished; }
            set
            {
                dataFormFinished = value;
                OnPropertyChanged();
                MessagingCenter.Send<Xamarin.Forms.Application>(App.Current, "UpdateDataForm");
            }
        }

        public string RecId;
        public int FormId;
        public int? GeomId;
        private bool NewRecord;
        private bool GoingToImageEditor;

        /// <summary>
        /// A bool used as an activity indicator
        /// </summary>
        private bool activity;
        public bool Activity
        {
            get { return activity; }
            set
            {
                activity = value;
                OnPropertyChanged("Activity");
            }
        }
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
        public FormPageVM(string recId, int formId, int? geomId, INavigation navigation)
        {
            FormId = formId;
            if (geomId == 0)
            {
                GeomId = null;
            }
            else
            {
                GeomId = geomId;
            }
            
            if (recId != null) 
            { 
                RecId = recId; 
            }
            Navigation = navigation;
            Assets = new List<View>();

            DeleteCommand = new Command(OnDelete, ValidateDelete);
            GUIDCommand = new Command(CopyGUID);
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            Activity = true;

            if (FormId != 0)
            {
                //Task.Run(async () =>
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await CreateForm(RecId, FormId, GeomId);
                    DataFormFinished = true;
                    Activity = false;
                });
            }

            MessagingCenter.Unsubscribe<Application>(App.Current, "BackButtonPressed");
            MessagingCenter.Subscribe<Application>(App.Current, "BackButtonPressed", (sender) =>
            {
                //If the BackButton is pressed, use the same behaviour as if the CancelButton is pressed
                OnCancel();
            });

        }

        public async Task CreateForm(string recId, int formId, int? geomId)
        {
            FormId = formId;
            GeomId = geomId;
            FormId = formId;
            //TempAssets = new List<View>();

            //Get the record and its corresponding variable values
            if (recId != null && recId != String.Empty)
            {
                queriedrec = await Record.FetchRecord(recId);
                if (queriedrec == null)
                {
                    queriedrec = await Record.CreateRecord(formId, geomId);
                    RecId = queriedrec.recordId;
                    NewRecord = true;
                }
                else
                {
                    ReadOnly = queriedrec.readOnly;
                    RecId = recId;
                }
            }
            else
            {
                queriedrec = await Record.CreateRecord(formId, geomId);
                RecId = queriedrec.recordId;
                NewRecord = true;
            }

            App.CurrentRoute = $"//Records/Form?formid={formId}&recid={RecId}&geomid={geomId}";
            var fontSize = 16;
            var txts = new List<TextData>();
            var nums = new List<NumericData>();
            var bools = new List<BooleanData>();
                txts = await TextData.FetchTextDataByRecordId(recId);
                nums = await NumericData.FetchNumericDataByRecordId(recId);
                bools = await BooleanData.FetchBooleanDataByRecordId(recId);
            //Compile the GUID
            BDCGUIDtext = "<<BDC><" + queriedrec.recordId + ">>";

            var projekt = await Project.FetchCurrentProject();
            //var formTemp = conn.Table<Form>().Where(Form => Form.formId == formId).Where(Form => Form.project_fk == projekt.Id).FirstOrDefault();
            var formTemp = await Form.FetchFormByFormAndProjectId(formId, projekt.Id);
            var conn = App.ActiveDatabaseConnection;
            var tempFormType = await conn.GetWithChildrenAsync<Form>(formTemp.Id);
            var formFields = tempFormType.formFields;
            formFields.OrderBy(f => f.order);

            tempFormType.formFields = formFields;
            formType = tempFormType;

            foreach (var formField in formType.formFields)
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
                    if (formField.typeId != 31) //Add label next to checkbox for boolean
                    {
                        Assets.Add(label);
                    }

                    if (formField.typeId == 11 || formField.typeId == 61)
                    {
                        try
                        {
                            var textList = await TextData.FetchTextDataByRecordId(recId);
                            var text = textList.Where(TextData => TextData.formFieldId == formField.fieldId).FirstOrDefault();
                            var textField = new CustomEntry();

                            if (text == null)
                            {
                                //CreateNew
                                var txt = new TextData { textId = Guid.NewGuid().ToString(), title = String.Empty, value = null, formFieldId = formField.fieldId, record_fk = RecId };
                                await conn.InsertAsync(txt);
                                txts.Add(txt);
                                queriedrec.texts = txts;
                                text = txt;
                            }
                            var localReadOnly = ReadOnly;
                            if (formField.standardValue != null && formField.standardValue.Substring(0, 1) == "=") { localReadOnly = true; }
                            textField.Text = await Form.DetermineText(queriedrec, text, formField.standardValue);
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
                            var textList = await TextData.FetchTextDataByRecordId(recId);
                            var text = textList.Where(TextData => TextData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
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
                                await conn.InsertAsync(txt);
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
                            var screenWidth = Application.Current.MainPage.Width;
                            if (screenWidth > 350)
                            {
                                dateField.WidthRequest = 170;
                            }
                            else
                            {
                                dateField.WidthRequest = 130;
                            }
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
                            if (screenWidth > 350)
                            {
                                timeField.WidthRequest = 70;
                            }
                            else
                            {
                                timeField.WidthRequest = 60;
                            }

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
                            var textList = await TextData.FetchTextDataByRecordId(RecId);
                            var text = textList.Where(TextData => TextData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                            var dropField = new CustomAutoComplete();
                            if (text == null)
                            {
                                //CreateNew
                                var txt = new TextData { textId = Guid.NewGuid().ToString(), title = String.Empty, value = String.Empty, formFieldId = formField.fieldId, record_fk = RecId };
                                    await conn.InsertAsync(txt);
                                txts.Add(txt);
                                queriedrec.texts = txts;
                                text = txt;
                            }
                            dropField.SetAppThemeColor(SfAutoComplete.BackgroundColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightBackgroundColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkBackgroundColor"]);
                            dropField.SetAppThemeColor(SfAutoComplete.TextColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
                            dropField.SetAppThemeColor(SfAutoComplete.ClearButtonColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
                            if (Device.RuntimePlatform == Device.Android)
                            {
                                dropField.SetAppThemeColor(SfAutoComplete.BorderColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
                            }
                            else if (Device.RuntimePlatform == Device.iOS)
                            {
                                dropField.SetAppThemeColor(SfAutoComplete.BorderColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
                            }

                            List<FieldChoice> fieldChoices = await Form.FetchFormChoicesForDropdown(formField.Id);

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
                            dropField.TextSize = fontSize;

                            dropField.DropDownBackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGrey"];
                            dropField.DropDownTextColor = Color.White;
                            dropField.HighlightedTextColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                            dropField.DropDownItemHeight = 50;
                            dropField.MaximumSuggestion = 50;
                            dropField.EnableSelectionIndicator = true;
                            dropField.SuggestionBoxPlacement = SuggestionBoxPlacement.Auto;
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
                            var numList = await NumericData.FetchNumericDataByRecordId(RecId);
                               var num = numList.Where(NumericData => NumericData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                            var textField = new CustomEntry();

                            var numericBehaviour = new Xamarin.CommunityToolkit.Behaviors.NumericValidationBehavior();
                            numericBehaviour.MaximumDecimalPlaces = 2;
                            textField.Behaviors.Add(numericBehaviour);
                            if (num == null)
                            {
                                //CreateNew
                                var nm = new NumericData { numericId = Guid.NewGuid().ToString(), title = String.Empty, value = null, formFieldId = formField.fieldId, record_fk = RecId };
                                await conn.InsertAsync(nm);
                                nums.Add(nm);
                                queriedrec.numerics = nums;
                                num = nm;

                            }

                            if (num.value == null)
                            {
                                textField = new CustomEntry { Text = String.Empty };
                            }
                            else
                            {
                                textField = new CustomEntry { Text = String.Format("{0:0.##}", (double)num.value) };
                            }

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
                            var boolList = await BooleanData.FetchBooleanDataByRecordId(RecId);
                            var boolValue = boolList.Where(BooleanData => BooleanData.formFieldId == formField.fieldId).Take(1).FirstOrDefault();
                            var checkBox = new CustomCheckBox();
                            if (boolValue == null)
                            {
                                //CreateNew
                                boolValue = new BooleanData { booleanId = Guid.NewGuid().ToString(), title = String.Empty, value = false, formFieldId = formField.fieldId, record_fk = RecId };
                                await conn.InsertAsync(boolValue);
                                bools.Add(boolValue);
                                queriedrec.booleans = bools;
                            }
                            if (boolValue != null)
                            {
                                checkBox.IsChecked = (bool)boolValue.value;
                            }
                            checkBox.Margin = new Thickness(0, 0, 0, 0);
                            checkBox.ValueId = boolValue.Id;
                            checkBox.TypeId = formField.typeId;
                            checkBox.IsEnabled = !ReadOnly;
                            checkBox.Color = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                            if (ReadOnly) { checkBox.Color = Color.LightGray; }
                            checkBox.CheckedChanged += BooleanFieldChanged;
                            checkBox.VerticalOptions = LayoutOptions.Fill;
                            checkBox.HorizontalOptions = LayoutOptions.Start;
                            label.VerticalOptions = LayoutOptions.Fill;
                            label.VerticalTextAlignment = TextAlignment.Center;
                            label.Margin = new Thickness(0, 0, 0, 0);
                            var stack = new CustomStackLayout()
                            {
                                Orientation = StackOrientation.Horizontal,
                                HorizontalOptions = LayoutOptions.Start,
                                VerticalOptions = LayoutOptions.Fill,
                                Children =
                                {
                                    checkBox,label
                                }
                            };
                            stack.Margin = new Thickness(0, 0, 0, 10);
                            Assets.Add(stack);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not create field" + e);
                        }

                    }
                    else if (formField.typeId == 81)
                    {
                        var view = new StackLayout();
                        view.HorizontalOptions = LayoutOptions.FillAndExpand;
                        view.Margin = 0;

                        var flexview = new FlexLayout();
                        flexview.Margin = 0;
                        flexview.Direction = FlexDirection.Row;
                        flexview.JustifyContent = FlexJustify.Start;
                        flexview.Wrap = FlexWrap.Wrap;
                        flexview.AlignContent = FlexAlignContent.SpaceBetween;
                        flexview.AlignItems = FlexAlignItems.Start;
                        var images = await GetImages(formField.fieldId);
                        view.HorizontalOptions = LayoutOptions.FillAndExpand;

                        foreach (BioDivImage image in images)
                        {
                            if (image != null && File.Exists(image.Source.ToString().Replace("File: ", String.Empty)))
                            {
                                image.FormFieldId = formField.fieldId;
                                var stackView = new StackLayout();
                                stackView.Orientation = StackOrientation.Horizontal;
                                stackView.HorizontalOptions = LayoutOptions.Center;
                                stackView.VerticalOptions = LayoutOptions.Center;
                                stackView.HeightRequest = 150;
                                stackView.WidthRequest = 300;
                                stackView.Margin = 10;
                                image.HorizontalOptions = LayoutOptions.Fill;
                                image.VerticalOptions = LayoutOptions.Center;
                                stackView.Children.Add(image);
                                var button = new CameraButton();
                                button.HorizontalOptions = LayoutOptions.End;
                                button.VerticalOptions = LayoutOptions.Center;
                                button.ImageSource = new FontImageSource() { FontFamily = "Material", Glyph = "\ue872", Color = Color.White };
                                button.WidthRequest = 50;
                                button.Style = (Style)Xamarin.Forms.Application.Current.Resources["DangerButtonStyle"];
                                button.HeightRequest = 50;
                                button.Margin = new Thickness(5, 0, 0, 0);
                                button.CornerRadius = 10;
                                button.FormFieldId = formField.fieldId;
                                button.RecordId = RecId;
                                button.BinaryId = image.BinaryId;
                                button.Clicked += Delete_Button_Clicked;
                                stackView.Children.Add(button);
                                flexview.Children.Add(stackView);
                            }
                        }
                        view.Children.Add(flexview);

                        var newbutton = new CameraButton();
                        newbutton.HorizontalOptions = LayoutOptions.End;
                        newbutton.ImageSource = new FontImageSource() { FontFamily = "Material", Glyph = "\ue43e", Color = Color.White };
                        newbutton.BackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
                        newbutton.HorizontalOptions = LayoutOptions.FillAndExpand;
                        newbutton.Margin = new Thickness(0, 0, 0, 20);
                        newbutton.CornerRadius = 10;
                        newbutton.VerticalOptions = LayoutOptions.Start;
                        newbutton.HeightRequest = 50;
                        newbutton.FormFieldId = formField.fieldId;
                        newbutton.RecordId = RecId;
                        newbutton.Clicked += New_Image_Button_Clicked;
                        view.Children.Add(newbutton);
                        Assets.Add(view);
                    }
                }
            }



            var geomlabel = new Label();
            geomlabel.Text = "Zugeordnete Geometrie";
            geomlabel.FontAttributes = FontAttributes.Bold;
            geomlabel.Margin = new Thickness(0, 10, 0, 0);
            Assets.Add(geomlabel);

            AssociatedGeometry = new CustomAutoComplete();
            var geomList = await ReferenceGeometry.GetAllGeometries();
            Geoms = geomList.Where(g => g.status < 3).OrderBy(g => g.geometryName).ToList();
            foreach (var gm in Geoms)
            {
                if (gm.geometryName == null)
                {
                    gm.geometryName = String.Empty; //Avoid a crash on android from null strings
                }
            }
            var general = new ReferenceGeometry() 
            { 
                geometryName = "Allgemeine Beobachtung" 
            };
            var refGeoms = Geoms;
            refGeoms.Insert(0, general);
            AssociatedGeometry.AutoCompleteSource = refGeoms.Select(c => c.geometryName).ToList();
            AssociatedGeometry.ItemsSource = refGeoms;
            AssociatedGeometry.SelectedItem = new Binding("geometryId");
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
            AssociatedGeometry.SetAppThemeColor(SfAutoComplete.ClearButtonColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
            if (Device.RuntimePlatform == Device.Android)
            {
                AssociatedGeometry.SetAppThemeColor(SfAutoComplete.BorderColorProperty, (Color)Xamarin.Forms.Application.Current.Resources["LightTextColor"], (Color)Xamarin.Forms.Application.Current.Resources["DarkTextColor"]);
            }
            else if (Device.RuntimePlatform == Device.iOS)
            {
                AssociatedGeometry.SetAppThemeColor(SfAutoComplete.BorderColorProperty, Color.FromRgb(0.95, 0.95, 0.95), Color.FromRgb(0.2, 0.2, 0.2));
            }

            if (queriedrec.geometry_fk != null)
            {
                var geom = await ReferenceGeometry.GetGeometry((int)queriedrec.geometry_fk);
                var selectedGeomIndex = Geoms.FindIndex(a => a.Id == geom.Id);
                AssociatedGeometry.SelectedIndex = selectedGeomIndex;
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
            AssociatedGeometry.SelectionChanged += DidSelectNewGeometry;

            Assets.Add(AssociatedGeometry);



            var DeleteButton = new Button();
            DeleteButton.ImageSource = new FontImageSource() { FontFamily = "Material", Glyph = "\ue872", Color = Color.White };
            DeleteButton.Command = DeleteCommand;
            DeleteButton.Style = (Style)Xamarin.Forms.Application.Current.Resources["DangerButtonStyle"];
            DeleteButton.Margin = new Thickness(0, 10, 5, 10);
            DeleteButton.CornerRadius = 10;
            DeleteButton.TextTransform = TextTransform.Uppercase;
            DeleteButton.WidthRequest = 50;

            var GUIDButton = new Button();
            GUIDButton.Text = "GUID";
            GUIDButton.BackgroundColor = (Color)Xamarin.Forms.Application.Current.Resources["BioDivGreen"];
            GUIDButton.Command = GUIDCommand;
            GUIDButton.HorizontalOptions = LayoutOptions.FillAndExpand;
            GUIDButton.TextColor = Color.White;
            GUIDButton.Margin = new Thickness(5, 10, 0, 10);
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


            this.PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();

        }


        public async void OnAppearing()
        {
            //Check if it was a new record when the new image button was pressed
            var newRecPrefs = Preferences.Get("newrecord", false);
            if (newRecPrefs || NewRecord) { NewRecord = true; }
            Preferences.Set("newrecord", false);
        }

        /// <summary>
        /// Carry out tasks on leaving the view
        /// </summary>
        public async Task OnDisappearing()
        {
            if (NewRecord && !GoingToImageEditor)
            {
                await Record.DeleteRecord(RecId); //Delete any temporary record
            }
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
        private async void OnCancel()
        {
            if (NewRecord)
            {
                await Record.DeleteRecord(RecId); //Delete any temporary record
            }
            // This will pop the current page off the navigation stack
            MessagingCenter.Send<Application>(App.Current, "NavigateBack");
        }

        /// <summary>
        /// Save any changes made to the record and return to the record list
        /// </summary>
        private async void OnSave()
        {
            NewRecord = false;
            await UpdateAssociatedGeometry(AssociatedGeometry);
            await Form.SaveValuesFromFormFields(Assets, RecId);

            MessagingCenter.Send<Xamarin.Forms.Application>(App.Current, "RefreshGeometries");
            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("//Records",true);
        }

        /// <summary>
        /// Delete this record from the device (after user confirmation)
        /// </summary>
        private async void OnDelete()
        {
            var conn = App.ActiveDatabaseConnection;
                var response = await App.Current.MainPage.DisplayActionSheet("Möchten Sie diese Beobachtung vom Gerät entfernen?", "Abbrechen", "Entfernen");
                if (response == "Entfernen")
                {
                    var rec = await conn.GetWithChildrenAsync<Record>(RecId);
                    await Record.DeleteRecord(rec.recordId);
                    await Shell.Current.GoToAsync("//Records",true);
                }
        }

        /// <summary>
        /// Update the geometry associated with the record with that selected from the picker
        /// </summary>
        /// <param name="choice"></param>
        private async Task UpdateAssociatedGeometry(SfAutoComplete choice)
        {
            var proj = await Project.FetchCurrentProject();
            if (choice?.SelectedIndex > 0)
            {
                var source = (List<ReferenceGeometry>)choice.ItemsSource;
                var geom = source[(int)choice.SelectedIndex] as ReferenceGeometry;
                queriedrec.geometry_fk = geom.Id;
                queriedrec.project_fk = proj.Id;
                if (queriedrec.status != -1)
                {
                    queriedrec.status = 2;
                }
                var conn = App.ActiveDatabaseConnection;
                await conn.UpdateAsync(queriedrec);
            }
            else
            {
                    queriedrec.geometry_fk = null;
                    queriedrec.project_fk = proj.Id;
                    if (queriedrec.status != -1)
                    {
                        queriedrec.status = 2;
                    }
                    var conn = App.ActiveDatabaseConnection;
                    await conn.UpdateAsync(queriedrec);
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
            var dic = (Dictionary<String, Object>)parameter;
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

            //using (SQLiteConnection conn = new SQLiteConnection(Preferences.Get("databaseLocation", "")))
                text.value = String.Empty;
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
                var empty = (date == null);
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
        private async void DidSelectFromChoices(object sender, EventArgs e)
        {
            var choice = sender as CustomAutoComplete;
            var conn = App.ActiveDatabaseConnection;
                var text = await conn.Table<TextData>().Where(TextData => TextData.Id == choice.ValueId).FirstOrDefaultAsync();
                
            if (choice.SelectedItem != null)
            {
                var choiceString = choice.SelectedItem.ToString();
                text.value = choiceString;
                var chosen = await conn.Table<FieldChoice>().Where(mychoice => mychoice.formField_fk == choice.TypeId).Where(mychoice => mychoice.text == choiceString).FirstOrDefaultAsync();

                if (chosen != null)
                {
                    text.fieldChoiceId = chosen.choiceId;
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                text.fieldChoiceId = null;
                text.value = null;
                SaveCommand.ChangeCanExecute();
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


        private async Task<List<Image>> GetImages(int fieldId)
        {
            var rec = await Record.FetchRecord(RecId);
            var binaryIds = await BinaryData.GetBinaryDataIds(rec.recordId, fieldId);
//            if (binaryIds == null || binaryIds.Count == 0)
//            { binaryIds = new List<string>() { Guid.NewGuid().ToString() }; }
            var images = new List<Image>();
            foreach (var binaryId in binaryIds)
            {
                if (!GoingToImageEditor)
                {
                    NewRecord = false;
                }
                
                var image = new BioDivImage();
                image.BinaryId = binaryId;
                image.HorizontalOptions = LayoutOptions.Start;
                image.MinimumHeightRequest = 100;
                image.MinimumWidthRequest = 100;

                try
                {
                    var directory = DependencyService.Get<FileInterface>().GetImagePath();
                    string filepath = Path.Combine(directory, binaryId + ".jpg");
                    image.Source = ImageSource.FromFile(filepath);
                }
                catch (Exception e)
                {
                    image.Source = new FontImageSource() { FontFamily = "Material", Glyph = "\uf116", Color = Color.Gray };
                }

                image.Aspect = Aspect.AspectFit;
                var gesture = new TapGestureRecognizer() { NumberOfTapsRequired = 1 };
                gesture.Tapped += Gesture_Tapped;
                image.GestureRecognizers.Add(gesture);
                images.Add(image);
            }

            return images;
        }

        private async void Gesture_Tapped(object sender, EventArgs e)
        {
            var image = (BioDivImage)sender;
            var imageEditor = new SfImageEditorPage(image.FormFieldId, image.BinaryId, RecId);
            var button = new Xamarin.Forms.ToolbarItem();
            button.Text = "Schliessen";
            button.Command = new Command(() =>
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("..", true);
                });
            });
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.Navigation.PushAsync(imageEditor, true);
            });
        }

        private void Delete_Button_Clicked(object sender, EventArgs e)
        {
            NewRecord = false;
            var button = sender as CameraButton;
            StackLayout layout = (StackLayout)button.Parent;
            
            Device.BeginInvokeOnMainThread(async () =>
            {
                var deleteResponse = await App.Current.MainPage.DisplayAlert("Bild Löschen", "Wollen Sie dieses Bild wirklich löschen?", "Löschen", "Abbrechen",FlowDirection.RightToLeft);
                if (deleteResponse == true)
                {
                    await layout.FadeTo(0, 250, null);
                    await BinaryData.DeleteBinary(button.BinaryId);
                    await Record.UpdateRecord(button.RecordId);
                    MessagingCenter.Send<Application>(App.Current, "PhotoDeleted");
                }
            });
        }

        private async void New_Image_Button_Clicked(object sender, EventArgs e)
        {
            GoingToImageEditor = true;
            if (NewRecord) { Preferences.Set("newrecord",true); }
            var button = sender as CameraButton;
            var rec = await Record.FetchRecord(RecId);

            if (rec == null) 
            {
                rec = await Record.CreateRecord(FormId, GeomId); 
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.Navigation.PushAsync(new SfImageEditorPage(button.FormFieldId, null, RecId), true);
            });
        }
    }


    public class CameraButton : Button
    {
        public string RecordId;
        public int FormFieldId;
        public string BinaryId;
    }

    public class BioDivImage : Image
    {
        public string BinaryId;
        public int FormFieldId;
    }
}
