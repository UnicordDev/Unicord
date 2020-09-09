using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Unicord
{
    /// <summary>
    /// Provides a base class to objects that can raise property change events 
    /// via <see cref="INotifyPropertyChanged"/> in a thread safe manner
    /// </summary>
    public class NotifyPropertyChangeImpl : INotifyPropertyChanged
    {
        private class ThreadHandlerCollection
        {
            public ThreadHandlerCollection(SynchronizationContext c)
            {
                context = c;
            }

            public readonly SynchronizationContext context;
            public PropertyChangedEventHandler events;

            public void Add(PropertyChangedEventHandler handler)
            {
                events = (PropertyChangedEventHandler)Delegate.Combine(events, handler);
            }

            public void Remove(PropertyChangedEventHandler handler)
            {
                events = (PropertyChangedEventHandler)Delegate.Remove(events, handler);
            }
        }

        private ThreadLocal<ThreadHandlerCollection> _propertyChangedEvents;

        private ThreadHandlerCollection PropertyChangeEvents =>
            (_propertyChangedEvents ?? (_propertyChangedEvents = new ThreadLocal<ThreadHandlerCollection>(() => new ThreadHandlerCollection(SynchronizationContext.Current), true))).Value;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => PropertyChangeEvents.Add(value);
            remove => PropertyChangeEvents.Remove(value);
        }

        // Holy hell is the C# Discord great.
        // Y'all should join https://aka.ms/csharp-discord
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnPropertySet<T>(ref T oldValue, T newValue, [CallerMemberName] string property = null)
        {
            if (oldValue == null || newValue == null || !newValue.Equals(oldValue))
            {
                oldValue = newValue;
                InvokePropertyChanged(property);
            }
        }

        // overload might avoid boxing?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnPropertySet<T>(ref T oldValue, T newValue, params string[] additionalProperties)
        {
            if (oldValue == null || newValue == null || !newValue.Equals(oldValue))
            {
                oldValue = newValue;

                foreach (var str in additionalProperties)
                {
                    InvokePropertyChanged(str);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void InvokePropertyChanged([CallerMemberName] string property = null)
        {
            if (_propertyChangedEvents == null)
                return;

            var args = new PropertyChangedEventArgs(property);
            var context = SynchronizationContext.Current;
            foreach (var item in _propertyChangedEvents.Values)
            {
                if (item.context == context || item.context == null)
                {
                    InvokeHandler(args, item.events);
                }
                else
                {
                    item.context.Post(o => InvokeHandler(args, item.events), null);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeHandler(PropertyChangedEventArgs args, PropertyChangedEventHandler handler)
        {
            try
            {
                handler?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in binding: {0}", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UnsafeInvokePropertyChanged(string property)
        {
            if (_propertyChangedEvents == null)
                return;

            var args = new PropertyChangedEventArgs(property);
            foreach (var item in _propertyChangedEvents.Values)
            {
                item.events?.Invoke(this, args);
            }
        }
    }
}
