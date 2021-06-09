using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace Unicord
{
    public class ThreadHandlerCollection<T> where T : Delegate
    {
        public ThreadHandlerCollection(SynchronizationContext c)
        {
            context = c;
        }

        public readonly SynchronizationContext context;
        public T events;

        public void Add(T handler)
        {
            events = (T)Delegate.Combine(events, handler);
        }

        public void Remove(T handler)
        {
            events = (T)Delegate.Remove(events, handler);
        }
    }

}
