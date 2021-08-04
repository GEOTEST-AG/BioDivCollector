using BioDivCollector.DB.Models.Domain;
using BioDivCollector.PluginContract;
using NetTopologySuite.Geometries;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;

namespace BioDivCollector.StandardValuePlugin
{
    public class StandardValueGenerator : BaseStandardValueGenerator
    {
        private BioDivContext db = new BioDivContext();

        public override string GetName()
        {
            return "StandardValueGenerator";
        }

        public override string GetStandardValue(FormField formField, ReferenceGeometry referenceGeometry, Record r, User user)
        {
            if (formField.StandardValue != null)
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
                    if ((referenceGeometry != null) && (referenceGeometry.Line != null))
                    {
                        List<Area> areaResult = Helper.RawSqlQuery<Area>("select st_length(st_transform(line,2056)) as PolyArea from geometries g where g.geometryid = '" + referenceGeometry.GeometryId + "'", x => new Area { PolyArea = (double)x[0] });
                        return Math.Round(areaResult.First().PolyArea, 2).ToString() + " m";
                    }
                }
                else if (formField.StandardValue.Contains("area()"))
                {
                    if ((referenceGeometry != null) && (referenceGeometry.Polygon != null))
                    {
                        List<Area> areaResult = Helper.RawSqlQuery<Area>("select st_area(st_transform(polygon,2056)) as PolyArea from geometries g where g.geometryid = '" + referenceGeometry.GeometryId + "'", x => new Area { PolyArea = (double)x[0] });
                        return Math.Round(areaResult.First().PolyArea,2).ToString() + " m2";
                    }
                }



            }

            return "";

        }
    }

    public class Area
    {
        public double PolyArea { get; set; }

    }


    public static class Helper
    {
        public static List<T> RawSqlQuery<T>(string query, Func<DbDataReader, T> map)
        {
            using (var context = new BioDivContext())
            {
                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    context.Database.OpenConnection();

                    using (var result = command.ExecuteReader())
                    {
                        var entities = new List<T>();

                        while (result.Read())
                        {
                            entities.Add(map(result));
                        }

                        return entities;
                    }
                }
            }
        }
    }
}
