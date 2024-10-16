using System.Linq;
using Unicord.Universal.Models.Emoji;
using Unicord.Universal.Models.Messages;

namespace Unicord.Universal.Commands.Messages
{
    internal class ReactCommand : DiscordCommand<MessageViewModel>
    {
        public ReactCommand(MessageViewModel message)
            : base(message)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return parameter is EmojiViewModel;
        }

        public override async void Execute(object parameter)
        {
            if (parameter is not EmojiViewModel emoji || emoji.DiscordEmoji == null)
                return;

            if (viewModel.Reactions.Any(r => r.IsMe && r.Emoji == emoji))
            {
                await viewModel.Message.DeleteOwnReactionAsync(emoji.DiscordEmoji);
            }
            else
            {
                await viewModel.Message.CreateReactionAsync(emoji.DiscordEmoji);
            }
        }
    }
}
