using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
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

        public async Task ShowOverlayAsync<T>(object model = null) where T : Page, IOverlay, new()
        {
            if (App.LocalSettings.Read("WindowedOverlays", false))
            {
                var view = CoreApplication.CreateNewView();
                var newViewId = 0;
                var currentViewId = ApplicationView.GetForCurrentView().Id;
                var t = default(T);
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ThemeManager.LoadCurrentTheme(App.Current.Resources);

                    var frame = new Frame();
                    frame.Navigate(typeof(T), model);

                    SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += (o, e) =>
                    {
                        frame.Navigate(typeof(Page), null);
                    };

                    Window.Current.SizeChanged += (o, e) =>
                    {
                        App.LocalSettings.Save($"{typeof(T).Name}_SavedWindowSize", e.Size);
                    };

                    Window.Current.Content = frame;
                    Window.Current.Activate();

                    t = (T)frame.Content;
                    newViewId = ApplicationView.GetForCurrentView().Id;
                });

                var size = App.LocalSettings.Read($"{typeof(T).Name}_SavedWindowSize", t.PreferredSize);
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
            }
            else
            {
                if (_overlayVisible)
                    throw new InvalidOperationException("Can't show an overlay when one is already visible!");

                _overlayVisible = true;
                _systemNavigationManager.BackRequested += OnBackRequested;
                _mainPage.CustomFrame.Navigate(typeof(T), model, new SuppressNavigationTransitionInfo());
                _mainPage.ShowCustomOverlay();
            }
        }

        internal void CloseOverlay()
        {
            if (App.LocalSettings.Read("WindowedOverlays", false))
            {
                Window.Current.Close();
            }
            else
            {
                if (!_overlayVisible)
                    return;

                _systemNavigationManager.BackRequested -= OnBackRequested;
                _mainPage.HideCustomOverlay();
                _overlayVisible = false;
            }
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

        Size PreferredSize { get; }
    }
}
