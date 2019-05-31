using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Controls;
using Unicord.Universal.Models;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Utilities
{
    internal static class WindowManager
    {
        private static ConcurrentDictionary<CoreWindow, ulong> _windowChannelDictionary
             = new ConcurrentDictionary<CoreWindow, ulong>();

        public static IEnumerable<ulong> VisibleChannels
            => _windowChannelDictionary.Values;

        internal static void SetChannelForCurrentWindow(ulong id)
        {
            _windowChannelDictionary[CoreWindow.GetForCurrentThread()] = id;
        }

        public static async Task<bool> ActivateOtherWindow(DiscordChannel channel)
        {
            var window = _windowChannelDictionary.FirstOrDefault(w => w.Value == channel.Id).Key;
            if (window != null)
            {
                var id = ApplicationView.GetApplicationViewIdForWindow(window);
                await ApplicationViewSwitcher.SwitchAsync(id);
                return true;
            }

            return false;
        }

        public static async Task OpenChannelWindowAsync(DiscordChannel channel)
        {
            if (await ActivateOtherWindow(channel))
                return;

            var viewId = 0;
            var coreView = CoreApplication.CreateNewView();
            await coreView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var coreWindow = coreView.CoreWindow;
                var window = Window.Current;

                var frame = new Frame();

                window.Content = frame;
                window.Activate();

                frame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channel.Id, FullFrame = true });

                var applicationView = ApplicationView.GetForCurrentView();
                viewId = applicationView.Id;

                void OnConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
                {
                    if (sender.Id == viewId)
                    {
                        if (window.Content is Frame f)
                        {
                            f.FindChild<MainPage>()?.RootFrame.Navigate(typeof(Page), null);
                            f.Navigate(typeof(Page), null);
                        }

                        sender.Consolidated -= OnConsolidated;
                        _windowChannelDictionary.TryRemove(coreWindow, out _);
                    }
                }

                applicationView.Consolidated += OnConsolidated;
            });

            //var prefs = ViewModePreferences.CreateDefault(ApplicationViewMode.Default);
            await ApplicationViewSwitcher.TryShowAsViewModeAsync(viewId, ApplicationViewMode.Default);
        }

        private static void ApplicationView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            if (Window.Current.Content is Frame f)
            {
                f.FindChild<MainPage>()?.RootFrame.Navigate(typeof(Page), null);
                f.Navigate(typeof(Page), null);
            }

            MessageViewer.CleanupTimer();

            sender.Consolidated -= ApplicationView_Consolidated;
            _windowChannelDictionary.TryRemove(CoreWindow.GetForCurrentThread(), out _);
        }
    }
}
