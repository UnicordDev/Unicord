using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;

namespace Unicord.Universal.Models
{
    public class ChannelPermissionViewModel
    {
        private DiscordOverwrite _overwrite;

        public ChannelPermissionViewModel(Permissions permission, DiscordOverwrite overwrite, ResourceLoader loader)
        {
            _overwrite = overwrite;

            Permission = permission;
            Title = loader.GetString($"{permission}Title");
            Subtitle = loader.GetString($"{permission}Subtitle");
            Allowed = overwrite.Allowed;
            Denied = overwrite.Denied;
        }

        public bool IsDirty { get; private set; }
        public Permissions Permission { get; private set; }
        public Permissions Allowed { get; private set; }
        public Permissions Denied { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public bool? Value
        {
            get => GetPermissionValue(Permission);
            set => SetPermissionValue(Permission, value);
        }

        private bool? GetPermissionValue(Permissions p)
        {
            if (Allowed.HasPermission(p))
                return true;

            if (Denied.HasPermission(p))
                return false;

            return null;
        }

        private void SetPermissionValue(Permissions p, bool? value)
        {
            Allowed = Allowed.Revoke(p);
            Denied = Denied.Revoke(p);

            if (value == true)
                Allowed = Allowed.Grant(p);

            if (value == false)
                Denied = Denied.Grant(p);
        }
    }

    public class ChannelPermissionEditViewModel : ViewModelBase
    {
        private DiscordOverwrite _overwrite;
        private ResourceLoader _resourceLoader;

        private static Permissions[] PERMISSION_GROUP_GENERAL = new[] { Permissions.AccessChannels, Permissions.ManageChannels, Permissions.ManageRoles };
        private static Permissions[] PERMISSION_GROUP_MEMBERSHIP = new[] { Permissions.CreateInstantInvite };
        private static Permissions[] PERMISSION_GROUP_TEXT = new[]
        {
            Permissions.SendMessages,
            Permissions.EmbedLinks,
            Permissions.AttachFiles,
            Permissions.AddReactions,
            Permissions.UseExternalEmojis,
            Permissions.MentionEveryone,
            Permissions.ManageMessages,
            Permissions.ReadMessageHistory,
            Permissions.SendTtsMessages,
            Permissions.ManageWebhooks
        };
        private static Permissions[] PERMISSION_GROUP_VOICE = new[]
        {
            Permissions.UseVoice,
            Permissions.Speak,
            Permissions.Stream,
            Permissions.UseVoiceDetection,
            Permissions.PrioritySpeaker,
            Permissions.MuteMembers,
            Permissions.DeafenMembers,
            Permissions.MoveMembers
        };

        public ChannelPermissionEditViewModel(DiscordOverwrite overwrite)
        {
            _overwrite = overwrite;
            _resourceLoader = ResourceLoader.GetForViewIndependentUse("Permissions");

            General = PERMISSION_GROUP_GENERAL.Select(p => new ChannelPermissionViewModel(p, overwrite, _resourceLoader)).ToList();
            Membership = PERMISSION_GROUP_MEMBERSHIP.Select(p => new ChannelPermissionViewModel(p, overwrite, _resourceLoader)).ToList();
            Text = PERMISSION_GROUP_TEXT.Select(p => new ChannelPermissionViewModel(p, overwrite, _resourceLoader)).ToList();
            Voice = PERMISSION_GROUP_VOICE.Select(p => new ChannelPermissionViewModel(p, overwrite, _resourceLoader)).ToList();
        }

        public List<ChannelPermissionViewModel> General { get; }
        public List<ChannelPermissionViewModel> Membership { get; }
        public List<ChannelPermissionViewModel> Text { get; }
        public List<ChannelPermissionViewModel> Voice { get; }
    }
}
