using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Pages;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Commands
{
    class MessageUserCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is DiscordMember m && m.Id != App.Discord.CurrentUser.Id)
            {
                return true;
            }
            else if (parameter is DiscordUser u && u.Id != App.Discord.CurrentUser.Id && App.Discord.Guilds.ToArray().Any(g => g.Value.Members.ToArray().Any(gm => gm.Id == u.Id)))
            {
                return true;
            }

            return false;
        }

        public async void Execute(object parameter)
        {
            if (parameter is DiscordUser user)
            {
                var channel = App.Discord.PrivateChannels.FirstOrDefault(c => c.Recipient?.Id == user.Id);
                if (channel == null)
                {
                    var member = user as DiscordMember ?? App.Discord.Guilds.Values.Select(g => g.Members.FirstOrDefault(m => m.Id == user.Id)).FirstOrDefault(m => m != null);
                    if (member != null)
                        channel = await member.CreateDmChannelAsync();
                }

                if (channel != null)
                {
                    var page = Window.Current.Content.FindChild<MainPage>();
                    if (page != null)
                    {
                        var discordPage = page.FindChild<DiscordPage>();
                        discordPage.Navigate(channel, new DrillInNavigationTransitionInfo());
                        page.HideOverlay();
                    }
                }
            }
        }
    }
}
