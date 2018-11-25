using System;
using System.Windows;
using System.Windows.Media;

namespace WamWooWam.Wpf
{
    public static class Extensions
    {
        public static T FindVisualParent<T>(this DependencyObject obj) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(obj);

            if (parent == null)
            {
                return null;
            }

            if (parent is T p)
            {
                return p;
            }
            else
            {
                return FindVisualParent<T>(parent);
            }
        }

        public static T FindLogicalParent<T>(this DependencyObject obj) where T : DependencyObject
        {
            var parent = LogicalTreeHelper.GetParent(obj);

            if (parent == null)
            {
                return null;
            }

            if (parent is T p)
            {
                return p;
            }
            else
            {
                return FindLogicalParent<T>(parent);
            }
        }

        public static T GetVisualChild<T>(this DependencyObject parent, string name = null) where T : DependencyObject
        {
            var child = default(T);

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null && (name == null || (child as FrameworkElement).Name == name))
                {
                    break;
                }
            }
            return child;
        }

        public static T FirstVisualChild<T>(this DependencyObject parent, Func<T, bool> precidate) where T : DependencyObject
        {
            var child = default(T);

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var v = VisualTreeHelper.GetChild(parent, i);
                if (v is T t && precidate(t))
                {
                    return t;
                }
                else
                {
                    child = FirstVisualChild(v, precidate);
                    if (child != null)
                    {
                        return child;
                    }
                }
            }
            return child;
        }
    }
}
