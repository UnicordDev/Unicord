using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Unicord.Universal.Background
{
    class Program
    {
        private static Mutex _mutex;

        [STAThread]
        static void Main(string[] args)
        {
            _mutex = new Mutex(true, "{88FE061B-B4D8-41F4-99FE-15870E0F535B}", out var createdNew);
            if (!createdNew) return;

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.Run(new NotificationApplicationContext());
            }
            finally
            {
                _mutex.Dispose();
            }
        }
    }
}
