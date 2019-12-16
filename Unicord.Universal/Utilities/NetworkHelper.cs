using Windows.Networking.Connectivity;

namespace Unicord.Universal.Utilities
{
    /// <summary>
    /// Provides some static properties that give information about the current network state.
    /// </summary>
    internal static class NetworkHelper
    {
        public static bool IsNetworkConnected { get; private set; }
        public static bool IsNetworkLimited { get; private set; }

        static NetworkHelper()
        {
            UpdateNetworkInfo();
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private static void UpdateNetworkInfo()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile != null)
            {
                IsNetworkConnected = profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;

                var cost = profile.GetConnectionCost();
                if (profile.IsWwanConnectionProfile || cost == null || cost.NetworkCostType != NetworkCostType.Unrestricted || cost.Roaming)
                {
                    IsNetworkLimited = true;
                }
            }
            else
            {
                IsNetworkConnected = false;
            }
        }

        private static void NetworkInformation_NetworkStatusChanged(object sender)
        {
            UpdateNetworkInfo();
        }
    }
}
