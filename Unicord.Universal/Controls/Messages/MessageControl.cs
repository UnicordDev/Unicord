using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Pages;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using static Unicord.Constants;

namespace Unicord.Universal.Controls.Messages
{
    public class MessageControl : Control
    {
        private bool _addedEditHandlers;
        private ImageBrush _imageBrush;

        #region Dependency Properties

        public MessageViewModel MessageViewModel
        {
            get { return (MessageViewModel)GetValue(MessageViewModelProperty); }
            set { SetValue(MessageViewModelProperty, value); }
        }

        public static readonly DependencyProperty MessageViewModelProperty =
            DependencyProperty.Register("MessageViewModel", typeof(MessageViewModel), typeof(MessageControl), new PropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageControl control && e.Property == MessageViewModelProperty)
            {
                control.OnMessageChanged(e);
            }
        }

        #endregion

        public MessageControl()
        {
            this.DefaultStyleKey = typeof(MessageControl);
        }

        protected override void OnApplyTemplate()
        {
            //this.UpdateCollapsedState();
        }

        protected virtual void OnMessageChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MessageViewModel message)
            {
                //this.DataContext = message;
                this.UpdateProfileImage(message);
            }
            else
            {
                //this.DataContext = null;
                this.ClearProfileImage();
                // reset
            }
        }

        private void ClearProfileImage()
        {
            if (_imageBrush == null)
            {
                var container = (Ellipse)this.GetTemplateChild("ImageContainer");
                if (container == null || container.Fill == null)
                    return;

                _imageBrush = (ImageBrush)container.Fill;
            }

            _imageBrush.ImageSource = null;
        }

        private void UpdateProfileImage(MessageViewModel message)
        {
            this.ClearProfileImage();

            if (_imageBrush == null || message.Author == null || message.Author.AvatarUrl == null)
                return;

            _imageBrush.ImageSource = new BitmapImage
            {
                UriSource = new Uri(message.Author.AvatarUrl),
                DecodePixelHeight = 64,
                DecodePixelWidth = 64,
                DecodePixelType = DecodePixelType.Physical
            };
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
                // TODO: this is horrible
                var control = this.FindChild<MessageEditTools>();
                var editBox = control.FindChild<TextBox>("MessageEditBox");
                var message = MessageViewModel.Message;
                if (!string.IsNullOrWhiteSpace(editBox.Text) && editBox.Text != message.Content)
                {
                    await message.ModifyAsync(editBox.Text);
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
