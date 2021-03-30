using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BioDivCollector.DB.Models.Domain;

namespace BioDivCollector.DB.Helpers
{
    public class GroupNameUniqueValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            BioDivContext db = (BioDivContext)context.GetService(typeof(BioDivContext));
            if (db.Groups.Where(m=>m.GroupName == value.ToString()).Any())
            {
                return new ValidationResult("Dieser Gruppenname wird bereits verwendet. Bitte einen anderen Gruppennamen wählen");
            }
            return ValidationResult.Success;
        }
    }
}
