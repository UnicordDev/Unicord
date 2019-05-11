using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Calls;
using Windows.Foundation.Collections;

namespace Unicord.Universal.Voice
{
    public class VoiceViewModel
    {
        public static async Task<VoiceViewModel> StartNewAsync(DiscordChannel channel)
        {
            var model = new VoiceViewModel();

            var service = new AppServiceConnection
            {
                AppServiceName = "com.wankerr.Unicord.Voice",
                PackageFamilyName = Package.Current.Id.FamilyName
            };

            var status = await service.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                throw new Exception("Whoops??");
            }

            var resp = await service.SendMessageAsync(new ValueSet()
            {
                ["request"] = "connect",
                ["token"] = App.Discord.Configuration.Token,
                ["server"] = channel.GuildId.ToString(),
                ["channel"] = channel.Id.ToString()
            });

            if (resp.Status == AppServiceResponseStatus.Success)
            {
                var connected = (bool)resp.Message["connected"];
                if (connected)
                {
                    service.RequestReceived += model.RequestRecieved;
                    return model;
                }
                else
                {
                    throw new Exception(resp.Message["error"] as string);
                }
            }

            return null;
        }

        private void RequestRecieved(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {

        }
    }
}
