using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Windows.Devices.Enumeration;

namespace Unicord.Universal.Models
{
    public class VoiceSettingsModel : PropertyChangedBase
    {
        private int _inputDevice;
        private int _outputDevice;

        public VoiceSettingsModel()
        {
            AvailableInputDevices = new List<DeviceInformation>();
            AvailableOutputDevices = new List<DeviceInformation>();
            AvailableInputDevices.Add(null);
            AvailableOutputDevices.Add(null);
        }

        public async Task LoadAsync()
        {
            foreach (var dev in await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture))
            {
                AvailableInputDevices.Add(dev);
            }

            foreach (var dev in await DeviceInformation.FindAllAsync(DeviceClass.AudioRender))
            {
                AvailableOutputDevices.Add(dev);
            }

            var inputDeviceId = App.LocalSettings.Read<string>("InputDevice", null);
            var outputDeviceId = App.LocalSettings.Read<string>("OutputDevice", null);

            InputDevice = AvailableInputDevices.IndexOf(AvailableInputDevices.FirstOrDefault(d => d?.Id == inputDeviceId) ?? AvailableInputDevices.FirstOrDefault(d => d?.IsDefault == true));
            InputDevice = _inputDevice == -1 ? 0 : _inputDevice;

            OutputDevice = AvailableOutputDevices.IndexOf(AvailableOutputDevices.FirstOrDefault(d => d?.Id == outputDeviceId) ?? AvailableOutputDevices.FirstOrDefault(d => d?.IsDefault == true));
            OutputDevice = _outputDevice == -1 ? 0 : _outputDevice;

            InvokePropertyChanged(nameof(InputDevice));
            InvokePropertyChanged(nameof(OutputDevice));
        }

        internal Task SaveAsync()
        {
            App.LocalSettings.Save("InputDevice", AvailableInputDevices.ElementAtOrDefault(InputDevice)?.Id);
            App.LocalSettings.Save("OutputDevice", AvailableOutputDevices.ElementAtOrDefault(OutputDevice)?.Id);

            return Task.CompletedTask;
        }

        public List<DeviceInformation> AvailableInputDevices { get; set; }
        public List<DeviceInformation> AvailableOutputDevices { get; set; }

        public int InputDevice
        {
            get => _inputDevice;
            set => OnPropertySet(ref _inputDevice, value);
        }

        public int OutputDevice
        {
            get => _outputDevice;
            set => OnPropertySet(ref _outputDevice, value);
        }
    }
}
