using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Unicord.Universal.Notifications
{
    public sealed class NotificationBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
        }
    }
}
