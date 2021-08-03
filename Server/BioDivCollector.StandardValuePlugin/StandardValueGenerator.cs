using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using System;
using System.Linq;

namespace BioDivCollector.StandardValuePlugin
{
    public class StandardValueGenerator : BaseStandardValueGenerator
    {
        public override string GetName()
        {
            return "StandardValueGenerator";
        }

        public override string GetStandardValue(FormField formField, ReferenceGeometry referenceGeometry, Record r, User user)
        {
            if (formField.StandardValue!=null)
            {
                // Check standard values which could be overwritten. If there is already a value, return this
                if ((!formField.StandardValue.StartsWith("=")) && (r.TextData.Where(m => m.FormFieldId == formField.FormFieldId).Any()))
                {
                    return r.TextData.Where(m => m.FormFieldId == formField.FormFieldId).First().Value;
                }

                // First all the standard values which replaces current value
                if (formField.StandardValue.ToLower().Contains("now()"))
                {
                    return DateTime.Now.ToString("dd.MM.yyyy");
                }
                else if (formField.StandardValue.Contains("userfullname()"))
                {
                    return user.FirstName + " " + user.Name;
                }
                else if (formField.StandardValue.Contains("userid()"))
                {
                    return user.UserId;
                }
                else if (formField.StandardValue.Contains("length()"))
                {
                    if ((referenceGeometry!=null) && (referenceGeometry.Line != null))
                    {
                        return referenceGeometry.Line.Length.ToString();
                    }
                }
                else if (formField.StandardValue.Contains("area()"))
                {
                    if ((referenceGeometry != null) && (referenceGeometry.Polygon != null))
                    {
                        return referenceGeometry.Polygon.Area.ToString();
                    }
                }

                
                
            }

            return "";

        }
    }
}
