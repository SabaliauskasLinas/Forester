using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class JsonHelper
    {
        public static string JsonToString(this object data)
        {
            if (data != null)
                return JsonConvert.SerializeObject(data);
            else
                return null;
        }

        public static T JsonToObject<T>(this string json)
        {
            if (string.IsNullOrEmpty(json))
                return default(T);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static Dictionary<string, object> JsonToDict(this string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new Dictionary<string, object>();

            return ToDictionary(JObject.Parse(json));
        }

        private static Dictionary<string, object> ToDictionary(this JObject @object)
        {
            var result = @object.ToObject<Dictionary<string, object>>();

            var JObjectKeys = (from r in result
                               let key = r.Key
                               let value = r.Value
                               where value?.GetType() == typeof(JObject)
                               select key).ToList();

            var JArrayKeys = (from r in result
                              let key = r.Key
                              let value = r.Value
                              where value?.GetType() == typeof(JArray)
                              select key).ToList();

            foreach (var key in JArrayKeys)
            {
                var values = new List<object>();
                var innerObjects = ((JArray)result[key]).Children().ToList();
                foreach (var val in innerObjects)
                {
                    if (val is JValue v)
                        values.Add(v.Value);
                    else
                        values.Add(val);
                }
                result[key] = values.ToArray();
            }
            foreach (var key in JObjectKeys)
            {
                var obj = result[key] as JObject;
                result[key] = ToDictionary(obj);
            }

            return result;
        }
    }
}
