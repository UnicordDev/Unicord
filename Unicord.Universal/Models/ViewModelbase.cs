using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using DSharpPlus;
using Unicord.Universal.Services;

namespace Unicord.Universal.Models
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected DiscordClient discord;
        protected SynchronizationContext syncContext;

        public ViewModelBase(ViewModelBase parent = null)
        {
            discord = DiscordManager.Discord; // capture the discord client
            syncContext = parent?.syncContext ?? SynchronizationContext.Current;
            Debug.Assert(discord != null);
            Debug.Assert(syncContext != null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
                syncContext.Post((o) =>
                {
                    foreach (var str in additionalProperties)
                    {
                        var args = new PropertyChangedEventArgs(str);
                        PropertyChanged?.Invoke(this, args);
                    }
                }, null);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UnsafeOnPropertySet<T>(ref T oldValue, T newValue, [CallerMemberName] string property = null)
        {
            if (oldValue == null || newValue == null || !newValue.Equals(oldValue))
            {
                oldValue = newValue;
                UnsafeInvokePropertyChanged(property);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UnsafeOnPropertySet<T>(ref T oldValue, T newValue, params string[] additionalProperties)
        {
            if (oldValue == null || newValue == null || !newValue.Equals(oldValue))
            {
                oldValue = newValue;
                foreach (var property in additionalProperties)
                {
                    UnsafeInvokePropertyChanged(property);
                }
            }
        }

        public virtual void InvokePropertyChanged([CallerMemberName] string property = null)
        {
            var args = new PropertyChangedEventArgs(property);
            syncContext.Post((o) => PropertyChanged?.Invoke(this, (PropertyChangedEventArgs)o), args);
        }

        protected void UnsafeInvokePropertyChanged([CallerMemberName] string property = null)
        {
            var args = new PropertyChangedEventArgs(property);
            PropertyChanged?.Invoke(this, args);
        }
    }
}
