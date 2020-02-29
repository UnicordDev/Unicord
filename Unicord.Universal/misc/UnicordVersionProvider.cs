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
            var version = Package.Current.Id.Version;
            return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.106 Safari/537.36 Unicord/{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
