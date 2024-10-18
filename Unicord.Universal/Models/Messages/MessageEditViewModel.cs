using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Unicord.Universal.Dialogs;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;
using Unicord.Universal.Extensions;

namespace Unicord.Universal.Models.Messages
{
    // TODO: this
    public class MessageEditViewModel : ViewModelBase
    {
        private MessageViewModel _parent;
        private string _content;
        private ILogger<MessageEditViewModel> _logger
            = Logger.GetLogger<MessageEditViewModel>();
        private int _selectionStart;

        public MessageEditViewModel(MessageViewModel viewModel)
            : base(viewModel)
        {
            this._parent = viewModel;
            this.Content = viewModel.Content;
            this.SelectionStart = viewModel.Content.Length;
            this.CommitCommand = new AsyncRelayCommand<TextBox>(CommitAsync);
            this.DiscardCommand = new RelayCommand(Discard);
            this.InsertNewLineCommand = new RelayCommand<TextBox>(InsertNewLine);

            InvokePropertyChanged(nameof(IsFocused));
        }

        // these properties, unlike most VM properties, are NOT THREAD SAFE, this helps when handling focus
        public string Content { get => _content; set => UnsafeOnPropertySet(ref _content, value); }
        public int SelectionStart { get => _selectionStart; set => UnsafeOnPropertySet(ref _selectionStart, value); }

        public ICommand CommitCommand { get; }
        public ICommand DiscardCommand { get; }
        public ICommand InsertNewLineCommand { get; }

        public bool IsFocused => true;

        public void InsertNewLine(TextBox textBox)
        {
            var start = textBox.SelectionStart;
            Content = textBox.Text.Insert(start, "\r");
            textBox.SelectionStart = start + 1;
        }

        private async Task CommitAsync(TextBox textBox)
        {
            try
            {
                if (textBox != null)
                    Content = textBox.Text;

                // prompt for deletion
                if (string.IsNullOrWhiteSpace(Content))
                {
                    var deleteDialog = new DeleteMessageDialog();
                    deleteDialog.Message = _parent.Message;

                    var result = await deleteDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                        await _parent.Message.DeleteAsync();

                    return;
                }

                if (Content != _parent.Content)
                {
                    await _parent.Message.ModifyAsync(Content);
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit message");
            }
            finally
            {
                _parent.IsEditing = false;

                var page = Window.Current.Content.FindChild<ChannelPage>();
                if (page != null)
                    page.FocusTextBox();
            }
        }

        private void Discard()
        {
            _parent.IsEditing = false;

            var page = Window.Current.Content.FindChild<ChannelPage>();
            if (page != null)
                page.FocusTextBox();
        }
    }
}
