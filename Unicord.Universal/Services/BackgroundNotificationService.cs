using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace Unicord.Universal.Services
{
    class BackgroundNotificationService : BaseService<BackgroundNotificationService>
    {
        private ApplicationTrigger _applicationTrigger;
        private AppServiceConnection _appServiceConnection;

        internal async Task ConnectAsync()
        {
            

        }

        internal async Task SetSuspendedAsync(bool suspended)
        {

        }

        internal void Disconnect()
        {
            Logger.Log("Disconnect");

            _appServiceConnection?.Dispose();
            _appServiceConnection = null;
        }

        private void OnRequest(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {

        }

        private void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Logger.Log("OnServiceClosed");

            _appServiceConnection = null;
        }
    }
}
