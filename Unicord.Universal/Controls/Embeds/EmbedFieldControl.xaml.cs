using DSharpPlus.Entities;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Embeds
{
    public sealed partial class EmbedFieldControl : UserControl
    {
        private DiscordChannel _channel;

        public EmbedFieldControl(DiscordChannel channel, DiscordEmbedField field)
        {
            InitializeComponent();
            _channel = channel;
            DataContext = field;
        }
    }
}
