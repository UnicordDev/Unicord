using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models;
using Unicord.Universal.Shared;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core.Preview;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Unicord.Universal.Services.Windowing
{
    class AppWindowWindowingService : WindowingService
    {
        class AppWindowHandle : WindowHandle
        {
            public AppWindowHandle(UIContext context)
            {
                Context = context;
            }

            public AppWindow Window { get; set; }

            public UIContext Context { get; set; }
        }


        private object defaultUIContext = new object();
        private AppWindowHandle _mainWindowHandle;

        private ConcurrentDictionary<object, AppWindowHandle> _uiContextDictionary
            = new ConcurrentDictionary<object, AppWindowHandle>();

        private ConcurrentDictionary<AppWindowHandle, ulong> _windowChannelDictionary
            = new ConcurrentDictionary<AppWindowHandle, ulong>();

        public override bool IsSupported
            => true;

        public AppWindowWindowingService()
        {

        }

        public override void SetMainWindow(UIElement reference)
        {
            _mainWindowHandle = new AppWindowHandle(reference.UIContext);
            _uiContextDictionary[reference.UIContext ?? defaultUIContext] = _mainWindowHandle;
        }

        public override WindowHandle GetHandle(UIElement reference)
        {
            if (!_uiContextDictionary.TryGetValue(reference.UIContext ?? defaultUIContext, out var handle))
            {
                handle = _uiContextDictionary[reference.UIContext ?? defaultUIContext] = new AppWindowHandle(reference.UIContext);
            }

            return handle;
        }

        public override bool IsMainWindow(WindowHandle handle)
        {
            return handle == _mainWindowHandle;
        }

        public override void SetWindowChannel(WindowHandle handle, ulong id)
        {
            if (handle is AppWindowHandle h)
                _windowChannelDictionary[h] = id;
            else
                throw new InvalidOperationException("Invalid handle!");
        }

        public override bool IsChannelVisible(ulong id)
        {
            return _windowChannelDictionary.Any(c => c.Value == id);
        }

        public override async Task<WindowHandle> OpenChannelWindowAsync(DiscordChannel channel, WindowHandle currentWindow = null)
        {
            if (!IsSupported)
                return null;

            if (await ActivateOtherWindowAsync(channel, currentWindow))
                return null;

            Analytics.TrackEvent("AppWindowWindowingService_OpenChannelWindowAsync");

            var window = await AppWindow.TryCreateAsync();
            window.RequestMoveRelativeToCurrentViewContent(new Point(276, 0));
            window.RequestSize(new Size(Window.Current.Bounds.Width - 276, Window.Current.Bounds.Height));
            window.Title = NotificationUtils.GetChannelHeaderName(channel);

            var handle = (AppWindowHandle)CreateOrUpdateHandle(window);

            var frame = new Frame();
            frame.Navigate(typeof(MainPage), new MainPageArgs() { ChannelId = channel.Id, FullFrame = true });

            ElementCompositionPreview.SetAppWindowContent(window, frame);
            var frame2 = ElementCompositionPreview.GetAppWindowContent(window);

            await window.TryShowAsync();
            window.Closed += (o, e) =>
            {
                _uiContextDictionary.TryRemove(handle.Context, out _);
                _windowChannelDictionary.TryRemove(handle, out _);

                handle = null;
                frame.Content = null;
                window = null;
            };

            HandleTitleBarForWindow(null, frame);

            return handle;
        }

        public override async Task<bool> ActivateOtherWindowAsync(DiscordChannel channel, WindowHandle currentWindow = null)
        {
            if (!IsSupported)
                return false;

            var handle = currentWindow as AppWindowHandle;
            try
            {
                var window = _windowChannelDictionary.FirstOrDefault(w => w.Value == channel.Id).Key;
                if (window != null && window.Window != handle?.Window && window.Window != null)
                {
                    await window.Window.TryShowAsync();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        public override Task CloseAllWindowsAsync()
        {
            return Task.CompletedTask;
        }

        private ApplicationViewWindowingService _legacyWindowingService
            = new ApplicationViewWindowingService();
        private List<FrameworkElement> _handledElements
           = new List<FrameworkElement>();

        public override void HandleTitleBarForWindow(FrameworkElement titleBar, FrameworkElement contentRoot)
        {
            lock (_handledElements)
            {
                //if (_handledElements.Contains(titleBar))
                //    return;

                var handle = (AppWindowHandle)GetHandle(contentRoot);
                if (handle.Window == null)
                {
                    //_legacyWindowingService.HandleTitleBarForWindow(titleBar, contentRoot);
                }
                else
                {
                    var baseTitleBar = handle.Window.TitleBar;
                    baseTitleBar.ExtendsContentIntoTitleBar = true;
                    baseTitleBar.ButtonBackgroundColor = Colors.Transparent;
                    baseTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    baseTitleBar.ButtonForegroundColor = (Color?)contentRoot.Resources["SystemChromeAltLowColor"];
                    baseTitleBar.ButtonInactiveForegroundColor = (Color?)contentRoot.Resources["SystemChromeAltLowColor"];

                    if (titleBar != null)
                    {
                        titleBar.Height = 42;

                        // i *believe* this handles it? not 100% sure
                        void ElementUnloaded(object sender, RoutedEventArgs e)
                        {
                            var fel = sender as FrameworkElement;
                            fel.Unloaded -= ElementUnloaded;

                            lock (_handledElements)
                            {
                                _handledElements.Remove(fel);
                            }
                        }

                        titleBar.Unloaded += ElementUnloaded;
                        titleBar.Visibility = Visibility.Visible;
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

                var handle = (AppWindowHandle)GetHandle(element);
                if (handle.Window == null)
                {
                    _legacyWindowingService.HandleTitleBarForControl(element, margin);
                }
                else
                {
                    var titleBar = handle.Window.TitleBar;
                    var padding = new Thickness(0, 42, 0, 0);

                    ApplyPadding(element, margin, padding);
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

        private WindowHandle CreateOrUpdateHandle(AppWindow window)
        {
            if (!_uiContextDictionary.TryGetValue(window.UIContext, out var handle))
            {
                handle = _uiContextDictionary[window.UIContext] = new AppWindowHandle(window.UIContext);
            }

            handle.Window = window;
            return handle;
        }
    }
}
