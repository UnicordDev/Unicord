using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSharpPlus.CommandsNext.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IncludeAttachmentsAttribute : Attribute
    {
        // This is a positional argument
        public IncludeAttachmentsAttribute() { }
    }
}
