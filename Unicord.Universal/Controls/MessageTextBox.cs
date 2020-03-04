using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls
{
    public sealed class MessageTextBox : Control
    {
        private CoreWindow _coreWindow;
        private TextBox _textBox;
        private ToggleButton _emoteButton;
        private Button _sendButton;
        private Button _submitButton;
        private Button _cancelButton;
        private Flyout _emoteFlyout;
        private EmotePicker _emotePicker;
        private Popup _suggestionPopup;
        private Grid _suggestionChild;

        #region Depdendency Properties

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MessageTextBox), new PropertyMetadata(null));

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(MessageTextBox), new PropertyMetadata(""));

        public DiscordChannel Channel
        {
            get { return (DiscordChannel)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(DiscordChannel), typeof(MessageTextBox), new PropertyMetadata(null));

        public DiscordUser CurrentUser
        {
            get { return (DiscordUser)GetValue(CurrentUserProperty); }
            set { SetValue(CurrentUserProperty, value); }
        }

        public static readonly DependencyProperty CurrentUserProperty =
            DependencyProperty.Register("CurrentUser", typeof(DiscordUser), typeof(MessageTextBox), new PropertyMetadata(null));

        public bool SendButtonEnabled
        {
            get { return (bool)GetValue(SendButtonEnabledProperty); }
            set { SetValue(SendButtonEnabledProperty, value); }
        }

        public static readonly DependencyProperty SendButtonEnabledProperty =
            DependencyProperty.Register("SendButtonEnabled", typeof(bool), typeof(MessageTextBox), new PropertyMetadata(true));

        public Visibility SendButtonVisibility
        {
            get { return (Visibility)GetValue(SendButtonVisibilityProperty); }
            set { SetValue(SendButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty SendButtonVisibilityProperty =
            DependencyProperty.Register("SendButtonVisibility", typeof(Visibility), typeof(MessageTextBox), new PropertyMetadata(Visibility.Visible));

        public bool SubmitButtonEnabled
        {
            get { return (bool)GetValue(SubmitButtonEnabledProperty); }
            set { SetValue(SubmitButtonEnabledProperty, value); }
        }

        public static readonly DependencyProperty SubmitButtonEnabledProperty =
            DependencyProperty.Register("SubmitButtonEnabled", typeof(bool), typeof(MessageTextBox), new PropertyMetadata(true));

        public Visibility SubmitButtonVisibility
        {
            get { return (Visibility)GetValue(SubmitButtonVisibilityProperty); }
            set { SetValue(SubmitButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty SubmitButtonVisibilityProperty =
            DependencyProperty.Register("SubmitButtonVisibility", typeof(Visibility), typeof(MessageTextBox), new PropertyMetadata(Visibility.Collapsed));

        public bool CancelButtonEnabled
        {
            get { return (bool)GetValue(CancelButtonEnabledProperty); }
            set { SetValue(CancelButtonEnabledProperty, value); }
        }

        public static readonly DependencyProperty CancelButtonEnabledProperty =
            DependencyProperty.Register("CancelButtonEnabled", typeof(bool), typeof(MessageTextBox), new PropertyMetadata(true));

        public Visibility CancelButtonVisibility
        {
            get { return (Visibility)GetValue(CancelButtonVisibilityProperty); }
            set { SetValue(CancelButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty CancelButtonVisibilityProperty =
            DependencyProperty.Register("CancelButtonVisibility", typeof(Visibility), typeof(MessageTextBox), new PropertyMetadata(Visibility.Collapsed));

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

        //
        // the fact i have to do this is fucking sickening microsoft
        //
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _textBox = this.FindChild<TextBox>("PART_TextBox");
            _textBox.SizeChanged += OnTextBoxSizeChanged;
            _textBox.TextChanged += OnTextBoxTextChanged;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.Paste += OnTextBoxPaste;

            _sendButton = this.FindChild<Button>("PART_SendButton");
            _sendButton.Click += OnSendButtonClick;

            _submitButton = this.FindChild<Button>("PART_SubmitButton");
            _submitButton.Click += OnSubmitButtonClick;

            _cancelButton = this.FindChild<Button>("PART_CancelButton");
            _cancelButton.Click += OnCancelButtonClick;

            _emoteButton = this.FindChild<ToggleButton>("PART_EmoteButton");
            _emoteButton.Checked += OnShowEmotePicker;
            // _emoteButton.Unchecked += OnHideEmotePicker;

            _suggestionPopup = this.FindChild<Popup>("PART_SuggestionPopup");

            _suggestionChild = _suggestionPopup.Child as Grid;
            _suggestionChild.Width = _textBox.ActualWidth;
            _suggestionChild.SizeChanged += OnPopupSizeChanged;
        }

        private void OnTextBoxSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _suggestionChild.Width = e.NewSize.Width;
        }

        private void OnPopupSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _suggestionPopup.VerticalOffset = -_suggestionChild.ActualHeight;
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

        private void OnEmotePicked(object sender, DSharpPlus.Entities.DiscordEmoji e)
        {
            if (e != null)
            {
                if (Text.Length > 0 && !char.IsWhiteSpace(Text[Text.Length - 1]))
                {
                    Text += " ";
                }

                Text += $"{e} ";
                Focus(FocusState.Programmatic);
            }
        }

        public new void Focus(FocusState state = FocusState.Keyboard)
        {
            if (_textBox != null)
            {
                _textBox.Focus(state);
                _textBox.SelectionStart = _textBox.Text.Length;
            }
        }

        public void Clear()
        {
            if (_textBox != null)
                _textBox.Text = "";
        }

        protected override void OnApplyTemplate()
        {
            _coreWindow = Window.Current.CoreWindow;
        }

        private void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            SendInvoked?.Invoke(this, _textBox.Text);
            Focus();
        }

        private void OnSubmitButtonClick(object sender, RoutedEventArgs e)
        {
            SubmitInvoked?.Invoke(this, _textBox.Text);
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            CancelInvoked?.Invoke(this, null);
        }

        private void OnTextBoxPaste(object sender, TextControlPasteEventArgs e)
        {
            Paste?.Invoke(sender, e);
        }

        private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var shift = _coreWindow.GetKeyState(VirtualKey.Shift);
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    var start = _textBox.SelectionStart;
                    _textBox.Text = _textBox.Text.Insert(start, "\r\n");
                    _textBox.SelectionStart = start + 1;
                }
                else
                {
                    SendInvoked?.Invoke(this, _textBox.Text);
                }
            }
            else if (e.Key == VirtualKey.Up && string.IsNullOrWhiteSpace(_textBox.Text))
            {
                EditInvoked?.Invoke(this, null);
            }
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_textBox.Text))
            {
                ShouldSendTyping?.Invoke(this, null);
            }

            Text = _textBox.Text; // fuck you

            if (Text.EndsWith(':'))
            {
                this.FindChild<Popup>("PART_SuggestionPopup").IsOpen = true;
            }
        }
    }
}
