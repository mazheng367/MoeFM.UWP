using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MoeFM.UWP.Common
{
    public static class JsonHelper
    {
        public static T Parse<T>(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return default(T);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException)
            {
#if DEBUG
                throw;
#endif
                return default(T);
            }
        }

        public static string Stringify<T>(T o)
        {
            return JsonConvert.SerializeObject(o);
        }
    }
}
