using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Emoji;
using WamWooWam.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Controls
{
    public class DiscordEntityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserTemplate { get; set; }
        public DataTemplate RoleTemplate { get; set; }
        public DataTemplate ChannelTemplate { get; set; }
        public DataTemplate EmojiTemplate { get; set; }
        public DataTemplate EmoteTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            switch (item)
            {
                case DiscordUser user:
                    return UserTemplate;
                case DiscordRole role:
                    return RoleTemplate;
                case DiscordChannel channel:
                    return ChannelTemplate;
                case DiscordEmoji emoji:
                    if (emoji.Id != 0)
                        return EmoteTemplate;
                    else
                        return EmojiTemplate;
                default:
                    return null;
            }
        }
    }

    public sealed class MessageTextBox : Control
    {
        private CoreWindow _coreWindow;
        private AutoSuggestBox _suggestBox;
        private TextBox _textBox;
        private ToggleButton _emoteButton;
        private Button _sendButton;
        private Button _submitButton;
        private Button _cancelButton;
        private Flyout _emoteFlyout;
        private EmotePicker _emotePicker;
        private List<DiscordEmoji> _emoji;
        private bool _shouldFixSelection;
        private int _index = -1;
        private int _length = -1;

        #region Depdendency Properties

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MessageTextBox), new PropertyMetadata(null, OnTextChanged));

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(MessageTextBox), new PropertyMetadata(""));

        public DiscordChannel Channel
        {
            get => (DiscordChannel)GetValue(ChannelProperty);
            set => SetValue(ChannelProperty, value);
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(DiscordChannel), typeof(MessageTextBox), new PropertyMetadata(null));

        public DiscordUser CurrentUser
        {
            get => (DiscordUser)GetValue(CurrentUserProperty);
            set => SetValue(CurrentUserProperty, value);
        }

        public static readonly DependencyProperty CurrentUserProperty =
            DependencyProperty.Register("CurrentUser", typeof(DiscordUser), typeof(MessageTextBox), new PropertyMetadata(null));

        public bool SendButtonEnabled
        {
            get => (bool)GetValue(SendButtonEnabledProperty);
            set => SetValue(SendButtonEnabledProperty, value);
        }

        public static readonly DependencyProperty SendButtonEnabledProperty =
            DependencyProperty.Register("SendButtonEnabled", typeof(bool), typeof(MessageTextBox), new PropertyMetadata(true));

        public Visibility SendButtonVisibility
        {
            get => (Visibility)GetValue(SendButtonVisibilityProperty);
            set => SetValue(SendButtonVisibilityProperty, value);
        }
        public static readonly DependencyProperty SendButtonVisibilityProperty =
            DependencyProperty.Register("SendButtonVisibility", typeof(Visibility), typeof(MessageTextBox), new PropertyMetadata(Visibility.Visible));

        public bool SubmitButtonEnabled
        {
            get => (bool)GetValue(SubmitButtonEnabledProperty);
            set => SetValue(SubmitButtonEnabledProperty, value);
        }

        public static readonly DependencyProperty SubmitButtonEnabledProperty =
            DependencyProperty.Register("SubmitButtonEnabled", typeof(bool), typeof(MessageTextBox), new PropertyMetadata(true));

        public Visibility SubmitButtonVisibility
        {
            get => (Visibility)GetValue(SubmitButtonVisibilityProperty);
            set => SetValue(SubmitButtonVisibilityProperty, value);
        }
        public static readonly DependencyProperty SubmitButtonVisibilityProperty =
            DependencyProperty.Register("SubmitButtonVisibility", typeof(Visibility), typeof(MessageTextBox), new PropertyMetadata(Visibility.Collapsed));

        public bool CancelButtonEnabled
        {
            get => (bool)GetValue(CancelButtonEnabledProperty);
            set => SetValue(CancelButtonEnabledProperty, value);
        }

        public static readonly DependencyProperty CancelButtonEnabledProperty =
            DependencyProperty.Register("CancelButtonEnabled", typeof(bool), typeof(MessageTextBox), new PropertyMetadata(true));

        public Visibility CancelButtonVisibility
        {
            get => (Visibility)GetValue(CancelButtonVisibilityProperty);
            set => SetValue(CancelButtonVisibilityProperty, value);
        }
        public static readonly DependencyProperty CancelButtonVisibilityProperty =
            DependencyProperty.Register("CancelButtonVisibility", typeof(Visibility), typeof(MessageTextBox), new PropertyMetadata(Visibility.Collapsed));

        private bool _hasLoaded = false;

        #endregion

        public event EventHandler<string> SendInvoked;
        public event EventHandler<string> SubmitInvoked;
        public event EventHandler CancelInvoked;
        public event EventHandler EditInvoked;
        public event EventHandler ShouldSendTyping;

        public event TextControlPasteEventHandler Paste;

        public MessageTextBox()
        {
            this.DefaultStyleKey = typeof(MessageTextBox);
            this.Loaded += OnLoaded;
        }

        public void AppendText(string text, bool focus = true)
        {
            _shouldFixSelection = true;
            Text = AppendText(Text, text);

            if (focus)
            {
                Focus(FocusState.Programmatic);
            }
        }

        public void AppendObject(object item, bool focus = true)
        {
            _shouldFixSelection = true;
            Text = AppendObject(Text, item);

            if (focus)
            {
                Focus(FocusState.Programmatic);
            }
        }

        public new void Focus(FocusState state = FocusState.Keyboard)
        {
            if (_suggestBox != null)
            {
                _suggestBox.Focus(state);
                _textBox.SelectionStart = Text.Length;
            }
        }

        public void Clear()
        {
            if (_suggestBox != null)
            {
                _suggestBox.Text = "";
                _suggestBox.ItemsSource = null;
            }

            Text = "";
        }

        protected override void OnApplyTemplate()
        {
            _coreWindow = Window.Current.CoreWindow;
        }

        //
        // the fact i have to do this is fucking sickening microsoft
        //
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_hasLoaded)
                return;

            _hasLoaded = true;

            _emoji = null;

            _suggestBox = this.FindChild<AutoSuggestBox>("PART_TextBox");
            _suggestBox.TextChanged += OnSuggestionBoxTextChanged;
            _suggestBox.SuggestionChosen += OnSuggestBoxSuggestionChosen;
            _suggestBox.QuerySubmitted += OnSuggestBoxQuerySubmitted;
            _suggestBox.ItemsSource = null;
            _suggestBox.Text = Text ?? ""; // ffs

            _textBox = _suggestBox.FindChild<TextBox>();
            _textBox.Paste += OnTextBoxPaste;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.TextChanged += OnTextChanged;

            _sendButton = this.FindChild<Button>("PART_SendButton");
            _sendButton.Click += OnSendButtonClick;

            _submitButton = this.FindChild<Button>("PART_SubmitButton");
            _submitButton.Click += OnSubmitButtonClick;

            _cancelButton = this.FindChild<Button>("PART_CancelButton");
            _cancelButton.Click += OnCancelButtonClick;

            _emoteButton = this.FindChild<ToggleButton>("PART_EmoteButton");
            _emoteButton.Checked += OnShowEmotePicker;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_shouldFixSelection)
            {
                _textBox.SelectionStart = _textBox.Text.Length;
                _shouldFixSelection = false;
            }
        }

        private void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            SendInvoked?.Invoke(this, _suggestBox.Text);
            Focus();
        }

        private void OnSubmitButtonClick(object sender, RoutedEventArgs e)
        {
            SubmitInvoked?.Invoke(this, _suggestBox.Text);
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            CancelInvoked?.Invoke(this, null);
        }

        private void OnTextBoxPaste(object sender, TextControlPasteEventArgs e)
        {
            Paste?.Invoke(sender, e);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d as MessageTextBox)._suggestBox is AutoSuggestBox box)
                box.Text = e.NewValue as string;
        }

        private void OnShowEmotePicker(object sender, RoutedEventArgs e)
        {
            if (_emoteFlyout == null || _emotePicker == null)
            {
                _emoteFlyout = FlyoutBase.GetAttachedFlyout(_emoteButton) as Flyout;
                _emoteFlyout.Closed += OnEmoteFlyoutClosed;

                _emotePicker = _emoteFlyout.Content as EmotePicker;
                _emotePicker.EmojiPicked += OnEmotePicked;
            }

            FlyoutBase.ShowAttachedFlyout(_emoteButton);
        }

        private void OnEmoteFlyoutClosed(object sender, object e)
        {
            _emoteButton.IsChecked = false;
            Focus();
        }

        private void OnEmotePicked(object sender, EmojiViewModel e)
        {
            if (e.IsValid)
            {
                AppendText(e.ToString());
            }
        }

        private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Up && string.IsNullOrWhiteSpace(_suggestBox.Text))
            {
                EditInvoked?.Invoke(this, null);
            }
        }

        private void OnSuggestionBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            Text = _suggestBox.Text; // fuck you

            if (!string.IsNullOrWhiteSpace(_suggestBox.Text))
            {
                ShouldSendTyping?.Invoke(this, null);
            }

            UpdateAutoSuggestBoxSource(sender);
        }

        public void OnSuggestBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            //sender.Text = Text;
        }

        private void OnSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // Text = _suggestBox.Text; // fuck you 2

            if (args.ChosenSuggestion != null)
            {
                if (_index != -1 && _length != -1)
                {
                    Text = Text.Remove(_index, _length);

                    _index = -1;
                    _length = -1;
                }

                AppendObject(args.ChosenSuggestion);
                return;
            }

            var shift = _coreWindow.GetKeyState(VirtualKey.Shift);
            if (shift.HasFlag(CoreVirtualKeyStates.Down))
            {
                var start = _textBox.SelectionStart.Clamp(0, Text.Length);
                _shouldFixSelection = true;
                Text = Text.Insert(start, "\r\n");
            }
            else
            {
                Text = args.QueryText;
                SendInvoked?.Invoke(this, args.QueryText);
            }
        }

        private void UpdateAutoSuggestBoxSource(AutoSuggestBox sender)
        {
            // don't ya just love off by one?
            var position = _textBox.SelectionStart.Clamp(0, Text.Length - 1);
            var i = position;
            for (; i >= 0; i--)
            {
                if (char.IsWhiteSpace(Text[i]))
                {
                    break; // return the last "word"
                }
            }

            // i understand this code is probably overly verbose but i want it to like, actualy work
            // and be somewhat understandable if it ever turns out to not work

            _index = i + 1;
            _length = position - i;
            if (_length == 0)
            {
                sender.ItemsSource = null;
                return;
            }

            var text = Text.Substring(_index, _length)
                           .Trim(':', '@', '#')
                           .ToLowerInvariant();

            var cult = CultureInfo.InvariantCulture.CompareInfo;

            // LINQ Fuckery: The Movie: The Video Game: The Novelisation
            // TODO: Commands?

            if (Text[_index] == '@')
            {
                UpdateSourceForUserRoleMentions(sender, text, cult);
                return;
            }

            if (Text[_index] == '#' && Channel.Guild != null)
            {
                UpdateSourceForChannelMentions(sender, text, cult);
                return;
            }

            if (Text[_index] == ':' && _length > 2)
            {
                UpdateSourceForEmojiMentions(sender, text, cult);
                return;
            }

            sender.ItemsSource = null;
            _index = -1;
            _length = -1;
        }

        private void UpdateSourceForUserRoleMentions(AutoSuggestBox sender, string text, CompareInfo cult)
        {
            var users = (Channel is DiscordDmChannel dm) ? dm.Recipients : Channel.Users.Cast<DiscordUser>();
            var roles = Channel.Guild?.Roles.Values.Where(r => r.IsMentionable) ?? Enumerable.Empty<DiscordRole>();

            var filteredUsers = users.Where(u => u != null && !string.IsNullOrWhiteSpace(u.Username) && !string.IsNullOrWhiteSpace(u.Discriminator))
                                     .Select(u => (user: u, index: cult.IndexOf($"{u.Username}#{u.Discriminator}", text, CompareOptions.IgnoreCase)))
                                     .Where(x => x.index != -1)
                                     .OrderBy(x => x.user.Username)
                                     .ThenByDescending(x => x.index)
                                     .Select(x => x.user)
                                     .Cast<object>();

            var filteredRoles = roles.Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                                     .Where(r => cult.IndexOf(r.Name, text, CompareOptions.IgnoreCase) != -1)
                                     .Cast<object>();

            sender.ItemsSource = filteredUsers.Concat(filteredRoles);
        }

        private void UpdateSourceForChannelMentions(AutoSuggestBox sender, string text, CompareInfo cult)
        {
            var channels = Channel.Guild.Channels.Values;
            sender.ItemsSource = channels.Where(c => c != null && c.Type != ChannelType.Voice && c.Type != ChannelType.Category && !string.IsNullOrWhiteSpace(c.Name))
                                         .Where(c => c.CurrentPermissions.HasPermission(Permissions.AccessChannels))
                                         .Select(c => (channel: c, index: cult.IndexOf(c.Name, text, CompareOptions.IgnoreCase)))
                                         .Where(x => x.index != -1)
                                         .OrderByDescending(x => x.index)
                                         .Select(x => x.channel);
        }

        private void UpdateSourceForEmojiMentions(AutoSuggestBox sender, string text, CompareInfo cult)
        {
            //if (_emoji == null)
            //{
            //    _emoji = Tools.GetEmoji(Channel);
            //}

            //sender.ItemsSource = _emoji.Select(e => (emoji: e, index: cult.IndexOf(e.SearchName, text, CompareOptions.IgnoreCase)))
            //                           .Where(x => x.index != -1)
            //                           .OrderBy(x => x.index)
            //                           .Select(x => x.emoji);
        }

        private string AppendText(string target, string text)
        {
            if (target.Length > 0 && !char.IsWhiteSpace(target[target.Length - 1]))
            {
                target += " ";
            }

            target += $"{text} ";

            return target;
        }

        private string AppendObject(string target, object item)
        {
            switch (item)
            {
                case DiscordUser user:
                    //return AppendText(target, $"@{user.Username}#{user.Discriminator}");
                    return AppendText(target, user.Mention);
                case DiscordChannel channel:
                    //return AppendText(target, $"#{channel.Name}");
                    return AppendText(target, channel.Mention);
                case DiscordRole role:
                    //return AppendText(target, $"@{role.Name}");
                    return AppendText(target, role.Mention);
                case DiscordEmoji emoji:
                    //return AppendText(target, emoji.RequiresColons ? emoji.Name : $":{emoji.Name}:");
                    return AppendText(target, emoji.ToString());
                default:
                    return AppendText(target, item.ToString());
            }
        }
    }
}
