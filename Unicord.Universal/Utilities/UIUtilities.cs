using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Dialogs;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Utilities
{
    internal static class UIUtilities
    {
        public static async Task ShowErrorDialogAsync(string title, string content, string icon = null)
        {
            try
            {
                var dialog = new ErrorDialog() { Title = title, Content = content };

                if (icon != null)
                {
                    dialog.Icon = icon;
                }

                await dialog.ShowAsync();
            }
            catch { }
        }

        public static async Task<bool> ShowYesNoDialogAsync(string title, string content, string icon = null)
        {
            try
            {
                var dialog = new YesNoDialog() { Title = title, Content = content };

                if (icon != null)
                {
                    dialog.Icon = icon;
                }

                return await dialog.ShowAsync() == ContentDialogResult.Primary ? true : false;
            }
            catch
            {
                return false;
            }
        }
    }
}
