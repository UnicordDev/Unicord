using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Dialogs;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Utilities
{
    internal static class UIUtilities
    {
        private static Lazy<ResourceLoader> _resources
            = new Lazy<ResourceLoader>(() => ResourceLoader.GetForViewIndependentUse("Dialogs"));

        public static async Task ShowErrorDialogAsync(string title, string content, string icon = null, string resourceMap = null)
        {
            try
            {
                var resources = resourceMap != null ? ResourceLoader.GetForViewIndependentUse(resourceMap) : _resources.Value;   
                
                var actualTitle = resources.GetString(title);
                var actualContent = resources.GetString(content);

                var dialog = new ErrorDialog()
                {
                    Title = string.IsNullOrWhiteSpace(actualTitle) ? title : actualTitle,
                    Content = string.IsNullOrWhiteSpace(actualContent) ? content : actualContent
                };

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
                var dialog = new ConfirmationDialog() { Title = title, Content = content };

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
