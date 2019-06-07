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
        private static ConcurrentDictionary<CoreWindow, ulong> _windowChannelDictionary
             = new ConcurrentDictionary<CoreWindow, ulong>();

        private static List<FrameworkElement> _handledElements
             = new List<FrameworkElement>();

        private static ThemeListener _notifier;
        private static Window _mainWindow;

        public static IEnumerable<ulong> VisibleChannels
            => _windowChannelDictionary.Values;

        public static bool MultipleWindowsSupported =>
            AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop";

        public static bool IsMainWindow =>
            Window.Current == _mainWindow;

        public static void SetMainWindow(Window window)
        {
            if (_mainWindow == null)
                _mainWindow = window;
        }

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

        public static async Task OpenChannelWindowAsync(DiscordChannel channel, ApplicationViewMode mode = ApplicationViewMode.Default)
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
                ThemeManager.LoadCurrentTheme(frame.Resources);

                window.Content = frame;
                window.Activate();

                frame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channel.Id, FullFrame = true });

                var applicationView = ApplicationView.GetForCurrentView();
                viewId = applicationView.Id;

                void OnConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
                {
                    if (sender.Id == viewId)
                    {
                        if (args.IsAppInitiated)
                            return;

                        // make sure we never accidentally clean up the main view
                        if (Window.Current == _mainWindow)
                            return;

                        MessageViewer.CleanupTimer();

                        sender.Consolidated -= OnConsolidated;
                        _windowChannelDictionary.TryRemove(CoreWindow.GetForCurrentThread(), out _);
                    }
                }

                applicationView.Consolidated += OnConsolidated;
            });

            //var prefs = ViewModePreferences.CreateDefault(ApplicationViewMode.Default);
            await ApplicationViewSwitcher.TryShowAsViewModeAsync(viewId, mode);
        }

        public static void HandleTitleBarForWindow(FrameworkElement titleBar)
        {
            lock (_handledElements)
            {
                if (_handledElements.Contains(titleBar))
                    return;

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

        public static void HandleTitleBarForGrid(Grid element)
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
                        element.Padding = new Thickness(0, rect.Height, 0, 0);
                    }
                }
                else
                {
                    var coreTitleBar = coreApplicationView.TitleBar;

                    // this method captures "element" meaning the GC might not be able to collect it. 
                    void UpdateTitleBarLayout(CoreApplicationViewTitleBar titleBar, object ev)
                    {
                        element.Padding = new Thickness(0, titleBar.Height, 0, 0);
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
                    element.Unloaded += ElementUnloaded;

                    UpdateTitleBarLayout(coreTitleBar, null);
                }
            }
        }

        // for some reason Grid doesn't inherit from Control???
        public static void HandleTitleBarForControl(Control element, bool margin = false)
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
                        if (margin)
                            element.Margin = new Thickness(0, rect.Height, 0, 0);
                        else
                            element.Padding = new Thickness(0, rect.Height, 0, 0);
                    }
                }
                else
                {
                    var coreTitleBar = coreApplicationView.TitleBar;

                    // this method captures "element" meaning the GC might not be able to collect it. 
                    void UpdateTitleBarLayout(CoreApplicationViewTitleBar titleBar, object ev)
                    {
                        if (margin)
                            element.Margin = new Thickness(0, titleBar.Height, 0, 0);
                        else
                            element.Padding = new Thickness(0, titleBar.Height, 0, 0);
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
                    element.Unloaded += ElementUnloaded;

                    UpdateTitleBarLayout(coreTitleBar, null);
                }
            }
        }
    }
}
