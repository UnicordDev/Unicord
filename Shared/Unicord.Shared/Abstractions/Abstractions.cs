using System;
using System.Collections.Generic;
using System.Text;

namespace Unicord.Abstractions
{
    public abstract class Abstractions<T> 
    {
        public static T Current { get; private set; }

        public static void SetAbstractions<TAbstractions>() where TAbstractions : T
            => Current = Activator.CreateInstance<TAbstractions>();
    }
}
