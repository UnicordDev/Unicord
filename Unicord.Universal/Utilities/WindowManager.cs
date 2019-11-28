using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using Unicord.Universal.Controls;
using Unicord.Universal.Models;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Utilities
{
    internal static class WindowManager
    {
        private static ConcurrentDictionary<int, ulong> _windowChannelDictionary
             = new ConcurrentDictionary<int, ulong>();

        private static List<FrameworkElement> _handledElements
             = new List<FrameworkElement>();

        private static ThemeListener _notifier;
        private static int _mainWindowId;
        private static bool _mainWindowSet;

        public static IEnumerable<ulong> VisibleChannels
            => _windowChannelDictionary.Values;

        public static bool MultipleWindowsSupported =>
            AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop";

        public static bool IsMainWindow =>
             _mainWindowSet && ApplicationView.GetForCurrentView().Id == _mainWindowId;

        public static void SetMainWindow()
        {
            if (!_mainWindowSet)
            {
                _mainWindowSet = true;
                _mainWindowId = ApplicationView.GetForCurrentView().Id;
            }
        }

        internal static void SetChannelForCurrentWindow(ulong id)
        {
            if (IsMainWindow)
                App.LocalSettings.Save("LastViewedChannel", id);
            _windowChannelDictionary[ApplicationView.GetForCurrentView().Id] = id;
        }

        public static async Task<bool> ActivateOtherWindow(DiscordChannel channel)
        {
            if (!MultipleWindowsSupported)
                return false;

            try
            {
                var window = _windowChannelDictionary.FirstOrDefault(w => w.Value == channel.Id).Key;
                if (window != 0 && window != ApplicationView.GetForCurrentView().Id)
                {
                    await ApplicationViewSwitcher.SwitchAsync(window);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        public static async Task OpenChannelWindowAsync(DiscordChannel channel, ApplicationViewMode mode = ApplicationViewMode.Default)
        {
            if (!MultipleWindowsSupported)
                return;

            if (await ActivateOtherWindow(channel))
                return;

            var viewId = 0;
            var coreView = CoreApplication.CreateNewView();
            await coreView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                coreView.Properties["channel_id"] = channel.Id;

                var coreWindow = coreView.CoreWindow;
                var window = Window.Current;
                try { ThemeManager.LoadCurrentTheme(App.Current.Resources); } catch { }

                var frame = new Frame();
                window.Content = frame;
                window.Activate();

                frame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channel.Id, FullFrame = true, ViewMode = mode });

                var applicationView = ApplicationView.GetForCurrentView();
                viewId = applicationView.Id;

                void OnConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
                {
                    if (sender.Id == viewId && sender.Id != _mainWindowId)
                    {
                        if (args.IsAppInitiated)
                            return;

                        sender.Consolidated -= OnConsolidated;
                        _windowChannelDictionary.TryRemove(sender.Id, out _);
                    }
                }

                applicationView.Consolidated += OnConsolidated;
            });

            await ApplicationViewSwitcher.TryShowAsViewModeAsync(viewId, mode);
        }

        public static void HandleTitleBarForWindow(FrameworkElement titleBar)
        {
            lock (_handledElements)
            {
                //if (_handledElements.Contains(titleBar))
                //    return;

                var applicationView = ApplicationView.GetForCurrentView();
                var coreApplicationView = CoreApplication.GetCurrentView();

                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        statusBar.BackgroundOpacity = 0;
                        statusBar.ForegroundColor = (Color?)Application.Current.Resources["SystemChromeAltLowColor"];

                        if (titleBar != null)
                        {
                            titleBar.Height = statusBar.OccludedRect.Height;
                        }

                        applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
                    }
                }
                else
                {
                    var coreTitleBar = coreApplicationView.TitleBar;
                    coreTitleBar.ExtendViewIntoTitleBar = true;

                    var baseTitlebar = applicationView.TitleBar;
                    baseTitlebar.ButtonBackgroundColor = Colors.Transparent;
                    baseTitlebar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    baseTitlebar.ButtonForegroundColor = (Color?)Application.Current.Resources["SystemChromeAltLowColor"];
                    baseTitlebar.ButtonInactiveForegroundColor = (Color?)Application.Current.Resources["SystemChromeAltLowColor"];

                    if (titleBar != null)
                    {
                        // this method captures "titleBar" meaning the GC might not be able to collect it. 
                        void UpdateTitleBarLayout(CoreApplicationViewTitleBar sender, object ev)
                        {
                            titleBar.Height = sender.Height;
                        }

                        // i *believe* this handles it? not 100% sure
                        void ElementUnloaded(object sender, RoutedEventArgs e)
                        {
                            coreTitleBar.LayoutMetricsChanged -= UpdateTitleBarLayout;
                            (sender as FrameworkElement).Unloaded -= ElementUnloaded;

                            lock (_handledElements)
                            {
                                _handledElements.Remove(sender as FrameworkElement);
                            }
                        }

                        coreTitleBar.LayoutMetricsChanged += UpdateTitleBarLayout;
                        titleBar.Unloaded += ElementUnloaded;
                        titleBar.Visibility = Visibility.Visible;

                        Window.Current.SetTitleBar(titleBar);
                    }
                }
            }
        }

        private static void _notifier_ThemeChanged(ThemeListener sender)
        {

        }

        // for some reason Grid doesn't inherit from Control???
        public static void HandleTitleBarForControl(FrameworkElement element, bool margin = false)
        {
            lock (_handledElements)
            {
                if (_handledElements.Contains(element))
                    return;

                var applicationView = ApplicationView.GetForCurrentView();
                var coreApplicationView = CoreApplication.GetCurrentView();

                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        var rect = statusBar.OccludedRect;
                        var padding = new Thickness(0, rect.Height, 0, 0);
                        ApplyPadding(element, margin, padding);
                    }
                }
                else
                {
                    var coreTitleBar = coreApplicationView.TitleBar;

                    // this method captures "element" meaning the GC might not be able to collect it. 
                    void UpdateTitleBarLayout(CoreApplicationViewTitleBar titleBar, object ev)
                    {
                        var padding = new Thickness(0, titleBar.Height, 0, 0);
                        ApplyPadding(element, margin, padding);
                    }

                    // i *believe* this handles it? not 100% sure
                    void ElementUnloaded(object sender, RoutedEventArgs e)
                    {
                        coreTitleBar.LayoutMetricsChanged -= UpdateTitleBarLayout;
                        (sender as FrameworkElement).Unloaded -= ElementUnloaded;

                        lock (_handledElements)
                        {
                            _handledElements.Remove(sender as FrameworkElement);
                        }

                        element = null;
                    }

                    coreTitleBar.LayoutMetricsChanged += UpdateTitleBarLayout;
                    element.Unloaded += ElementUnloaded;

                    UpdateTitleBarLayout(coreTitleBar, null);
                }
            }
        }

        private static void ApplyPadding(FrameworkElement element, bool margin, Thickness padding)
        {
            if (margin)
            {
                element.Margin = padding;
            }
            else
            {
                if (element is Grid grid)
                    grid.Padding = padding;
                if (element is Control control)
                    control.Padding = padding;
            }
        }
    }
}