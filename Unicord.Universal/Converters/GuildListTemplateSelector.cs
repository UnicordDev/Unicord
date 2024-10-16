using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Converters
{
    public class GuildListTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GuildTemplate { get; set; }
        public DataTemplate GuildFolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is GuildListFolderViewModel)
            {
                return GuildFolderTemplate;
            }

            return GuildTemplate;
        }
    }
}
