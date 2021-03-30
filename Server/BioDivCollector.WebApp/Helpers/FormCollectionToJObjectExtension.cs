using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioDivCollector.WebApp.Helpers
{
    public static class FormCollectionToJObjectExtension
    {
        public static JObject ToJObject(this IFormCollection form)
        {
            JObject target = new JObject();
            foreach (var element in form)
            {
                Add(target, element.Key, element.Value);
            }
            return target;

            void Add(JObject jo, string key, StringValues value)
            {
                var chars = new[] { '.', '[' };

                var x = key.IndexOfAny(chars);
                if (x == -1)
                    jo[key] = value.LastOrDefault();
                else
                {
                    var name = key.Substring(0, x);
                    if (key[x] == '.')
                    {
                        var subJo = jo[name] as JObject ?? (JObject)(jo[name] = new JObject());
                        Add(subJo, key.Substring(x + 1), value);
                    }
                    else
                    {
                        var subJa = jo[name] as JArray ?? (JArray)(jo[name] = new JArray());

                        var closeBracketsIndex = key.IndexOf(']', x + 1);
                        var itemIndex = int.Parse(key.Substring(x + 1, closeBracketsIndex - x - 1));
                        while (subJa.Count < itemIndex + 1) subJa.Add(null);

                        if (closeBracketsIndex == key.Length - 1)
                        {
                            subJa[itemIndex] = value.LastOrDefault();
                            return;
                        }
                        if (key[closeBracketsIndex + 1] != '.') throw new Exception();
                        var remainder = key.Substring(closeBracketsIndex + 2);
                        var subJo = subJa[itemIndex] as JObject ?? (JObject)(subJa[itemIndex] = new JObject());
                        Add(subJo, remainder, value);
                    }
                }
            }
        }
    }
}
