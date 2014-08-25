using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json.Linq
{
    public static class JsonLdExtensions
    {
        public static T GetScalar<T>(this JObject obj, Uri property)
        {
            JToken val;
            if (!obj.TryGetValue(property.AbsoluteUri, out val))
            {
                return default(T);
            }
            val = UnwrapObject(UnwrapArray(val));
            return val.Value<T>();
        }

        public static Uri GetScalarUri(this JObject obj, Uri property)
        {
            var str = GetScalar<string>(obj, property);
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }
            return new Uri(str);
        }

        public static IEnumerable<T> GetArray<T>(this JObject obj, Uri property)
        {
            JToken val;
            if (!obj.TryGetValue(property.AbsoluteUri, out val))
            {
                yield break;
            }
            if (val.Type == JTokenType.Array)
            {
                foreach (var item in ((JArray)val))
                {
                    yield return UnwrapObject(item).Value<T>();
                }
            }
            else
            {
                yield return val.Value<T>();
            }
        }

        private static JToken UnwrapArray(JToken val)
        {
            if (val.Type == JTokenType.Array)
            {
                return ((JArray)val)[0];
            }
            return val;
        }

        private static JToken UnwrapObject(JToken val)
        {
            if (val.Type == JTokenType.Object)
            {
                JObject obj = (JObject)val;
                var prop = obj.Property("@value");
                if (prop != null)
                {
                    return prop.Value;
                }
            }
            return val;
        }
    }
}
