using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Controls.Flyouts;
using Unicord.Universal.Pages;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Unicord.Constants;

namespace Unicord.Universal.Controls.Messages
{
    public class MessageControl : Control
    {
        private bool _addedEditHandlers;

        #region Dependency Properties
        public DiscordMessage Message
        {
            get { return (DiscordMessage)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(DiscordMessage), typeof(MessageControl), new PropertyMetadata(null, OnPropertyChanged));

        public DiscordUser Author => Message.Author;
        public DiscordChannel Channel => Message.Channel;

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageControl control)
            {
                if (e.Property == MessageProperty)
                {
                    control.OnMessageChanged(e);
                }
            }
        }

        #endregion

        public MessageControl()
        {
            this.DefaultStyleKey = typeof(MessageControl);
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //if (this.Style == null)
            //{
            //    ApplyCustomStyles();
            //}
        }

        //protected virtual void ApplyCustomStyles()
        //{
        //    // if the style isn't explicitly set, load it
        //    if (!App.Current.Resources.TryGetValue(App.LocalSettings.Read(MESSAGE_STYLE_KEY, MESSAGE_STYLE_DEFAULT), out var obj) || !(obj is Style s))
        //    {
        //        s = App.Current.Resources[MESSAGE_STYLE_DEFAULT] as Style;
        //        App.LocalSettings.Save(MESSAGE_STYLE_KEY, MESSAGE_STYLE_DEFAULT);
        //    }

        //    this.Style = s;
        //}

        protected override void OnApplyTemplate()
        {
            this.UpdateCollapsedState();
        }

        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            base.OnRightTapped(e);
        }

        protected virtual void OnMessageChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DiscordMessage message)
            {
                this.DataContext = message;
                this.UpdateCollapsedState();
            }
            else
            {
                this.DataContext = null;
                // reset
            }
        }

        // TODO: Could prolly move this somewhere better
        private void UpdateCollapsedState()
        {
            if (Message == null || !IsEnabled)
                return;

            VisualStateManager.GoToState(this, "NotEditing", false);

            var list = this.FindParent<ListView>();
            if (list != null)
            {
                if (list.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    VisualStateManager.GoToState(this, "EditMode", false);
                    return;
                }

                var index = list.Items.IndexOf(Message);

                if (index > 0)
                {
                    if (list.Items[index - 1] is DiscordMessage other && (other.MessageType == MessageType.Default || other.MessageType == MessageType.Reply) && Message.MessageType == MessageType.Default)
                    {
                        var timeSpan = (Message.CreationTimestamp - other.CreationTimestamp);
                        if (other.Author.Id == Message.Author.Id && timeSpan <= TimeSpan.FromMinutes(10))
                        {
                            VisualStateManager.GoToState(this, "Collapsed", false);
                            return;
                        }
                    }
                }
            }

            VisualStateManager.GoToState(this, "Normal", false);
        }

        public virtual void BeginEdit()
        {
            VisualStateManager.GoToState(this, "Editing", true);

            var control = GetTemplateChild("MessageEditTools") as MessageEditTools;
            control.ApplyTemplate();

            var editBox = control.FindChild<TextBox>("MessageEditBox");

            if (!_addedEditHandlers)
            {
                var editFinishButton = control.FindChild<Button>("MessageEditFinishButton");
                var editCancelButton = control.FindChild<Button>("MessageEditCancelButton");

                editBox.KeyDown += this.OnEditKeyDown;
                editFinishButton.Click += this.OnEditFinishClick;
                editCancelButton.Click += this.OnEditCancelClick;

                _addedEditHandlers = true;
            }

            editBox.Focus(FocusState.Keyboard);
            editBox.SelectionStart = editBox.Text.Length;
        }

        private async Task SaveEditAsync()
        {
            try
            {
                var control = this.FindChild<MessageEditTools>();
                var editBox = control.FindChild<TextBox>("MessageEditBox");
                if (!string.IsNullOrWhiteSpace(editBox.Text) && editBox.Text != Message.Content)
                {
                    await Message.ModifyAsync(editBox.Text);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public virtual void EndEdit()
        {
            VisualStateManager.GoToState(this, "NotEditing", true);

            var page = this.FindParent<ChannelPage>();
            if (page != null)
            {
                page.FocusTextBox();
            }
        }

        private async void OnEditKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var textBox = (sender as TextBox);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    var start = textBox.SelectionStart;
                    textBox.Text = textBox.Text.Insert(start, "\r\n");
                    textBox.SelectionStart = start + 1;
                }
                else
                {
                    EndEdit();
                    await SaveEditAsync();
                }
            }

            if (e.Key == VirtualKey.Escape)
            {
                EndEdit();
            }
        }

        private async void OnEditFinishClick(object sender, RoutedEventArgs e)
        {
            EndEdit();
            await SaveEditAsync();
        }

        private void OnEditCancelClick(object sender, RoutedEventArgs e)
        {
            EndEdit();
        }
    }
}
