using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioDivCollectorXamarin.Controls;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensionsAsync.Extensions;
using Xamarin.Forms;

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
        public static async Task<List<Form>> FetchFormsForProject()
        {
            var conn = App.ActiveDatabaseConnection;
                try
                {
                    var proj = await Project.FetchCurrentProject();
                    var forms = await conn.Table<Form>().Where(Form => Form.project_fk == proj.Id).ToListAsync();
                    return forms;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return null;
        }

        /// <summary>
        /// Get forms relevant to the current project from the database
        /// </summary>
        /// <returns>List of forms</returns>
        public static async Task<List<Form>> FetchFormsForProject(int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var forms = await conn.Table<Form>().Where(Form => Form.project_fk == projectId).ToListAsync();
                return forms;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Gets a list of just the form names for the project
        /// </summary>
        /// <returns>List of form names</returns>
        public static async Task<List<String>> FetchFormNamesForProject()
        {
            var conn = App.ActiveDatabaseConnection;
            try
                {
                    var proj = await Project.FetchCurrentProject();
                    var forms = await conn.Table<Form>().Where(Form => Form.project_fk == proj.Id).ToListAsync();
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
            return null;
        }

        /// <summary>
        /// Fetches a form with a given form name. If multiple exist with the same name, it returns the first
        /// </summary>
        /// <param name="formName"></param>
        /// <returns>Form</returns>
        public static async Task<Form> FetchFormWithFormName(string formName)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var proj = await Project.FetchCurrentProject();
                var form = await conn.Table<Form>().Where(Form => Form.project_fk == proj.Id).Where(Form => Form.title == formName).FirstOrDefaultAsync();
                return form;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Get all the parameters and their entry types for any form
        /// </summary>
        /// <param name="formId"></param>
        /// <returns>A list of form fields</returns>
        public static async Task<List<FormField>> FetchFormFields(int formId)
        {
            var conn = App.ActiveDatabaseConnection;
                try
                {
                    var projekt = await Project.FetchCurrentProject();
                    var formTemp = await conn.Table<Form>().Where(Form => Form.formId == formId).Where(Form => Form.project_fk == projekt.Id).FirstOrDefaultAsync();
                if (formTemp!= null)
                {

                var formType = await conn.GetWithChildrenAsync<Form>(formTemp.Id);
                    var formFields = formType.formFields.Where(FormField => FormField.useInRecordTitle == true).ToList();
                    return formFields;
                }
                else
                {
                    return null;
                }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            return null;
        }

        /// <summary>
        /// Fetches the form for a specific database id
        /// </summary>
        /// <param name="formDbId"></param>
        /// <returns>The Form</returns>
        public static async Task<Form> FetchForm(int formDbId)
        {
            var conn = App.ActiveDatabaseConnection;
                try
                {
                    var form = await conn.Table<Form>().Where(Form => Form.Id == formDbId).FirstOrDefaultAsync();
                    return form;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            return null;
        }

        /// <summary>
        /// Fetch the Form by its Form id and project id 
        /// </summary>
        /// <param name="fieldId"></param>
        /// <returns>List of choice strings</returns>
        public static async Task<Form> FetchFormByFormAndProjectId(int formId, int projectId)
        {
            var conn = App.ActiveDatabaseConnection;
                try
                {
                    var form = await conn.Table<Form>().Where(Form => Form.formId == formId).Where(Form => Form.project_fk == projectId).FirstOrDefaultAsync();
                    return form;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            return null;
        }

        /// <summary>
        /// Fetch a list of dropdown choice strings for a given dropdown field id
        /// </summary>
        /// <param name="fieldId"></param>
        /// <returns>List of choice strings</returns>
        public static async Task<List<FieldChoice>> FetchFormChoicesForDropdown(int fieldId)
        {
            var conn = App.ActiveDatabaseConnection;
            try
            {
                var choices = await conn.Table<FieldChoice>().Where(FieldChoice => FieldChoice.formField_fk == fieldId).OrderBy(f => f.order).ToListAsync();
                return choices;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        /// <summary>
        /// Saves all the values in a form when the save button is pressed
        /// </summary>
        /// <param name="Assets"></param>
        /// <param name="RecId"></param>
        public static async Task SaveValuesFromFormFields(List<View> Assets, string RecId)
        {
            var conn = App.ActiveDatabaseConnection;
                foreach (var field in Assets)
                {
                    if (field.GetType() == typeof(CustomEntry))
                    {
                        var txtField = (CustomEntry)field;
                        if (txtField.TypeId == 21) //Numeric data
                        {
                            try
                            {
                                var num = await conn.Table<NumericData>().Where(NumericData => NumericData.record_fk == RecId).Where(NumericData => NumericData.Id == txtField.ValueId).FirstOrDefaultAsync();
                                num.value = Convert.ToDouble(txtField.Text);
                                await conn.UpdateAsync(num);
                                await Record.UpdateRecord(num.record_fk);
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
                                var text = await conn.Table<TextData>().Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == txtField.ValueId).FirstOrDefaultAsync();
                                if (text != null)
                                {
                                    text.value = txtField.Text;
                                    await conn.UpdateAsync(text);
                                    await Record.UpdateRecord(text.record_fk);
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Could not save data" + e);
                            }

                        }
                    }
                    else if (field.GetType() == typeof(CustomAutoComplete))
                    {
                        //Save the chosen values from the picker
                        var txtField = (CustomAutoComplete)field;
                        if (txtField.TypeId != -999)
                        {
                            try
                            {
                                if (txtField.SelectedItem != null)
                                {
                                    var text = await conn.Table<TextData>().Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == txtField.ValueId).FirstOrDefaultAsync(); //Get the existing data from the database
                                    var choice = txtField.SelectedIndex;
                                    var choiceString = txtField.AutoCompleteSource[choice];
                                    var chosen = await conn.Table<FieldChoice>().Where(mychoice => mychoice.formField_fk == txtField.TypeId).Where(mychoice => mychoice.text == choiceString).FirstOrDefaultAsync(); //Find the corresponding dropdown choice from the database
                                    if (chosen != null)
                                    {
                                        text.value = chosen.text; //update the text from the choice
                                        text.fieldChoiceId = chosen.choiceId; //Update the database entry with the dropdown choice id
                                        await conn.UpdateAsync(text);
                                        await Record.UpdateRecord(text.record_fk); //Write back to the db
                                    }
                                }
                                else
                                {
                                    var text = await conn.Table<TextData>().Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == txtField.ValueId).FirstOrDefaultAsync();
                                    text.value = null; //update the text from the choice
                                    text.fieldChoiceId = null; //Update the database entry with the dropdown choice id
                                    await conn.UpdateAsync(text);
                                    await Record.UpdateRecord(text.record_fk); //Write back to the db
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
                            var text = await conn.Table<TextData>().Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == dateField.ValueId).FirstOrDefaultAsync();
                            if (dateField.NullableDate != null)
                            {
                                var newDate = (DateTime)dateField.NullableDate;
                                text.value = newDate.ToString();
                                
                            }
                            else
                            {
                                text.value = null;
                            }
                            await conn.UpdateAsync(text);


                            await Record.UpdateRecord(text.record_fk);
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
                                    var text = await conn.Table<TextData>().Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == dateField.ValueId).FirstOrDefaultAsync();
                                    
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
                                    await conn.UpdateAsync(text);
                                    await Record.UpdateRecord(text.record_fk);
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
                                    var text = await conn.Table<TextData>().Where(TextData => TextData.record_fk == RecId).Where(TextData => TextData.Id == timeField.ValueId).FirstOrDefaultAsync();
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

                                    await conn.UpdateAsync(text);
                                    await Record.UpdateRecord(text.record_fk);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Could not save data" + e);
                                }
                            }
                            else if (subview.GetType() == typeof(CustomCheckBox)) //Tick boxes
                            {
                                try
                                {
                                    var checkField = (CustomCheckBox)subview;
                                    var boolValue = await conn.Table<BooleanData>().Where(BooleanData => BooleanData.record_fk == RecId).Where(BooleanData => BooleanData.Id == checkField.ValueId).FirstOrDefaultAsync();
                                    boolValue.value = checkField.IsChecked;
                                    await conn.UpdateAsync(boolValue);
                                    await Record.UpdateRecord(boolValue.record_fk);
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
                            var boolValue = await conn.Table<BooleanData>().Where(BooleanData => BooleanData.record_fk == RecId).Where(BooleanData => BooleanData.Id == checkField.ValueId).FirstOrDefaultAsync();
                            boolValue.value = checkField.IsChecked;
                            await conn.UpdateAsync(boolValue);
                            await Record.UpdateRecord(boolValue.record_fk);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not save data" + e);
                        }
                    }
                }
        }

        public static async Task<string> DetermineText(Record record, TextData text, string standardValue)
        {
            var value = text.value;
            if (standardValue == null)
            {
                if (value == null)
                { return String.Empty; }
                return value;
            }
            if ((value == null && standardValue != String.Empty) || standardValue.Substring(0, 1) == "=")
            {
                if (standardValue.Substring(0, 1) == "=")
                {
                    standardValue = standardValue.Remove(0, 1);
                }
                if (standardValue == "userfullname()")
                {
                    return App.CurrentUser.firstName + " " + App.CurrentUser.name;
                }
                else if (standardValue == "now()")
                {
                    return DateTime.Now.ToShortDateString();
                }
                else if (standardValue == "userid()")
                {
                    return App.CurrentUser.userId;
                }
                else if (standardValue == "length()")
                {
                    ReferenceGeometry geom = await ReferenceGeometry.GetGeometry((int)record.geometry_fk);
                    var length = ReferenceGeometry.CalculateLengthOfLine(geom);
                    return length.ToString("F2");
                }
                else if (standardValue == "area()")
                {
                    ReferenceGeometry geom = await ReferenceGeometry.GetGeometry((int)record.geometry_fk);
                    var area = ReferenceGeometry.CalculateAreaOfPolygon(geom);
                    return area.ToString("F2");
                }
                else
                {
                    return String.Empty;
                }
            }
            return value;
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
        public string standardValue { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<FieldChoice> fieldChoices { get; set; }

        [ForeignKey(typeof(Form))]
        public int form_fk { get; set; }

        public static async Task<FormField> FetchFormFieldByFieldIdAndFormKey(int fieldId, int form_fk)
        {
            var conn = App.ActiveDatabaseConnection;
            return await conn.Table<FormField>().Where(FormField => FormField.fieldId == fieldId).Where(FormField => FormField.form_fk == form_fk).FirstOrDefaultAsync();
        }

       
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
