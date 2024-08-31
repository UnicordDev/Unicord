using System;
using System.Linq;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Commands
{
    class MessageUserCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            if (App.Discord?.CurrentUser == null)
                return false;

            if (parameter is DiscordMember m && m.Id != App.Discord.CurrentUser.Id)
            {
                return true;
            }
            else if (parameter is DiscordUser u && u.Id != App.Discord?.CurrentUser?.Id)
            {
                if (App.Discord.Relationships.TryGetValue(u.Id, out var rel) && rel.RelationshipType == DiscordRelationshipType.Friend)
                {
                    return true;
                }

                if (App.Discord.Guilds.Values.Any(g => g.Members.ContainsKey(u.Id)))
                {
                    return true;
                }
            }

            return false;
        }

        public async void Execute(object parameter)
        {
            Analytics.TrackEvent("MessageUserCommand_Invoked");

            if (parameter is DiscordUser user)
            {
                var channel = App.Discord.PrivateChannels.Values.FirstOrDefault(c => c.Recipients.FirstOrDefault()?.Id == user.Id);
                if (channel == null)
                {
                    if ((App.Discord.Relationships.TryGetValue(user.Id, out var rel) && rel.RelationshipType == DiscordRelationshipType.Friend) ||
                        App.Discord.Guilds.Any(g => g.Value.Members.ContainsKey(user.Id)))
                    {
                        // apparently this is bad
                        // channel = await App.Discord.CreateDmChannelAsync(user.Id);
                    }
                }

                if (channel != null)
                {
                    var page = Window.Current.Content.FindChild<MainPage>();
                    if (page != null)
                    {
                        page.HideUserOverlay();

                        var service = DiscordNavigationService.GetForCurrentView();
                        await service.NavigateAsync(channel);
                    }
                }
            }
        }
    }
}
