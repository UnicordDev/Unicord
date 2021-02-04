using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Gaming.XboxGameBar;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.GameBar
{
    public sealed partial class GameBarFriendsPage : Page
    {
        private GameBarPageParameters _params;
        private XboxGameBarWidgetControl _control;

        public GameBarFriendsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _params = (GameBarPageParameters)e.Parameter;
            _control = new XboxGameBarWidgetControl(_params.Widget);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new FriendsViewModel();
        }

        private async void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            var rel = (sender as Button).DataContext as DiscordRelationship;
            var uri = _control.CreateActivationUri("App", "unicord-channel", $"{rel.User.Id}", "", "");
            await _control.ActivateWithUriAsync(uri);
        }
    }
}
