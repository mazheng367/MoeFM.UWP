using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFM.UWP.Common
{
    public static class MoeAppCache
    {
        private static readonly Dictionary<string, object> AppCache = new Dictionary<string, object>();

        //private const int MaxCache = 10;

        /// <summary>
        /// 保存数据到缓存临时缓存中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public static void SaveData(string key, object data)
        {
            if (data == null) return;
            if (!AppCache.ContainsKey(key) || AppCache[key] == null)
            {
                AppCache.Add(key, data);
            }
        }

        public static object GetData(string key)
        {
            if (!AppCache.ContainsKey(key)) return null;
            return AppCache[key];
        }

        public static T GetData<T>(string key)
        {
            var o = GetData(key);
            if (o == null) return default(T);
            return (T) o;
        }

        public static bool Remove(string key)
        {
            return AppCache.ContainsKey(key) && AppCache.Remove(key);
        }

        public static void RemoveLeft(string keyLeft)
        {
            try
            {
                var keys = AppCache.Keys.Where(s => !string.IsNullOrEmpty(s) && s.StartsWith(keyLeft)).ToList();
                foreach (var k in keys)
                {
                    AppCache.Remove(k);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
