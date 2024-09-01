using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Unicord.Universal.Misc
{
    class UnicordVersionProvider : IVersionProvider
    {
        public string GetVersionString()
        {
            var version = 70 + ((DateTime.Now.Year - 2020) * 12) + DateTime.Now.Month;
            return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version}.0.4280.141 Safari/537.36";
        }
    }
}
