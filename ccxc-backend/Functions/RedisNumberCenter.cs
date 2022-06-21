using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Functions
{
    public static class RedisNumberCenter
    {
        public const string RedisPrefix = "/ccxc-backend/runtimecache/";
        public static async Task SetInt(string key, int value)
        {
            var cache = DbFactory.GetCache();
            var rkey = RedisPrefix + key;
            await cache.PutString(rkey, value.ToString());
        }

        public static async Task<int> GetInt(string key)
        {
            var cache = DbFactory.GetCache();
            var rkey = RedisPrefix + key;
            var intString = await cache.GetString(rkey);
            _ = int.TryParse(intString, out var value);
            return value;
        }
    }
}
