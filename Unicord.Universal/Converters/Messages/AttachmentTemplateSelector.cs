using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Converters
{
    public class AttachmentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate AudioTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate SvgTemplate { get; set; }
        public DataTemplate GenericTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var attachmentType = AttachmentType.Generic;
            if (item is AttachmentViewModel viewModel)
            {
                attachmentType = viewModel.Type;
            }

            return attachmentType switch
            {
                AttachmentType.Image => ImageTemplate,
                AttachmentType.Audio => AudioTemplate,
                AttachmentType.Video => VideoTemplate,
                AttachmentType.Svg => SvgTemplate,
                AttachmentType.Generic => GenericTemplate,
                _ => throw new InvalidOperationException(),
            };
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
