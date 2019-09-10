using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VoiceSettingsPage : Page
    {
        private VoiceSettingsModel Model { get; set; }

        public VoiceSettingsPage()
        {
            Model = new VoiceSettingsModel();
            Model.PropertyChanged += Model_PropertyChanged;

            InitializeComponent();
        }

        private async void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await UpdateVoiceSettings();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Model.LoadAsync();
            Bindings.Update();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await Model.SaveAsync();
            await UpdateVoiceSettings();
        }

        private async System.Threading.Tasks.Task UpdateVoiceSettings()
        {
            try
            {
                if (this.FindParent<DiscordPage>().DataContext is DiscordPageModel model)
                {
                    var inputInfo = Model.AvailableInputDevices.ElementAtOrDefault(Model.InputDevice);
                    var outputInfo = Model.AvailableOutputDevices.ElementAtOrDefault(Model.OutputDevice);
                    await model.VoiceModel?.UpdatePreferredAudioDevicesAsync(outputInfo?.Id, inputInfo?.Id);
                }
            }
            catch { }
        }
    }
}
