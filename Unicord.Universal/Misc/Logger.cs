using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Unicord.Universal
{
    internal static class Logger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(object message, [CallerMemberName] string source = "General")
        {
            Trace.WriteLine(message, source);
        }
    }
}

