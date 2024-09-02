using Microsoft.Toolkit.Uwp.UI.Controls;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls.Messages
{
    public sealed class EmbedFieldsView : ItemsControl
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (item is EmbedFieldViewModel field)
                Grid.SetColumnSpan((FrameworkElement)element, field.ColumnSpan);

            base.PrepareContainerForItemOverride(element, item);
        }
    }
}
