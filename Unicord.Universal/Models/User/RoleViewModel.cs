using DSharpPlus.Entities;
using Unicord.Universal.Extensions;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Models.User
{
    public class RoleViewModel : ViewModelBase
    {
        private DiscordRole role;

        public RoleViewModel(DiscordRole role, ViewModelBase parent)
            : base(parent)
        {
            this.role = role;
        }

        public string Name
            => this.role.Name;
        public SolidColorBrush Color
            =>  role.Color.Value != 0 ?
                new SolidColorBrush(role.Color.ToColor())
            : App.Current.Resources["DefaultTextForegroundThemeBrush"] as SolidColorBrush; // FallbackValue just doesn't work lol
    }
}
