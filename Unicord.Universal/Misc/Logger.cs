using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace Unicord.Universal
{
    internal static class Logger
    {
        public static ILoggerFactory LoggerFactory = new LoggerFactory(new ILoggerProvider[] {
#if DEBUG
            new DebugLoggerProvider(),
#endif
        }, new LoggerFilterOptions()
        {
#if DEBUG
            MinLevel = LogLevel.Debug
#else
            MinLevel = LogLevel.Information
#endif
        });

        private static ILogger InternalLogger = LoggerFactory.CreateLogger("Unicord");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(object message, [CallerMemberName] string source = "General")
        {
            InternalLogger.Log(LogLevel.Information, "[{Source}] {Message}", source, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Exception ex, [CallerMemberName] string memberName = "UnknownMember", [CallerFilePath] string filePath = "Unknown.cs", [CallerLineNumber] int lineNum = 0)
        {
            Crashes.TrackError(ex, new Dictionary<string, string> { ["MemberName"] = memberName, ["FilePath"] = filePath, ["LineNumber"] = lineNum.ToString() });
        }
    }
}

