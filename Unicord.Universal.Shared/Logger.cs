
// make sure this only gets included in UWP targets
#if WINDOWS_UWP

using Microsoft.Extensions.Logging;
#if DEBUG
using Microsoft.Extensions.Logging.Debug;
#endif
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace Unicord.Universal
{
    internal static class Logger
    {
        private class WinRTLoggerProvider : ILoggerProvider
        {
            private class WinRTLogger : ILogger, IDisposable
            {
                private readonly LoggingChannel channel;
                private readonly FileLoggingSession session;

                public WinRTLogger(string categoryName, FileLoggingSession session)
                {
                    this.channel = new LoggingChannel(categoryName, new LoggingChannelOptions());
                    this.session = session;
                    session.AddLoggingChannel(channel);
                }

                public IDisposable BeginScope<TState>(TState state) where TState : notnull
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    var fields = new LoggingFields();
                    fields.AddString("Message", formatter(state, exception));
                    this.channel.LogEvent(eventId.ToString(), fields, MapLevel(logLevel));
                }

                private static LoggingLevel MapLevel(LogLevel level) => level switch
                {
                    LogLevel.Trace or LogLevel.Debug => LoggingLevel.Verbose,
                    LogLevel.Information => LoggingLevel.Information,
                    LogLevel.Warning => LoggingLevel.Warning,
                    LogLevel.Error => LoggingLevel.Error,
                    LogLevel.Critical => LoggingLevel.Critical,
                    _ => LoggingLevel.Verbose
                };

                public void Dispose()
                {
                    this.session.RemoveLoggingChannel(this.channel);
                }
            }

            private FileLoggingSession session;
            public WinRTLoggerProvider()
            {
                this.session = new FileLoggingSession("Unicord-" + Guid.NewGuid().ToString());
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new WinRTLogger(categoryName, session);
            }

            public async Task SaveAsync()
            {
                var oldSession = session;
                this.session = new FileLoggingSession("Unicord-" + Guid.NewGuid().ToString());

                await oldSession.CloseAndSaveToFileAsync();
            }

            public void Dispose()
            {
                this.session.Dispose();
            }
        }

        private static WinRTLoggerProvider WinRTProvider
             = new WinRTLoggerProvider();

        public static ILoggerFactory LoggerFactory = new LoggerFactory(new ILoggerProvider[] {
#if DEBUG
            new DebugLoggerProvider(),
#endif
            WinRTProvider
        }, new LoggerFilterOptions()
        {
#if DEBUG
            MinLevel = LogLevel.Debug
#else
            MinLevel = LogLevel.Information
#endif
        });

        private static readonly ILogger InternalLogger
            = LoggerFactory.CreateLogger("Unicord");

        public static ILogger<T> GetLogger<T>() where T : class
            => LoggerFactory.CreateLogger<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(object message, [CallerMemberName] string source = "General")
        {
            InternalLogger.Log(LogLevel.Information, new EventId(100, source), "[{Source}] {Message}", source, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(Exception ex, [CallerMemberName] string memberName = "UnknownMember", [CallerFilePath] string filePath = "Unknown.cs", [CallerLineNumber] int lineNum = 0)
        {
            InternalLogger.Log(LogLevel.Error, ex, "An error occured: {MemberName} @ {FilePath}:{LineNumber}", memberName, filePath, lineNum);
        }

        public static async Task OnSuspendingAsync()
        {
            await WinRTProvider.SaveAsync();
        }
    }

#endif
}
