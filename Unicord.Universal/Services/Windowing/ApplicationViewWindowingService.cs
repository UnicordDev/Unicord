using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Models;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Services.Windowing
{
    class ApplicationViewWindowingService : WindowingService
    {
        class ApplicationViewWindowHandle : WindowHandle
        {
            public int Id { get; set; }
        }

        private int _mainWindowId;
        private ConcurrentDictionary<int, ApplicationViewWindowHandle> _windowHandles
             = new ConcurrentDictionary<int, ApplicationViewWindowHandle>();
        private ConcurrentDictionary<int, ulong> _windowChannelDictionary
             = new ConcurrentDictionary<int, ulong>();

        private ConcurrentDictionary<int, bool> ActivatedStates
            = new ConcurrentDictionary<int, bool>();

        public override bool IsSupported => true; // todo: work this out

        public override WindowHandle CurrentWindow
        {
            get
            {
                return GetOrCreateHandleForId(ApplicationView.GetForCurrentView().Id);
            }
        }

        public override void SetMainWindow(UIElement reference)
        {
            _mainWindowId = ApplicationView.GetForCurrentView().Id;

            CoreWindow.GetForCurrentThread().Activated += this.OnWindowActivationChanged;
        }

        public override WindowHandle GetHandle(UIElement reference)
        {
            if (!reference.Dispatcher.HasThreadAccess)
                throw new InvalidOperationException("Can't get handle for another thread!");

            return GetOrCreateHandleForId(ApplicationView.GetForCurrentView().Id);
        }

        public override bool IsMainWindow(WindowHandle handle)
        {
            if (handle is ApplicationViewWindowHandle h)
                return h.Id == _mainWindowId;

            throw new InvalidOperationException("Invalid WindowHandle!");
        }

        public override bool IsCompactOverlay(WindowHandle handle)
        {
            return ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay;
        }

        public override void SetWindowChannel(WindowHandle handle, ulong id)
        {
            if (handle is ApplicationViewWindowHandle h)
                _windowChannelDictionary[h.Id] = id;
            else
                throw new InvalidOperationException("Invalid WindowHandle!");
        }

        public override bool IsChannelVisible(ulong id)
        {
            return _windowChannelDictionary.Any(c => c.Value == id);
        }

        public override bool IsActive(WindowHandle handle)
        {
            return ActivatedStates[((ApplicationViewWindowHandle)handle).Id];
        }

        public override async Task<WindowHandle> OpenChannelWindowAsync(DiscordChannel channel, bool compactOverlay, WindowHandle currentWindow = null)
        {
            if (!IsSupported)
                return null;

            if (await ActivateOtherWindowAsync(channel, currentWindow))
                return null;

            Analytics.TrackEvent("ApplicationViewWindowingService_OpenChannelWindowAsync");

            var viewId = 0;
            var coreView = CoreApplication.CreateNewView();
            await coreView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var coreWindow = coreView.CoreWindow;
                var window = Window.Current;

                coreWindow.Activated += OnWindowActivationChanged;

                var frame = new Frame();
                window.Content = frame;
                window.Activate();

                frame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channel.Id, FullFrame = true });

                var applicationView = ApplicationView.GetForCurrentView();
                viewId = applicationView.Id;

                void OnConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
                {
                    Analytics.TrackEvent("WindowManager_WindowConsolidated");

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

            if (await ApplicationViewSwitcher.TryShowAsViewModeAsync(viewId, compactOverlay ? ApplicationViewMode.CompactOverlay : ApplicationViewMode.Default))
                return GetOrCreateHandleForId(viewId);

            return null;
        }

        private void OnWindowActivationChanged(CoreWindow sender, WindowActivatedEventArgs args)
        {
            var id = ApplicationView.GetApplicationViewIdForWindow(sender);
            ActivatedStates[id] = args.WindowActivationState != CoreWindowActivationState.Deactivated;
        }

        public override async Task<bool> ActivateOtherWindowAsync(DiscordChannel channel, WindowHandle currentWindow = null)
        {
            if (!IsSupported)
                return false;

            var handle = currentWindow as ApplicationViewWindowHandle;
            try
            {
                var window = _windowChannelDictionary.FirstOrDefault(w => w.Value == channel.Id).Key;
                if (window != 0 && window != ApplicationView.GetForCurrentView().Id && window != handle?.Id)
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

        public override async Task CloseAllWindowsAsync()
        {
            _windowChannelDictionary.Clear();
            _windowHandles.Clear();
            _windowHandles[_mainWindowId] = new ApplicationViewWindowHandle() { Id = _mainWindowId };

            var views = CoreApplication.Views.ToList();
            foreach (var view in views)
            {
                await view.ExecuteOnUIThreadAsync(async () =>
                {
                    if (view.IsMain) return;
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                });
            }
        }

        private List<FrameworkElement> _handledElements
            = new List<FrameworkElement>();

        public override void HandleTitleBarForWindow(FrameworkElement titleBar, FrameworkElement contentRoot)
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
                        if (contentRoot != null)
                        {
                            var margin = contentRoot.Margin;
                            void OnApplicationViewBoundsChanged(ApplicationView sender, object args)
                            {
                                var visibleBounds = sender.VisibleBounds;
                                var bounds = coreApplicationView.CoreWindow.Bounds;
                                var occludedHeight = bounds.Height - visibleBounds.Height - statusBar.OccludedRect.Height;
                                contentRoot.Margin = new Thickness(margin.Left, margin.Top, margin.Right, occludedHeight);
                            }

                            applicationView.VisibleBoundsChanged += OnApplicationViewBoundsChanged;
                            OnApplicationViewBoundsChanged(applicationView, null);
                        }

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
                        var originalMargin = titleBar.Margin;

                        // this method captures "titleBar" meaning the GC might not be able to collect it. 
                        void UpdateTitleBarLayout(CoreApplicationViewTitleBar sender, object ev)
                        {
                            titleBar.Height = sender.Height;
                            titleBar.Margin = new Thickness(
                                Math.Max(sender.SystemOverlayLeftInset, originalMargin.Left),
                                originalMargin.Top,
                                Math.Max(sender.SystemOverlayRightInset, originalMargin.Right),
                                originalMargin.Bottom);
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

                        UpdateTitleBarLayout(coreTitleBar, null);
                        coreTitleBar.LayoutMetricsChanged += UpdateTitleBarLayout;
                        titleBar.Unloaded += ElementUnloaded;
                        titleBar.Visibility = Visibility.Visible;

                        if (contentRoot != null)
                            Window.Current.SetTitleBar(titleBar);
                    }
                }
            }
        }

        public override void HandleTitleBarForWindowControls(FrameworkElement cotainerElement, Grid titleBarElement, params FrameworkElement[] controls)
        {
            lock (_handledElements)
            {
                //if (_handledElements.Contains(titleBar))
                //    return;

                var applicationView = ApplicationView.GetForCurrentView();
                var coreApplicationView = CoreApplication.GetCurrentView();

                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    return; // TODO: this
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

                    if (titleBarElement != null)
                    {
                        var originalMargin = titleBarElement.Padding;

                        // this method captures "titleBar" meaning the GC might not be able to collect it. 
                        void UpdateTitleBarLayout(CoreApplicationViewTitleBar sender, object ev)
                        {
                            var overlayLeftInset = sender.IsVisible ? sender.SystemOverlayLeftInset : 0;
                            var overlayRightInset = sender.IsVisible ? sender.SystemOverlayRightInset : 0;

                            titleBarElement.Padding = new Thickness(
                                Math.Max(overlayLeftInset, originalMargin.Left),
                                originalMargin.Top,
                                Math.Max(overlayRightInset, originalMargin.Right),
                                originalMargin.Bottom);

                            Logger.Log($"{sender.SystemOverlayLeftInset} {sender.SystemOverlayRightInset}");

                            foreach (var control in controls)
                            {
                                control.Margin = new Thickness(
                                    overlayLeftInset,
                                    originalMargin.Top,
                                    overlayRightInset,
                                    originalMargin.Bottom);
                            }
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

                        UpdateTitleBarLayout(coreTitleBar, null);
                        coreTitleBar.LayoutMetricsChanged += UpdateTitleBarLayout;
                        coreTitleBar.IsVisibleChanged += UpdateTitleBarLayout;
                        titleBarElement.Unloaded += ElementUnloaded;
                        Window.Current.SetTitleBar(titleBarElement);
                    }
                }
            }
        }

        public override void HandleTitleBarForControl(FrameworkElement element, bool margin = false)
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

        private void ApplyPadding(FrameworkElement element, bool margin, Thickness padding)
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

        private WindowHandle GetOrCreateHandleForId(int id)
            => _windowHandles.TryGetValue(id, out var handle) ? handle : _windowHandles[id] = new ApplicationViewWindowHandle() { Id = id };
    }
}
