using BioDivCollector.DB.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BioDivCollector.PluginContract
{
    public abstract class BaseStandardValueGenerator :  IPlugin
    {

        public abstract string GetName();

        public abstract string GetStandardValue(FormField formField, ReferenceGeometry referenceGeometry, Record r, User user);
    }
}
