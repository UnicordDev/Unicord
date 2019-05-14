using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace Unicord.Universal.Utilities
{
    internal static class NetworkHelper
    {
        public static bool IsNetworkLimited { get; private set; }

        static NetworkHelper()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            UpdateNetworkInfo();
        }

        private static void UpdateNetworkInfo()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            var cost = profile.GetConnectionCost();
            if (profile.IsWwanConnectionProfile || cost.NetworkCostType != NetworkCostType.Unrestricted || cost.Roaming)
            {
                IsNetworkLimited = true;
            }
        }

        private static void NetworkInformation_NetworkStatusChanged(object sender)
        {
            UpdateNetworkInfo();
        }
    }
}
