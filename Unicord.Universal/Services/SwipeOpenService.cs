using Unicord.Universal.Utilities;
using Windows.UI.Xaml;


namespace Unicord.Universal.Services
{
    internal class SwipeOpenService : BaseService<SwipeOpenService>
    {
        internal SwipeOpenHelper Helper { get; set; }

        protected override void Initialise()
        {
            //Helper = Window.Current.Content.FindChild<DiscordPage>()?._helper;
        }

        public void AddAdditionalElement(UIElement element) => Helper?.AddAdditionalElement(element);
    }
}
