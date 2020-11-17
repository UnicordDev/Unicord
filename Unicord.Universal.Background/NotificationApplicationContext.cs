using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Background.Properties;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background
{
    class NotificationApplicationContext : ApplicationContext
    {
        private DiscordClient _discord = null;
        private BadgeManager _badgeManager = null;
        private ToastManager _toastManager = null;

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _closeMenuItem;

        private Task _connectTask;
        private string _token = null;

        public NotificationApplicationContext()
        {
            Application.ApplicationExit += OnApplicationExit;

            if (!TryGetToken(out _token))
            {
                ExitThread();
            }

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Resources.TrayIcon;
            _notifyIcon.Text = "Unicord";

            _contextMenu = new ContextMenuStrip();
            _contextMenu.SuspendLayout();

            _closeMenuItem = new ToolStripMenuItem("Close");
            _closeMenuItem.Click += OnCloseMenuItemClicked;
            _contextMenu.Items.Add(_closeMenuItem);

            _contextMenu.ResumeLayout(false);
            _notifyIcon.ContextMenuStrip = _contextMenu;

            _connectTask = Task.Run(async () => await InitialiseAsync());
            _notifyIcon.Visible = true;
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
                    LogLevel = LogLevel.Debug,
                    TokenType = TokenType.User,
                    Token = _token,
                    MessageCacheSize = 0,
                    UseInternalLogHandler = true
                });

                _badgeManager = new BadgeManager(_discord);
                _toastManager = new ToastManager();

                _discord.MessageCreated += OnDiscordMessage;
                _discord.MessageAcknowledged += OnMessageAcknowledged;

                await _discord.ConnectAsync(status: UserStatus.Invisible, idlesince: DateTimeOffset.Now);
            }
            catch (Exception)
            {
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

        private Task OnDiscordMessage(MessageCreateEventArgs e)
        {
            if (Tools.WillShowToast(e.Message))
            {
                _toastManager?.HandleMessage(e.Message);
                _badgeManager?.Update();
            }

            return Task.CompletedTask;
        }

        private Task OnMessageAcknowledged(MessageAcknowledgeEventArgs e)
        {
            try
            {
                _badgeManager?.Update();
                _toastManager?.HandleAcknowledge(e.Channel);
            }
            catch (Exception)
            {
                // TODO: log
            }

            return Task.CompletedTask;
        }

        private bool TryGetToken(out string token)
        {
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
