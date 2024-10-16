﻿using System.Threading.Tasks;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            DataContext = Model;
            Model.InvokePropertyChanged("");
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await Model.SaveAsync();
            await UpdateVoiceSettings();
        }

        private async Task UpdateVoiceSettings()
        {
            try
            {
                if (this.FindParent<DiscordPage>()?.DataContext is DiscordPageViewModel model)
                {
                    var inputInfo = Model.InputDevice;
                    var outputInfo = Model.OutputDevice;
                    //await (model?.VoiceModel?.UpdatePreferredAudioDevicesAsync(outputInfo?.Id, inputInfo?.Id) ?? Task.CompletedTask);
                }
            }
            catch { }
        }
    }
}
