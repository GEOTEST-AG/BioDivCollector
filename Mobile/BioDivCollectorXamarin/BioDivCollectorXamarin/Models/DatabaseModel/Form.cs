using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;
using BioDivCollectorXamarin.Controls;

namespace BioDivCollectorXamarin.Models.DatabaseModel
{
    [Table("Form")]
    public class Form
    {
        /// <summary>
        /// Form Database Table Definition
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int formId { get; set; }
        public string title { get; set; }
        public int status { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<FormField> formFields { get; set; }

        [ForeignKey(typeof(Project))]
        public int project_fk { get; set; }



        /// <summary>
        /// Get forms relevant to the current project from the database
        /// </summary>
        /// <returns>List of forms</returns>
        public static List<Form> FetchFormsForProject()
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                try
                {
                    var proj = Project.FetchCurrentProject();
                    var forms = conn.Table<Form>().Where(Form => Form.project_fk == proj.Id).ToList();
                    return forms;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a list of just the form names for the project
        /// </summary>
        /// <returns>List of form names</returns>
        public static List<String> FetchFormNamesForProject()
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                try
                {
                    var proj = Project.FetchCurrentProject();
                    var forms = conn.Table<Form>().Where(Form => Form.project_fk == proj.Id).ToList();
                    var formNames = new List<string>();
                    foreach (var form in forms)
                    {
                        formNames.Add(form.title);
                    }
                    return formNames;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Fetches a form with a given form name. If multiple exist with the same name, it returns the first
        /// </summary>
        /// <param name="formName"></param>
        /// <returns>Form</returns>
        public static Form FetchFormWithFormName(string formName)
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                try
                {
                    var proj = Project.FetchCurrentProject();
                    var form = conn.Table<Form>().Where(Form => Form.project_fk == proj.Id).Where(Form => Form.title == formName).FirstOrDefault();
                    return form;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Get all the parameters and their entry types for any form
        /// </summary>
        /// <param name="formId"></param>
        /// <returns>A list of form fields</returns>
        public static List<FormField> FetchFormFields(int formId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                try
                {
                    var formTemp = conn.Table<Form>().Where(Form => Form.formId == formId).FirstOrDefault();
                    var formType = conn.GetWithChildren<Form>(formTemp.Id);
                    var formFields = formType.formFields.Where(FormField => FormField.useInRecordTitle == true).ToList();
                    return formFields;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Fetch a list of dropdown choice strings for a given dropdown field id
        /// </summary>
        /// <param name="fieldId"></param>
        /// <returns>List of choice strings</returns>
        public static List<string> FetchFormChoicesForDropdown(int fieldId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                try
                {
                    var choices = conn.Table<FieldChoice>().Where(FieldChoice => FieldChoice.formField_fk == fieldId).OrderBy(f => f.order).Select(f => f.text).ToList();
                    return choices;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return null;
        }

        /// <summary>
        /// Saves all the values in a form when the save button is pressed
        /// </summary>
        /// <param name="Assets"></param>
        /// <param name="RecId"></param>
        public static void SaveValuesFromFormFields(List<View> Assets, int RecId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
            {
                foreach (var field in Assets)
                {
                    if (field.GetType() == typeof(CustomEntry))
                    {
                        var txtField = (CustomEntry)field;
                        if (txtField.TypeId == 21) //Numeric data
                        {
                            try
                            {
                                var num = conn.Table<NumericData>().Select(n => n).Where(NumericData => NumericData.record_fk == RecId).Where(NumericData => NumericData.Id == txtField.ValueId).FirstOrDefault();
                                num.value = Convert.ToDouble(txtField.Text);
                                conn.Update(num);
                                Record.UpdateRecord(num.record_fk);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not save data" + e);
                            }

                        }
                        else //Text data
                        {
                            try
                            {
                                var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == txtField.ValueId).FirstOrDefault();
                                text.value = txtField.Text;
                                conn.Update(text);
                                Record.UpdateRecord(text.record_fk);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not save data" + e);
                            }

                        }
                    }
                    else if (field.GetType() == typeof(CustomPicker))
                    {
                        //Save the chosen values from the picker
                        var txtField = (CustomPicker)field;
                        if (txtField.TypeId != -999)
                        {
                            try
                            {
                                if (txtField.SelectedItem != null)
                                {
                                    var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == txtField.ValueId).FirstOrDefault(); //Get the existing data from the database
                                    var choiceString = txtField.SelectedItem.ToString();
                                    text.value = choiceString; //update the text from the field
                                    var chosen = conn.Table<FieldChoice>().Select(t => t).Where(mychoice => mychoice.formField_fk == txtField.TypeId).Where(mychoice => mychoice.text == choiceString).FirstOrDefault(); //Find the corresponding dropdown choice from the database
                                    if (chosen != null)
                                    {
                                        text.fieldChoiceId = chosen.choiceId; //Update the database entry with the dropdown choice id
                                        conn.Update(text);
                                        Record.UpdateRecord(text.record_fk); //Write back to the db
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not save data" + e);
                            }
                        }
                        
                    }
                    else if (field.GetType() == typeof(CustomDatePicker)) //Date values
                    {
                        var dateField = (CustomDatePicker)field;
                        try
                        {
                            var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == dateField.ValueId).FirstOrDefault();
                            if (dateField.NullableDate != null)
                            {
                                var newDate = (DateTime)dateField.NullableDate;
                                text.value = newDate.ToString();
                                
                            }
                            else
                            {
                                text.value = null;
                            }
                            conn.Update(text);


                            Record.UpdateRecord(text.record_fk);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not save data" + e);
                        }
                    }
                    else if (field.GetType() == typeof(CustomStackLayout)) //DateTime values
                    {
                        var stack = field as CustomStackLayout;
                        foreach (var subview in stack.Children)
                        {
                            if (subview.GetType() == typeof(CustomDatePicker)) //Write the date
                            {
                                var dateField = (CustomDatePicker)subview;
                                try
                                {
                                    var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == dateField.ValueId).FirstOrDefault();
                                    
                                    if (dateField.NullableDate != null)
                                    {
                                        var oldDate = DateTime.Now;
                                        if (text.value != null && text.value != string.Empty)
                                        {
                                            oldDate = DateTime.ParseExact(text.value, "yyyy-MM-ddTHH:mm:sszzz", null);
                                        }

                                        var time = new TimeSpan(oldDate.TimeOfDay.Hours, oldDate.TimeOfDay.Minutes, 0);
                                        var newDate = (DateTime)dateField.NullableDate;
                                        var newDateTime = newDate.Date + time;
                                        text.value = newDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
                                    }
                                    else
                                    {
                                        text.value = String.Empty;
                                    }
                                    conn.Update(text);
                                    Record.UpdateRecord(text.record_fk);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Could not save data" + e);
                                }
                            }
                            else if (subview.GetType() == typeof(CustomTimePicker)) //Write the time
                            {
                                var timeField = (CustomTimePicker)subview;
                                try
                                {
                                    var text = conn.Table<TextData>().Select(t => t).Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == timeField.ValueId).FirstOrDefault();
                                    if (timeField.NullableDate != null)
                                    {
                                        var oldDate = DateTime.Now;
                                        if (text.value != null && text.value != string.Empty)
                                        {
                                            oldDate = DateTime.ParseExact(text.value, "yyyy-MM-ddTHH:mm:sszzz", null);
                                        }

                                        var time = new TimeSpan(oldDate.TimeOfDay.Hours, oldDate.TimeOfDay.Minutes, 0);
                                        var newDate = oldDate.Date + timeField.Time;
                                        text.value = newDate.ToString("yyyy-MM-ddTHH:mm:sszzz");
                                    }
                                    else
                                    {
                                        text.value = String.Empty;
                                    }

                                    conn.Update(text);
                                    Record.UpdateRecord(text.record_fk);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Could not save data" + e);
                                }
                            }
                        }

                    }
                    else if (field.GetType() == typeof(CustomCheckBox)) //Tick boxes
                    {
                        try
                        {
                            var checkField = (CustomCheckBox)field;
                            var boolValue = conn.Table<BooleanData>().Select(n => n).Where(BooleanData => BooleanData.record_fk == RecId).Where(BooleanData => BooleanData.Id == checkField.ValueId).FirstOrDefault();
                            boolValue.value = checkField.IsChecked;
                            conn.Update(boolValue);
                            Record.UpdateRecord(boolValue.record_fk);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not save data" + e);
                        }

                    }
                }

            }

        }

    }


    /// <summary>
    /// Form field database table definition
    /// </summary>
    [Table("FormField")]
    public class FormField
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int fieldId { get; set; }
        public int typeId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string source { get; set; }

        public int order { get; set; }
        public bool mandatory { get; set; }
        public bool useInRecordTitle { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<FieldChoice> fieldChoices { get; set; }

        [ForeignKey(typeof(Form))]
        public int form_fk { get; set; }
    }


    /// <summary>
    /// Field choice (dropdown choice) database table definition
    /// </summary>
    [Table("FieldChoice")]
    public class FieldChoice
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int choiceId { get; set; }
        public string text { get; set; }
        public int order { get; set; }

        [ForeignKey(typeof(FormField))]
        public int formField_fk { get; set; }
    }
}
