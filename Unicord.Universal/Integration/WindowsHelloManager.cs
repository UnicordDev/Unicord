using System;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials.UI;
using static Unicord.Constants;

namespace Unicord.Universal
{
    internal static class WindowsHelloManager
    {
        public static async Task<bool> VerifyAsync(string setting, string displayReason)
        {
            if ((DateTimeOffset.Now - App.RoamingSettings.Read("LastVerified", DateTimeOffset.MinValue))
                <= App.RoamingSettings.Read(AUTHENTICATION_TIME, TimeSpan.FromMinutes(5)))
            {
                return true;
            }

            if (!App.RoamingSettings.Read(setting, false))
            {
                return true;
            }

            Analytics.TrackEvent("WindowsHelloManager_Verify");

            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            var actualReason = resourceLoader.GetString(displayReason);
            actualReason = string.IsNullOrWhiteSpace(actualReason) ? displayReason : actualReason;

            var available = await UserConsentVerifier.CheckAvailabilityAsync();
            if (available == UserConsentVerifierAvailability.Available)
            {
                var consentResult = await UserConsentVerifier.RequestVerificationAsync(actualReason);
                if (consentResult == UserConsentVerificationResult.Verified)
                {
                    App.RoamingSettings.Save("LastVerified", DateTimeOffset.Now);
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }
    }
}
