using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Services
{
    internal class OverlayService : BaseService<OverlayService>
    {
        private MainPage _mainPage;
        private SystemNavigationManager _systemNavigationManager;
        private bool _overlayVisible;

        protected override void Initialise()
        {
            _mainPage = Window.Current.Content.FindChild<MainPage>();
            _systemNavigationManager = SystemNavigationManager.GetForCurrentView();
        }

        public void ShowOverlay<T>(object model = null) where T : Page, IOverlay, new()
        {
            if (_overlayVisible)
                throw new InvalidOperationException("Can't show an overlay when one is already visible!");

            _overlayVisible = true;
            _systemNavigationManager.BackRequested += OnBackRequested;

            _mainPage.CustomFrame.Navigate(typeof(T), model, new SuppressNavigationTransitionInfo());
            _mainPage.ShowCustomOverlay();
        }

        internal void CloseOverlay()
        {
            if (!_overlayVisible)
                return;

            _systemNavigationManager.BackRequested -= OnBackRequested;
            _mainPage.HideCustomOverlay();
            _overlayVisible = false;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            CloseOverlay();
        }
    }

    internal interface IOverlay
    {
        object DataContext { get; set; }
        double MaxWidth { get; set; }
        double MaxHeight { get; set; }
    }
}
