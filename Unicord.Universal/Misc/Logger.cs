using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace Unicord.Universal
{
    internal static class Logger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(object message, [CallerMemberName] string source = "General")
        {
            Debug.WriteLine(message, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Exception ex, [CallerMemberName] string memberName = "UnknownMember", [CallerFilePath] string filePath = "Unknown.cs", [CallerLineNumber] int lineNum = 0)
        {
            Crashes.TrackError(ex, new Dictionary<string, string> { ["MemberName"] = memberName, ["FilePath"] = filePath, ["LineNumber"] = lineNum.ToString() });
        }
    }
}

