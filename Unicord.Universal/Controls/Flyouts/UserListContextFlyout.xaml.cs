using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserListContextFlyout : MenuFlyout
    {
        public UserListContextFlyout()
        {
            InitializeComponent();
        }

        public bool CanKick =>
            (Target.DataContext is DiscordMember member) ? member.Guild.CurrentMember.PermissionsIn(null).HasPermission(DSharpPlus.Permissions.KickMembers) : false;

        public bool CanBan =>
            (Target.DataContext is DiscordMember member) ? member.Guild.CurrentMember.PermissionsIn(null).HasPermission(DSharpPlus.Permissions.BanMembers) : false;

        public bool CanChangeNickname
        {
            get
            {
                if (Target.DataContext is DiscordMember member)
                {
                    var perms = member.PermissionsIn(null);
                    if (member.Id == App.Discord.CurrentUser.Id)
                    {
                        return perms.HasPermission(DSharpPlus.Permissions.ChangeNickname);
                    }
                    else
                    {
                        return perms.HasPermission(DSharpPlus.Permissions.ManageNicknames);
                    }
                }

                return false;
            }
        }

        public bool ShowManagementSeparator =>
            CanKick || CanBan || CanChangeNickname;
    }
}
