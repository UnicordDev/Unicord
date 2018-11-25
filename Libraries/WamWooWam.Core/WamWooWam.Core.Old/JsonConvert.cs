using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamWooWam.Core
{
    public static class JsonConvertAsync
    {
        public static async Task<T> DeserializeObjectAsync<T>(string json)
        {
            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(json));
        }

        public static async Task<string> SerializeObjectAsync(object value)
        {
            return await Task.Factory.StartNew(() => JsonConvert.SerializeObject(value));
        }
    }
}
