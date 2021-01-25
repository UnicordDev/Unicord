using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Services.Windowing;
using Windows.UI.Xaml;

namespace Unicord.Universal.Services
{
    /// <summary>
    /// An opaque class to represent a Window
    /// </summary>
    public abstract class WindowHandle { }

    public abstract class WindowingService
    {

        private static Lazy<WindowingService> _windowingServiceLazy = new Lazy<WindowingService>(() => new ApplicationViewWindowingService(), true);
        public static WindowingService Current
            => _windowingServiceLazy.Value;

        public abstract bool Supported { get; }

        public abstract void SetMainWindow(UIElement reference);

        public abstract WindowHandle GetHandle(UIElement reference);

        public abstract bool IsMainWindow(WindowHandle handle);

        public abstract void SetWindowChannel(WindowHandle handle, ulong id);

        public abstract bool IsChannelVisible(ulong id);

        public abstract Task<bool> ActivateOtherWindowAsync(DiscordChannel channel, WindowHandle currentWindow = null);

        public abstract Task<WindowHandle> OpenChannelWindowAsync(DiscordChannel channel, WindowHandle currentWindow = null);

        public abstract Task CloseAllWindowsAsync();

        public abstract void HandleTitleBarForWindow(FrameworkElement titleBar, FrameworkElement contentRoot);
        public abstract void HandleTitleBarForControl(FrameworkElement element, bool margin = false);
    }
}
