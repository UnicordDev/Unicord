using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Extensions
{
    internal static class VisualTreeExtensions
    {
        public static T FindChild<T>(this DependencyObject parent, string controlName = null) where T : FrameworkElement
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); ++index)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T t && (controlName == null || t.Name == controlName))
                {
                    return t;
                }
                else if ((child = FindChild<T>(child, controlName)) != null)
                {
                    return child as T;
                }
            }

            return default;
        }

        public static T FindParent<T>(this DependencyObject obj, string controlName = null) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent == null)
                return default;

            return parent is T found && (controlName == null || found.Name == controlName) ? found : parent.FindParent<T>(controlName);
        }
    }
}