using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Unicord.Universal.Utilities
{
    class ThemeHelpers
    {
        public static async Task<Theme> SafeInstallFromArchiveAsync(StorageFile theme, bool noPrompt = false)
        {
            try
            {
                return await ThemeManager.InstallFromArchiveAsync(theme, noPrompt);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync("Failed to install theme!", ex.Message);
            }

            return null;
        }

        public static async Task RequestRestartAsync()
        {
            if (ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "RequestRestartAsync"))
            {
                var resources = ResourceLoader.GetForCurrentView("ThemesSettingsPage");
                if (await UIUtilities.ShowYesNoDialogAsync(resources.GetString("ThemeChangedTitle"), resources.GetString("ThemeChangedMessage")))
                {
                    await CoreApplication.RequestRestartAsync("");
                }
            }
        }
    }
}
