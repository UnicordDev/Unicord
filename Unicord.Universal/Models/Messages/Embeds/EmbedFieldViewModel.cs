using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
    public class EmbedFieldViewModel : ViewModelBase
    {
        public EmbedFieldViewModel(DiscordEmbedField field, EmbedViewModel parent)
            : base(parent)
        {
            Title = field.Name;
            Text = field.Value;
            ColumnSpan = field.Inline ? 1 : 3;
            Channel = parent.Channel;
        }

        public string Title { get; }
        public string Text { get; }
        public int ColumnSpan { get; }
        public DiscordChannel Channel { get; }
    }
}
