using DSharpPlus.Entities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Unicord.Universal.Controls;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal
{
    public class MessageViewerFactory
    {
        private static ThreadLocal<MessageViewerFactory> _viewerFactories = new ThreadLocal<MessageViewerFactory>(() => new MessageViewerFactory(75), true);

        private ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, MessageViewer>> _messageViewerCache
            = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, MessageViewer>>();

        private ConcurrentQueue<MessageViewer> ViewerQueue { get; }
            = new ConcurrentQueue<MessageViewer>();

        public MessageViewerFactory(int viewers)
        {
#if DEBUG
            Logger.Log($"Creating new MessageViewerFactory with {viewers} viewers.");
            var watch = Stopwatch.StartNew();
#endif

            for (var i = 0; i < viewers; i++)
            {
                ViewerQueue.Enqueue(new MessageViewer());
            }

#if DEBUG
            Logger.Log($"Created in {watch.Elapsed} ({(double)watch.Elapsed.Milliseconds / viewers}ms/viewer)");
            watch.Stop();
#endif
        }

        public MessageViewer GetViewerForMessage(DiscordMessage message)
        {
            try
            {
                if (_messageViewerCache.TryGetValue(message.Channel.Id, out var channel))
                {
                    if (channel.TryGetValue(message.Id, out var viewer))
                    {
                        if (viewer.Parent is Panel p)
                            p.Children.Remove(viewer);

                        return viewer;
                    }
                }
                else
                {
                    _messageViewerCache[message.Channel.Id] = new ConcurrentDictionary<ulong, MessageViewer>();
                }

                if (ViewerQueue.TryDequeue(out var newv))
                {
                    if (newv.Message != null)
                    {
                        foreach (var things in _messageViewerCache)
                        {
                            things.Value.TryRemove(newv.Message.Id, out _);
                        }
                    }

                    if (newv.Parent is Panel p)
                        p.Children.Remove(newv);

                    newv.Message = message;
                    _messageViewerCache[message.Channel.Id][message.Id] = newv;

                    return newv;
                }

                return new MessageViewer() { Message = message };
            }
            finally
            {
#if DEBUG
                Logger.Log(ViewerQueue.Count);
#endif
            }
        }

        public void RequeueViewer(MessageViewer viewer)
        {
#if DEBUG
            Logger.Log("Requeued Viewer");
#endif

            if (viewer.Parent is Panel p)
                p.Children.Remove(viewer);

            viewer.Unload();
            ViewerQueue.Enqueue(viewer);
        }

        public static MessageViewerFactory GetForCurrentThread()
        {
            return _viewerFactories.Value;
        }
    }
}
