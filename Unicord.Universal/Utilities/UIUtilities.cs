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
        public static async Task ShowErrorDialogAsync(string title, string content)
        {
            try
            {
                var dialog = new ErrorDialog() { Title = title, Content = content };
                await dialog.ShowAsync();
            }
            catch { }
        }
    }
}
