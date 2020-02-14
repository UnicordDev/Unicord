using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Windows.Devices.Enumeration;

namespace Unicord.Universal.Models
{
    public class DeviceInformationWrapper
    {
        public DeviceInformation Info { get; set; }

        public string Id => Info?.Id;
        public string Name => Info?.Name;

        public static implicit operator DeviceInformationWrapper(DeviceInformation info) { return new DeviceInformationWrapper() { Info = info }; }
    }

    public class VoiceSettingsModel : NotifyPropertyChangeImpl
    {
        private DeviceInformationWrapper _inputDevice;
        private DeviceInformationWrapper _outputDevice;

        public VoiceSettingsModel()
        {
            AvailableInputDevices = new List<DeviceInformationWrapper>();
            AvailableOutputDevices = new List<DeviceInformationWrapper>();
            AvailableInputDevices.Add(new DeviceInformationWrapper());
            AvailableOutputDevices.Add(new DeviceInformationWrapper());
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

            _inputDevice = AvailableInputDevices.FirstOrDefault(d => d?.Id == inputDeviceId);
            _outputDevice = AvailableOutputDevices.FirstOrDefault(d => d?.Id == outputDeviceId);

            InvokePropertyChanged(nameof(AvailableInputDevices));
            InvokePropertyChanged(nameof(AvailableOutputDevices));
            InvokePropertyChanged(nameof(InputDevice));
            InvokePropertyChanged(nameof(OutputDevice));
        }

        internal Task SaveAsync()
        {
            App.LocalSettings.Save("InputDevice", InputDevice?.Id);
            App.LocalSettings.Save("OutputDevice", OutputDevice?.Id);

            return Task.CompletedTask;
        }

        public List<DeviceInformationWrapper> AvailableInputDevices { get; set; }
        public List<DeviceInformationWrapper> AvailableOutputDevices { get; set; }

        public DeviceInformationWrapper InputDevice
        {
            get => _inputDevice;
            set => OnPropertySet(ref _inputDevice, value);
        }

        public DeviceInformationWrapper OutputDevice
        {
            get => _outputDevice;
            set => OnPropertySet(ref _outputDevice, value);
        }
    }
}
