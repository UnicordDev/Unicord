using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Services
{
    internal class FullscreenService : BaseService<FullscreenService>
    {
        private record CapturedProperties(FrameworkElement Control)
        {
            public readonly double Width = Control.Width;
            public readonly double Height = Control.Height;
            public readonly double MaxWidth = Control.MaxWidth;
            public readonly double MaxHeight = Control.MaxHeight;
            public readonly Transform RenderTransform = Control.RenderTransform;
            public readonly Point RenderTransformOrigin = Control.RenderTransformOrigin;

            public void Apply(FrameworkElement target)
            {
                target.Width = Width;
                target.Height = Height;
                target.MaxWidth = MaxWidth;
                target.MaxHeight = MaxHeight;
                target.RenderTransform = RenderTransform;
                target.RenderTransformOrigin = RenderTransformOrigin;
            }
        }

        private MainPage _page;
        private Canvas _canvas;
        private ApplicationView _view;

        private CapturedProperties _fullscreenProperties;
        private FrameworkElement _fullscreenElement;
        private Border _fullscreenParent;

        public event EventHandler<EventArgs> FullscreenEntered;
        public event EventHandler<EventArgs> FullscreenExited;

        public bool IsFullscreenMode => _view.IsFullScreenMode;

        protected override void Initialise()
        {
            _page = Window.Current.Content.FindChild<MainPage>();
            _canvas = _page?.FindChild<Canvas>("FullscreenBorder");
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
                    LeaveFullscreenAsync(_fullscreenElement, _fullscreenParent);
                }
                else
                {
                    LeaveFullscreen();
                }

                e.Handled = true;
            }
        }

        public async Task EnterFullscreenAsync(FrameworkElement element, Border parent)
        {
            Analytics.TrackEvent("FullscreenService_EnterFullscreen");

            LeaveFullscreen();

            var tcs = new TaskCompletionSource<object>();
            var maxWidth = Window.Current.Bounds.Width;
            var maxHeight = Window.Current.Bounds.Height;

            if (_view.TryEnterFullScreenMode())
            {
                var info = DisplayInformation.GetForCurrentView();
                maxWidth = info.ScreenWidthInRawPixels / info.RawPixelsPerViewPixel;
                maxHeight = info.ScreenHeightInRawPixels / info.RawPixelsPerViewPixel;
            }

            _fullscreenProperties = new CapturedProperties(element);

            var placeholder = new Border();
            _fullscreenProperties.Apply(placeholder);

            var bounds = parent.TransformToVisual(null)
                               .TransformBounds(new Rect(0, 0, parent.ActualWidth, parent.ActualHeight));

            element.MaxWidth = double.PositiveInfinity;
            element.MaxHeight = double.PositiveInfinity;

            Canvas.SetLeft(element, 0);
            Canvas.SetTop(element, 0);
            element.Width = maxWidth;
            element.Height = maxHeight;

            var ratioX = (double)maxWidth / bounds.Width;
            var ratioY = (double)maxHeight / bounds.Height;

            var transformCollection = new CompositeTransform
            {
                TranslateX = (bounds.Left + (bounds.Width / 2)) - (maxWidth / 2),
                TranslateY = (bounds.Top + (bounds.Height / 2)) - (maxHeight / 2),
                ScaleX = bounds.Width / maxWidth,
                ScaleY = bounds.Height / maxHeight
            };

            VisualTreeHelper.DisconnectChildrenRecursive(parent);
            VisualTreeHelper.DisconnectChildrenRecursive(_canvas);

            _fullscreenElement = element;
            _fullscreenParent = parent;
            _canvas.Children.Add(element);
            parent.Child = placeholder;

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = transformCollection;

            var storyboard = new Storyboard();
            AnimateDouble(transformCollection, storyboard, 1, "ScaleX");
            AnimateDouble(transformCollection, storyboard, 1, "ScaleY");
            AnimateDouble(transformCollection, storyboard, 0, "TranslateX");
            AnimateDouble(transformCollection, storyboard, 0, "TranslateY");

            storyboard.Completed += (o, ev) =>
            {
                tcs.SetResult(null);
                FullscreenEntered?.Invoke(this, null);
            };

            storyboard.Begin();

            _canvas.Visibility = Visibility.Visible;
            await tcs.Task;
        }

        public void LeaveFullscreen()
        {
            Analytics.TrackEvent("FullscreenService_LeaveFullscreen");

            _view.ExitFullScreenMode();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            _fullscreenElement = null;
            _fullscreenParent = null;

            VisualTreeHelper.DisconnectChildrenRecursive(_canvas);
            _canvas.Visibility = Visibility.Collapsed;

            FullscreenExited?.Invoke(this, null);
        }

        public Task LeaveFullscreenAsync(FrameworkElement element, Border parent)
        {
            var tcs = new TaskCompletionSource<object>();

            Analytics.TrackEvent("FullscreenService_LeaveFullscreen");

            var bounds = parent.TransformToVisual(null)
                               .TransformBounds(new Rect(0, 0, parent.ActualWidth, parent.ActualHeight));
            var ratioX = (double)element.ActualWidth / bounds.Width;
            var ratioY = (double)element.ActualHeight / bounds.Height;

            var transformCollection = new CompositeTransform();

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = transformCollection;

            var storyboard = new Storyboard();
            AnimateDouble(transformCollection, storyboard, bounds.Width / element.ActualWidth, "ScaleX");
            AnimateDouble(transformCollection, storyboard, bounds.Height / element.ActualHeight, "ScaleY");
            AnimateDouble(transformCollection, storyboard, (bounds.Left + (bounds.Width / 2)) - (element.ActualWidth / 2), "TranslateX");
            AnimateDouble(transformCollection, storyboard, (bounds.Top + (bounds.Height / 2)) - (element.ActualHeight / 2), "TranslateY");

            storyboard.Begin();
            storyboard.Completed += (o, ev) =>
            {
                _view.ExitFullScreenMode();

                _canvas.Children.Clear();
                _fullscreenParent.Child = null;

                if (_fullscreenElement is not MediaPlayerElement)
                {
                    VisualTreeHelper.DisconnectChildrenRecursive(_fullscreenParent);
                    VisualTreeHelper.DisconnectChildrenRecursive(_canvas);
                }

                _canvas.Visibility = Visibility.Collapsed;
                _fullscreenParent.Child = _fullscreenElement;
                _fullscreenProperties.Apply(_fullscreenElement);

                _fullscreenProperties = null;
                _fullscreenElement = null;
                _fullscreenParent = null;

                tcs.SetResult(null);
                FullscreenExited?.Invoke(this, null);
            };

            return tcs.Task;
        }

        private static void AnimateDouble(CompositeTransform transformCollection, Storyboard storyboard, double to, string property)
        {
            var scaleXAnimation = new DoubleAnimation()
            {
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(1.0 / 3.0)),
                EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(scaleXAnimation, transformCollection);
            Storyboard.SetTargetProperty(scaleXAnimation, property);
            storyboard.Children.Add(scaleXAnimation);
        }
    }
}
