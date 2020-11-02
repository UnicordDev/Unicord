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
        private Grid _canvas;
        private ApplicationView _view;

        private FrameworkElement _fullscreenElement;
        private Panel _fullscreenParent;

        public event EventHandler<EventArgs> FullscreenEntered;
        public event EventHandler<EventArgs> FullscreenExited;

        public bool IsFullscreenMode => _view.IsFullScreenMode;

        protected override void Initialise()
        {
            _page = Window.Current.Content.FindChild<MainPage>();
            _canvas = _page?.FindChild<Grid>("fullscreenCanvas");
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

        public void EnterFullscreen(FrameworkElement element, Panel parent)
        {
            Analytics.TrackEvent("FullscreenService_EnterFullscreen");

            _view.TryEnterFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped | DisplayOrientations.Portrait;

            parent.Children.Remove(element);
            _fullscreenElement = element;
            _fullscreenParent = parent;

            _canvas.Visibility = Visibility.Visible;
            _canvas.Children.Add(element);

            element.Width = double.NaN;
            element.Height = double.NaN;

            FullscreenEntered?.Invoke(this, null);
        }

        public void LeaveFullscreen()
        {
            Analytics.TrackEvent("FullscreenService_LeaveFullscreen");

            _view.ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            _fullscreenElement = null;
            _fullscreenParent = null;

            _canvas.Children.Clear();
            _canvas.Visibility = Visibility.Collapsed;

            FullscreenExited?.Invoke(this, null);
        }

        public void LeaveFullscreen(FrameworkElement element, Panel parent)
        {
            Analytics.TrackEvent("FullscreenService_LeaveFullscreen");

            _view.ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            _canvas.Children.Remove(element);
            parent.Children.Insert(0, element);
            element.Width = double.NaN;
            element.Height = double.NaN;

            _fullscreenElement = null;
            _fullscreenParent = null;

            _canvas.Visibility = Visibility.Collapsed;

            FullscreenExited?.Invoke(this, null);
        }
    }
}
