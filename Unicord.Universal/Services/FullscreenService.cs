using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Services
{
    internal class FullscreenService : BaseService<FullscreenService>
    {
        private MainPage _page;
        private Border _canvas;
        private ApplicationView _view;

        private FrameworkElement _fullscreenElement;
        private Border _fullscreenParent;

        public event EventHandler<EventArgs> FullscreenEntered;
        public event EventHandler<EventArgs> FullscreenExited;

        public bool IsFullscreenMode => _view.IsFullScreenMode;

        protected override void Initialise()
        {
            _page = Window.Current.Content.FindChild<MainPage>();
            _canvas = _page?.FindChild<Border>("FullscreenBorder");
            _view = ApplicationView.GetForCurrentView();
            _isInitialised = true;

            var nav = SystemNavigationManager.GetForCurrentView();
            nav.BackRequested += OnBackRequested;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (_canvas.Visibility == Visibility.Visible)
            {
                if (_fullscreenElement != null && _fullscreenParent != null)
                {
                    LeaveFullscreen(_fullscreenElement, _fullscreenParent);
                }
                else
                {
                    LeaveFullscreen();
                }

                e.Handled = true;
            }
        }

        public void EnterFullscreen(FrameworkElement element, Border parent)
        {
            Analytics.TrackEvent("FullscreenService_EnterFullscreen");

            _view.TryEnterFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped | DisplayOrientations.Portrait;

            parent.Child = null;
            _fullscreenElement = element;
            _fullscreenParent = parent;

            _canvas.Visibility = Visibility.Visible;
            _canvas.Child = element;

            FullscreenEntered?.Invoke(this, null);
        }

        public void LeaveFullscreen()
        {
            Analytics.TrackEvent("FullscreenService_LeaveFullscreen");

            _view.ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            _fullscreenElement = null;
            _fullscreenParent = null;

            _canvas.Child = null;
            _canvas.Visibility = Visibility.Collapsed;

            FullscreenExited?.Invoke(this, null);
        }

        public void LeaveFullscreen(FrameworkElement element, Border parent)
        {
            Analytics.TrackEvent("FullscreenService_LeaveFullscreen");

            _canvas.Child = null;
            _canvas.Visibility = Visibility.Collapsed;

            _fullscreenParent.Child = _fullscreenElement;
            _fullscreenElement = null;
            _fullscreenParent = null;

            _view.ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            FullscreenExited?.Invoke(this, null);
        }
    }
}
