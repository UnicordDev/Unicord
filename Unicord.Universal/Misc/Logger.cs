using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
#if !STORE
            Debug.WriteLine(message, source);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Exception ex, [CallerMemberName] string memberName = "UnknownMember", [CallerFilePath] string filePath = "Unknown.cs", [CallerLineNumber] int lineNum = 0)
        {
            Crashes.TrackError(ex, new Dictionary<string, string> { ["MemberName"] = memberName, ["FilePath"] = filePath, ["LineNumber"] = lineNum.ToString() });
        }
    }
}

