using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NeoSmart.Unicode;
using Unicord.Universal.Commands;
using Unicord.Universal.Controls.Flyouts;
using Unicord.Universal.Pages;
using Unicord.Universal.Utilities;
using WamWooWam.Uwp.UI.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Controls
{
    public sealed partial class MessageViewer : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(DiscordMessage), typeof(MessageViewer), new PropertyMetadata(null, OnPropertyChanged));

        public DiscordMessage Message { get => (DiscordMessage)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

        private static ThreadLocal<DispatcherTimer> _timestampTimer;

        private ObservableCollection<DiscordReaction> _reactions = new ObservableCollection<DiscordReaction>();
        private DiscordMessage _message;
        private DiscordChannel _channel;
        private DiscordUser _author;
        private DiscordMember _member;
        private DiscordMember _currentMember;
        private Permissions _permissions;
        private bool _isEditing;
        private Visibility _collapsedVisibility = Visibility.Visible;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Flyout Utilities

        private bool _canReact => _currentMember == null || _permissions.HasPermission(Permissions.AddReactions);

        private bool _changeNickname =>
            _author is DiscordMember ? _permissions.HasPermission(Permissions.ManageNicknames) || (_author.Id == App.Discord.CurrentUser.Id && _permissions.HasPermission(Permissions.ChangeNickname)) : false;

        private bool _manageMessages =>
            _currentMember != null ? _currentMember.IsOwner || _permissions.HasPermission(Permissions.ManageMessages) : false;

        private bool _kickMembers =>
            _currentMember != null ? _currentMember.IsOwner || CheckPermission(Permissions.KickMembers) : false;

        private bool _banMembers =>
            _currentMember != null ? _currentMember.IsOwner || CheckPermission(Permissions.BanMembers) : false;

        public ulong Id => Message.Id;

        private bool CanEdit => Message?.Author.Id == App.Discord?.CurrentUser.Id;

        public bool ShowBottomSeparator =>
            CanEdit || (DeleteMessageCommand.Instance?.CanExecute(Message) ?? false);

        public Visibility CollapsedVisibility
        {
            get => _collapsedVisibility;
            set
            {
                _collapsedVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CollapsedVisibility)));
            }
        }

        #endregion

        public MessageViewer()
        {
            InitializeComponent();

            if (_timestampTimer == null)
            {
                _timestampTimer = new ThreadLocal<DispatcherTimer>(() =>
                    new DispatcherTimer() { Interval = TimeSpan.FromSeconds(30) });
                _timestampTimer.Value.Start();
            }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageViewer t)
            {
                var oldMessage = e.OldValue as DiscordMessage;
                var newMessage = e.NewValue as DiscordMessage;

                if (oldMessage != newMessage)
                {
                    t.UpdateViewer(e.Property, oldMessage, newMessage);
                }
            }
        }

        public void UpdateViewer(DependencyProperty property, DiscordMessage oldMessage, DiscordMessage newMessage)
        {
            try
            {
                if (property == MessageProperty)
                {
                    if (newMessage == null)
                    {
                        _message = null;
                        _member = null;
                        _author = null;
                        _channel = null;

                        embeds?.Children.Clear();
                        markdown.Text = "";
                        grid.DataContext = null;
                    }
                    else
                    {
                        _message = newMessage;
                        embeds?.Children.Clear();

                        markdown.FontSize = Emoji.IsEmoji(newMessage.Content) ? 26 : 14;
                        grid.DataContext = newMessage;

                        _channel = newMessage.Channel;
                        _author = newMessage.Author;
                        _member = _author as DiscordMember;

                        if (newMessage.Channel.Guild != null)
                        {
                            _currentMember = newMessage.Channel.Guild.CurrentMember;
                            _permissions = newMessage.Channel.PermissionsFor(_currentMember);
                        }

                        ChangeSize();
                        HandleAttachments(newMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
            finally
            {
                Bindings.Update();
            }
        }

        private void HandleAttachments(DiscordMessage m)
        {
            foreach (var attach in m.Attachments)
            {
                if (!embeds.Children.OfType<UserControl>().Any(e => e.Tag == attach))
                {
                    embeds.Children.Add(new AttachmentViewer(attach) { Tag = attach });
                }
            }

            foreach (var embed in m.Embeds)
            {
                if (!embeds.Children.OfType<UserControl>().Any(e => e.Tag == embed))
                {
                    embeds.Children.Add(new EmbedViewer(m, embed) { Tag = embed });
                }
            }
        }

        public void ChangeSize()
        {
            var list = this.FindParent<ListView>();
            if (list != null)
            {
                if (list.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    CollapsedVisibility = Visibility.Visible;
                    embeds.Visibility = Visibility.Collapsed;
                    grid.Padding = new Thickness(8);
                    return;
                }

                var index = list.Items.IndexOf(Message);

                if (index > 0)
                {
                    if (list.Items[index - 1] is DiscordMessage other && other.MessageType == MessageType.Default)
                    {
                        var timeSpan = (Message.CreationTimestamp - other.CreationTimestamp);
                        if (other.Author.Id == Message.Author.Id && timeSpan <= TimeSpan.FromMinutes(10))
                        {
                            CollapsedVisibility = Visibility.Collapsed;
                            grid.Padding = new Thickness(8, 4, 8, 0);
                            return;
                        }
                    }
                }
                
                grid.Padding = new Thickness(8, 20, 8, 0);
            }

            CollapsedVisibility = Visibility.Visible;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Discord.MessageUpdated += Discord_MessageUpdated;
            _timestampTimer.Value.Tick += _timestampTimer_Tick;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _timestampTimer.Value.Tick -= _timestampTimer_Tick;
            if (App.Discord != null)
            {
                App.Discord.MessageUpdated -= Discord_MessageUpdated;
            }
        }

        private void _timestampTimer_Tick(object sender, object e)
        {
            if (_collapsedVisibility == Visibility.Visible)
                _message?.InvokePropertyChanged(nameof(_message.Timestamp));
        }

        private Task Discord_MessageUpdated(MessageUpdateEventArgs e)
        {
            if (_message.Id == e.Message.Id)
            {
                return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    embeds?.Children.Clear();
                    HandleAttachments(e.Message);
                }).AsTask();
            }

            return Task.CompletedTask;
        }

        private async void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
        }

        private bool CheckPermission(Permissions permission)
        {
            return _currentMember?.IsOwner == true || _permissions.HasPermission(permission) && Tools.CheckRoleHeirarchy(_currentMember, _member);
        }

        private void profileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<MainPage>()
                .ShowUserOverlay(_author, true);
        }

        public void BeginEditing()
        {
            if (!_isEditing)
            {
                _isEditing = true;
                markdown.Visibility = Visibility.Collapsed;

                if (FindName("messageEditContainer") is Grid grid)
                {
                    grid.Visibility = Visibility.Visible;
                    messageEditBox.Focus(FocusState.Keyboard);
                    messageEditBox.SelectionStart = messageEditBox.Text.Length;
                }
            }
        }

        private async void messageEditFinishButton_Click(object sender, RoutedEventArgs e)
        {
            await FinishEditAndSend();
        }

        private void messageEditCancelButton_Click(object sender, RoutedEventArgs e)
        {
            FinishEdit();
        }

        private async void messageEditBox_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                if (!shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    await FinishEditAndSend();
                }
                else
                {
                    var start = messageEditBox.SelectionStart;
                    messageEditBox.Text = messageEditBox.Text.Insert(start, "\r\n");
                    messageEditBox.SelectionStart = start + 1;
                }
            }
        }

        private void FinishEdit()
        {
            if (_isEditing)
            {
                _isEditing = false;
                markdown.Visibility = Visibility.Visible;

                if (FindName("messageEditContainer") is Grid grid)
                {
                    grid.Visibility = Visibility.Collapsed;
                    this.FindParent<ChannelPage>()?.FocusTextBox();
                }
            }
        }

        private async Task FinishEditAndSend()
        {
            if (_isEditing)
            {
                FinishEdit();
                if (!string.IsNullOrWhiteSpace(messageEditBox.Text) && messageEditBox.Text != markdown.Text)
                {
                    markdown.Text = messageEditBox.Text;
                    await Message.ModifyAsync(messageEditBox.Text);
                }
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            BeginEditing();
        }

        private void EditModeItem_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<ChannelPage>().EnterEditMode(Message);
        }

        private void CopyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(message.Text);
            Clipboard.SetContent(package);
        }

        private void AuthorName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AdaptiveFlyoutUtilities.ShowAdaptiveFlyout<UserFlyout>(_author, sender as FrameworkElement);
        }
    }
}