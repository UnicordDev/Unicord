using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.HockeyApp;
using NeoSmart.Unicode;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unicord.Universal.Pages;
using WamWooWam.Uwp.UI.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls
{
    public sealed partial class MessageViewer : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(DiscordMessage), typeof(MessageViewer), new PropertyMetadata(null, OnPropertyChanged));

        public DiscordMessage Message { get => (DiscordMessage)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

        public bool AutoSize { get; set; } = true;

        private ObservableCollection<DiscordReaction> _reactions = new ObservableCollection<DiscordReaction>();
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

        private bool _canDelete =>
           _canEdit || _manageMessages;

        private bool _canEdit =>
            _author?.Id == App.Discord.CurrentUser.Id;

        private bool _changeNickname =>
            _author is DiscordMember ? _permissions.HasPermission(Permissions.ManageNicknames) || (_author.Id == App.Discord.CurrentUser.Id && _permissions.HasPermission(Permissions.ChangeNickname)) : false;

        private bool _pinMessages =>
            _manageMessages || _channel is DiscordDmChannel;

        private bool _manageMessages =>
            _currentMember != null ? _currentMember.IsOwner || _permissions.HasPermission(Permissions.ManageMessages) : false;

        private bool _kickMembers =>
            _currentMember != null ? _currentMember.IsOwner || CheckPermission(Permissions.KickMembers) : false;

        private bool _banMembers =>
            _currentMember != null ? _currentMember.IsOwner || CheckPermission(Permissions.BanMembers) : false;

        private bool _middleSeparatorVisible => _changeNickname || _manageMessages || _pinMessages || _kickMembers || _banMembers;

        private bool _bottomSeparatorVisible => _canDelete || _canEdit;

        public ulong Id => Message.Id;

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
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var t = d as MessageViewer;
            t.UpdateViewer(e.Property, e.OldValue as DiscordMessage, e.NewValue as DiscordMessage);
        }

        public void UpdateViewer(DependencyProperty property, DiscordMessage oldMessage, DiscordMessage newMessage)
        {
            try
            {
                if (property == MessageProperty)
                {
                    void PropertyChanged(object ob, PropertyChangedEventArgs ev)
                    {
                        if (ev.PropertyName == "Content")
                        {
                            var newM = ob as DiscordMessage;
                            markdown.Text = newM.Content;
                        }
                    }

                    if (oldMessage != null)
                    {
                        oldMessage.PropertyChanged -= PropertyChanged;
                    }

                    _reactions.Clear();

                    if (newMessage == null)
                    {
                        embeds?.Children.Clear();
                        bg.Fill = Application.Current.Resources["TransparentBrush"] as Brush;
                        markdown.FontSize = 14;
                        markdown.Text = "";
                        DataContext = null;
                    }
                    else
                    {
                        embeds?.Children.Clear();

                        Logger.Log($"{newMessage.MessageType} {(int)newMessage.MessageType}");

                        newMessage.PropertyChanged += PropertyChanged;

                        if (newMessage.MentionedUsers.Any(me => me != null && me.Id == me.Discord.CurrentUser.Id))
                        {
                            bg.Fill = newMessage.Channel.Guild?.CurrentMember?.ColorBrush != null ?
                                new SolidColorBrush(newMessage.Channel.Guild.CurrentMember.ColorBrush.Color) { Opacity = 0.1 } :
                                Application.Current.Resources["MentionBrush"] as Brush;
                        }
                        else
                        {
                            bg.Fill = Application.Current.Resources["TransparentBrush"] as Brush;
                        }

                        markdown.FontSize = Emoji.IsEmoji(newMessage.Content) ? 26 : 14;
                        markdown.Text = newMessage?.Content;
                        DataContext = newMessage;

                        _channel = newMessage.Channel;
                        _author = newMessage.Author;
                        _member = _author as DiscordMember;

                        foreach (var r in newMessage.Reactions)
                        {
                            _reactions.Add(r);
                        }

                        if (newMessage.Channel.Guild != null)
                        {
                            _currentMember = newMessage.Channel.Guild.CurrentMember;
                            _permissions = newMessage.Channel.PermissionsFor(_currentMember);
                        }

                        HandleAttachments(newMessage);
                    }
                }
            }
            catch
            {

            }
            finally
            {
                Bindings.Update();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeSize();
                Bindings.Update();
            }
            catch (Exception ex)
            {
                HockeyClient.Current.TrackException(ex);
            }
        }

        private void HandleAttachments(DiscordMessage m)
        {
            if (m.Attachments.Any() || m.Embeds.Any())
            {
                foreach (var attach in m.Attachments)
                {
                    if (!embeds.Children.OfType<UserControl>().Any(e => e.Tag == attach))
                        embeds.Children.Add(new AttachmentViewer(attach) { Tag = attach });
                }

                foreach (var embed in m.Embeds)
                {
                    if (!embeds.Children.OfType<UserControl>().Any(e => e.Tag == embed))
                    {
                        if (embed.Type == "image")
                        {
                            ImageElement element = null;

                            if (embed.Thumbnail != null)
                            {
                                element = new ImageElement() { Tag = embed, ImageUri = embed.Thumbnail.ProxyUrl, ImageWidth = embed.Thumbnail.Width, ImageHeight = embed.Thumbnail.Height, HorizontalAlignment = HorizontalAlignment.Left };
                            }
                            else if (embed.Image != null)
                            {
                                element = new ImageElement() { Tag = embed, ImageUri = embed.Image.ProxyUrl, ImageWidth = embed.Image.Width, ImageHeight = embed.Image.Height, HorizontalAlignment = HorizontalAlignment.Left };
                            }

                            if (element != null)
                            {
                                element.Margin = new Thickness(0, 10, 0, 0);
                                embeds.Children.Add(element);
                            }
                        }
                        else
                        {
                            embeds.Children.Add(new EmbedViewer(m, embed) { Tag = embed });
                        }
                    }
                }
            }
        }

        private void ChangeSize()
        {
            var page = this.FindParent<ChannelPage>();
            if (page != null)
            {
                var stack = page.FindChild<StackPanel>();
                if (stack != null)
                {
                    var index = stack.Children.IndexOf(this);

                    if (index > 0)
                    {
                        if (stack.Children.ElementAt(index - 1) is MessageViewer other)
                        {
                            if (other.Message.Author.Id == Message.Author.Id && (other.Message.Timestamp - Message.Timestamp) < TimeSpan.FromHours(1))
                            {
                                bg.Margin = new Thickness(0, 1.5, 0, -1.5);
                                grid.Padding = new Thickness(10, 3, 10, 0);
                                CollapsedVisibility = Visibility.Collapsed;
                                return;
                            }
                        }
                    }

                    bg.Margin = new Thickness(0, 15, 0, -2);
                    grid.Padding = new Thickness(10, 20, 10, 0);
                }
            }

            CollapsedVisibility = Visibility.Visible;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }

        internal void Unload()
        {
            AutoSize = true;
        }

        private async void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
        }

        private bool CheckPermission(Permissions permission)
        {
            return _currentMember?.IsOwner == true || _permissions.HasPermission(permission) && Tools.CheckRoleHeirarchy(_member, _currentMember);
        }

        private void profileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<MainPage>()
                .ShowUserOverlay(_author, true);
        }

        private void editMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing)
            {
                _isEditing = true;
                markdown.Visibility = Visibility.Collapsed;
                messageEditContainer.Visibility = Visibility.Visible;
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
            if (e.Key == VirtualKey.Enter && shift.HasFlag(CoreVirtualKeyStates.None))
            {
                await FinishEditAndSend();
            }
        }

        private void FinishEdit()
        {
            if (_isEditing)
            {
                _isEditing = false;
                markdown.Visibility = Visibility.Visible;
                messageEditContainer.Visibility = Visibility.Collapsed;
            }
        }

        private async Task FinishEditAndSend()
        {
            if (_isEditing)
            {
                if (!string.IsNullOrWhiteSpace(messageEditBox.Text))
                {
                    markdown.Text = messageEditBox.Text;
                    await Message.ModifyAsync(messageEditBox.Text);
                }

                FinishEdit();
            }
        }
    }
}
