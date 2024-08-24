using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Shared;
using Windows.Storage;
using NotificationUtils = Unicord.Universal.Shared.NotificationUtils;

namespace Unicord.Universal.Background
{
    class NotificationApplicationContext : ApplicationContext
    {
        private DiscordClient _discord = null;
        private BadgeManager _badgeManager = null;
        private TileManager _tileManager;
        private SecondaryTileManager _secondaryTileManager;
        private ToastManager _toastManager = null;

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _openMenuItem;
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
            _notifyIcon.Icon = Properties.Resources.TrayIcon;
            _notifyIcon.Text = "Unicord";

            _contextMenu = new ContextMenuStrip();
            _contextMenu.SuspendLayout();

            _openMenuItem = new ToolStripMenuItem("Open Unicord");
            _openMenuItem.Click += OnOpenMenuItemClicked;
            _contextMenu.Items.Add(_openMenuItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            _closeMenuItem = new ToolStripMenuItem("Close");
            _closeMenuItem.Click += OnCloseMenuItemClicked;
            _contextMenu.Items.Add(_closeMenuItem);

            _contextMenu.ResumeLayout(false);
            _notifyIcon.ContextMenuStrip = _contextMenu;

            _connectTask = Task.Run(async () => await InitialiseAsync());
            _notifyIcon.Visible = true;
        }

        private void OnOpenMenuItemClicked(object sender, EventArgs e)
        {
            Process.Start("unicord:");
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
                _tileManager = new TileManager(_discord);
                _secondaryTileManager = new SecondaryTileManager(_discord);
                _toastManager = new ToastManager();

                _discord.Ready += OnReady;
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

        private async Task OnReady(ReadyEventArgs e)
        {
            await _tileManager.InitialiseAsync();
        }

        private Task OnDiscordMessage(MessageCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000);

                    if (NotificationUtils.WillShowToast(e.Message))
                    {
                        _toastManager?.HandleMessage(e.Message);
                        _badgeManager?.Update();

                        if (_tileManager != null)
                            await _tileManager.HandleMessageAsync(e.Message);
                    }

                    if (_secondaryTileManager != null)
                        await _secondaryTileManager.HandleMessageAsync(e.Message);
                }
                catch (Exception)
                {
                    // TODO: log
                }
            });

            return Task.CompletedTask;
        }

        private async Task OnMessageAcknowledged(MessageAcknowledgeEventArgs e)
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
            catch (Exception)
            {
                // TODO: log
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
