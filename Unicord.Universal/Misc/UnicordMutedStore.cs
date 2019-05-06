using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;

namespace Unicord.Universal.Misc
{
    class UnicordMutedStore : IMutedStore
    {
        private ConcurrentDictionary<ulong, bool> _mutedStore;

        internal UnicordMutedStore()
        {
            _mutedStore = App.RoamingSettings.Read("MutedChannels", new ConcurrentDictionary<ulong, bool>());
        }

        public bool GetMuted(ulong id)
        {
            if(_mutedStore.TryGetValue(id, out var muted))
            {
                return muted;
            }

            return false;
        }

        public void SetMuted(ulong id, bool muted)
        {
            _mutedStore[id] = muted;
            App.RoamingSettings.Save("MutedChannels", _mutedStore);
        }
    }
}
