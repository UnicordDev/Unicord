using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Shared;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Win32.Foundation;
using static Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE;
using static Windows.Win32.PInvoke;

namespace Unicord.Universal.Background
{
    class NotificationApplicationContext : ApplicationContext
    {
        private DiscordClient _discord = null;
        private BadgeManager _badgeManager = null;
        private TileManager _tileManager;
        private SecondaryTileManager _secondaryTileManager;
        private ToastManager _toastManager = null;

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenu _contextMenu;
        private readonly MenuItem _openMenuItem;
        private readonly MenuItem _closeMenuItem;

        private Task _connectTask;
        private string _token = null;

        private static readonly FieldInfo _windowField
            = typeof(NotifyIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);

        public NotificationApplicationContext()
        {
            Application.ApplicationExit += OnApplicationExit;

            if (!TryGetToken(out _token))
            {
                ExitThread();
            }

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Properties.Resources.TrayIcon;
            _notifyIcon.Text = "Unicord";
            _notifyIcon.DoubleClick += OnOpenMenuItemClicked;

            _contextMenu = new ContextMenu();

            _openMenuItem = new MenuItem("Open Unicord");
            _openMenuItem.Click += OnOpenMenuItemClicked;
            _contextMenu.MenuItems.Add(_openMenuItem);

            _contextMenu.MenuItems.Add("-");

            _closeMenuItem = new MenuItem("Close");
            _closeMenuItem.Click += OnCloseMenuItemClicked;
            _contextMenu.MenuItems.Add(_closeMenuItem);

            //_contextMenu.ResumeLayout(false);
            _notifyIcon.ContextMenu = _contextMenu;
            _notifyIcon.Visible = true;

            EnableDarkMode(_notifyIcon);

            _connectTask = Task.Run(async () => await InitialiseAsync());
        }

        // here be dragons and awful hacks
        private void EnableDarkMode(NotifyIcon notifyIcon)
        {
            var osVersion = Environment.OSVersion.Version;

            // Windows 10 1809 and later
            if (osVersion.Major < 10 || osVersion.Build < 17763)
                return;

            try
            {
                var hwnd = new HWND(((NativeWindow)_windowField.GetValue(notifyIcon)).Handle);
                if (osVersion.Build < 18362)
                {
                    UxThemePrivate.AllowDarkModeForWindow(hwnd, true);
                }
                else
                {
                    UxThemePrivate.SetPreferredAppMode(UxThemePrivate.PreferredAppMode.AllowDark);
                }

                UxThemePrivate.FlushMenuThemes();
            }
            catch
            {
                // ignore this, it doesn't matter
            }
        }

        private async void OnOpenMenuItemClicked(object sender, EventArgs e)
        {
            var appListEntries = await Package.Current.GetAppListEntriesAsync();
            var app = appListEntries.FirstOrDefault();
            if (app != null)
                await app.LaunchAsync();
        }

        private void OnCloseMenuItemClicked(object sender, EventArgs e)
        {
            this.ExitThread();
        }

        private async Task InitialiseAsync()
        {
            try
            {
                _discord = new DiscordClient(new DiscordConfiguration()
                {
                    TokenType = TokenType.User,
                    Token = _token,
                    MessageCacheSize = 0,
                    ReconnectIndefinitely = true
                });

                _badgeManager = new BadgeManager(_discord);
                _tileManager = new TileManager(_discord);
                _secondaryTileManager = new SecondaryTileManager(_discord);
                _toastManager = new ToastManager();

                _discord.Ready += OnReady;
                _discord.Resumed += OnResumed;
                _discord.MessageCreated += OnDiscordMessage;
                _discord.MessageUpdated += OnMessageUpdated;
                _discord.MessageAcknowledged += OnMessageAcknowledged;

                await _discord.ConnectAsync(status: UserStatus.Invisible, idlesince: DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                this.ExitThread();
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (_discord != null)
                _discord.DisconnectAsync().GetAwaiter().GetResult();

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }

        private async Task OnReady(DiscordClient client, ReadyEventArgs e)
        {
            await _tileManager.InitialiseAsync();
            _badgeManager.Update();

            _ = Task.Run(GCTask);
        }

        private Task OnResumed(DiscordClient sender, ResumedEventArgs args)
        {
            _ = Task.Run(GCTask);
            return Task.CompletedTask;
        }

        private async Task GCTask()
        {
            await Task.Delay(5000);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private async Task OnDiscordMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(client, e.Message))
                {
                    _toastManager?.HandleMessage(client, e.Message, UnicordFinder.IsUnicordVisible());
                    _badgeManager?.Update();

                    if (_tileManager != null)
                        await _tileManager.HandleMessageAsync(e.Message);
                }

                if (_secondaryTileManager != null)
                    await _secondaryTileManager.HandleMessageAsync(client, e.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            try
            {
                if (NotificationUtils.WillShowToast(client, e.Message))
                {
                    _toastManager?.HandleMessageUpdated(client, e.Message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageAcknowledged(DiscordClient client, MessageAcknowledgeEventArgs e)
        {
            try
            {
                _badgeManager?.Update();
                _toastManager?.HandleAcknowledge(e.Channel);

                if (_tileManager != null)
                    await _tileManager.HandleAcknowledgeAsync(e.Channel);

                if (_secondaryTileManager != null)
                    await _secondaryTileManager.HandleAcknowledgeAsync(e.Channel);
            }
            catch (Exception ex)
            {
                // TODO: log
                Debug.WriteLine(ex);
            }
        }

        private bool TryGetToken(out string token)
        {
#if DEBUG // for testing
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2)
            {
                token = args[1];
                return true;
            }
#endif

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("Token", out var s))
            {
                token = (string)s;
                return true;
            }

            token = null;
            return false;
        }
    }
}
