using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Converters
{
    public class Static
    {
        public static bool NotNull(object obj)
            => obj != null;

        public static bool Not(bool b)
            => !b;
    }
}
