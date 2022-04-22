using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Unicord.Universal.Models;

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
