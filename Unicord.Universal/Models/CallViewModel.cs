using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Unicord.Universal.Voice;

namespace Unicord.Universal.Models
{
    public class CallViewModel : PropertyChangedBase, IDisposable
    {
        public CallViewModel(DiscordCall call)
        {
            Call = call;
            Call.PropertyChanged += OnPropertyChanged;
            App.Discord.VoiceStateUpdated += OnVoiceStateUpdated;
            App.Discord.CallCreated += OnCallCreated;
            App.Discord.CallUpdated += OnCallUpdated;
            App.Discord.CallDeleted += OnCallDeleted;

            if (!VoiceConnectionModel.OngoingCalls.TryGetValue(Call.Channel, out var model))
            {
                model = new VoiceConnectionModel(Call);
                VoiceConnectionModel.OngoingCalls[Call.Channel] = model;
            }

            Connection = model;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvokePropertyChanged(string.Empty);
        }

        private Task OnCallCreated(CallCreateEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task OnCallUpdated(CallUpdateEventArgs e)
        {
            if (e.Channel == Call.Channel)
            {
                // todo: more efficient invoking
                InvokePropertyChanged(string.Empty); 
            }

            return Task.CompletedTask;
        }

        private Task OnCallDeleted(CallDeleteEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task OnVoiceStateUpdated(VoiceStateUpdateEventArgs e)
        {
            if (e.Channel == Call.Channel || e.After?.Channel == Call.Channel || e.Before?.Channel == Call.Channel)
            {
                InvokePropertyChanged(string.Empty);
            }

            return Task.CompletedTask;
        }

        public IEnumerable<DiscordCallState> CallStates => Call.CallStates;

        public bool IsActive => Call.VoiceStates.ContainsKey(App.Discord.CurrentUser.Id) && (VoiceConnectionModel.OngoingCalls.TryGetValue(Call.Channel, out var model) ? model.IsConnected : false);
        public bool IsNotActive => !IsActive;

        public DiscordCall Call { get; }
        public VoiceConnectionModel Connection { get; }

        public void Dispose()
        {
            Call.PropertyChanged -= OnPropertyChanged;
            App.Discord.VoiceStateUpdated -= OnVoiceStateUpdated;
        }
    }
}
