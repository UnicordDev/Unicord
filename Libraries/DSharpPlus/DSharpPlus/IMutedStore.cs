using System;
using System.Collections.Generic;
using System.Text;

namespace DSharpPlus
{
    public interface IMutedStore
    {
        bool GetMuted(ulong id);
        void SetMuted(ulong id, bool muted);
    }
}
