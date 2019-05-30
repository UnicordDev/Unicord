using System;
using System.Collections.Generic;
using System.Text;

namespace DSharpPlus
{
    public interface IMutedStore
    {
        bool GetMutedChannel(ulong id);
        void SetMutedChannel(ulong id, bool muted);

        bool GetMutedGuild(ulong id);
        void SetMutedGuild(ulong id, bool muted);
    }
}
