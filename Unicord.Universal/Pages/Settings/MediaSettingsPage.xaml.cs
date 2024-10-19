using System.Linq;
using Unicord.Universal.Models;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Pages.Settings
{
    public sealed partial class MediaSettingsPage : Page
    {
        public MediaSettingsPage()
        {
            InitializeComponent();
            DataContext = new MediaSettingsModel();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MediaSettingsModel model && e.AddedItems.FirstOrDefault() is string str)
            {
                model.Resolution = str;
            }
        }
    }
}
