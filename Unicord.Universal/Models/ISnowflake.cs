using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Models
{
    public interface ISnowflake
    {
        ulong Id { get; }
    }
}
