using System;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Converters
{
    internal class EmbedTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate RichTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is not EmbedViewModel embedViewModel) throw new InvalidOperationException();

            if (embedViewModel.IsRawImage) return ImageTemplate;
            if (embedViewModel.IsRawVideo) return VideoTemplate;

            return RichTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
