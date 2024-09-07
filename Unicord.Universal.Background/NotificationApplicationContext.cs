using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Shared;
using Windows.ApplicationModel.AppService;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;

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
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _openMenuItem;
        private readonly ToolStripMenuItem _closeMenuItem;
        private readonly AppServiceConnection _connection;

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
                    TokenType = TokenType.User,
                    Token = _token,
                    MessageCacheSize = 0
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

        private async Task OnReady(DiscordClient client, ReadyEventArgs e)
        {
            await _tileManager.InitialiseAsync();
            _badgeManager.Update();

            var timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += OnGCTimer;
            timer.Start();

        }
        private Task OnResumed(DiscordClient sender, ResumedEventArgs args)
        {
            var timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += OnGCTimer;
            timer.Start();

            return Task.CompletedTask;
        }

        private void OnGCTimer(object sender, EventArgs e)
        {
            (sender as Timer).Stop();
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
            catch (Exception)
            {
                // TODO: log
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
            catch (Exception)
            {
                // TODO: log
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
