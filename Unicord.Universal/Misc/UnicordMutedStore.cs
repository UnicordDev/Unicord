using System.Collections.Concurrent;
using DSharpPlus;

namespace Unicord.Universal.Misc
{
    class UnicordMutedStore : IMutedStore
    {
        private ConcurrentDictionary<ulong, bool> _mutedChannelStore;
        private ConcurrentDictionary<ulong, bool> _mutedServerStore;

        internal UnicordMutedStore()
        {
            _mutedChannelStore = App.RoamingSettings.Read("MutedChannels", new ConcurrentDictionary<ulong, bool>());
            _mutedServerStore = App.RoamingSettings.Read("MutedServers", new ConcurrentDictionary<ulong, bool>());
        }

        public bool GetMutedChannel(ulong id)
        {
            if (_mutedChannelStore.TryGetValue(id, out var muted))
            {
                return muted;
            }

            return false;
        }

        public void SetMutedChannel(ulong id, bool muted)
        {
            _mutedChannelStore[id] = muted;
            App.RoamingSettings.Save("MutedChannels", _mutedChannelStore);
        }

        public bool GetMutedGuild(ulong id)
        {
            if (_mutedServerStore.TryGetValue(id, out var muted))
            {
                return muted;
            }

            return false;
        }

        public void SetMutedGuild(ulong id, bool muted)
        {
            _mutedServerStore[id] = muted;
            App.RoamingSettings.Save("MutedServers", _mutedServerStore);
        }
    }
}
