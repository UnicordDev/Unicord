using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Unicord.Universal.Utilities
{
    internal class SwipeOpenHelper
    {
        private GestureRecognizer _recogniser;
        private UIElement _target;
        private UIElement _reference;
        private TranslateTransform _transform;
        private Storyboard _openAnimation;
        private Storyboard _closeAnimation;
        private double _xTotal = 0.0;

        public bool IsEnabled { get; set; }

        public SwipeOpenHelper(UIElement target, UIElement reference, Storyboard openAnimation, Storyboard closeAnimation)
        {
            _recogniser = new GestureRecognizer() { GestureSettings = GestureSettings.ManipulationTranslateX };
            _target = target;
            _reference = reference;
            _transform = _target.RenderTransform as TranslateTransform;
            _openAnimation = openAnimation;
            _closeAnimation = closeAnimation;

            target.PointerPressed += OnPointerPressed;
            target.PointerMoved += OnPointerMoved;
            target.PointerReleased += OnPointerReleased;
            target.PointerCanceled += OnPointerCanceled;

            _recogniser.ManipulationStarted += OnManipulationStarted;
            _recogniser.ManipulationUpdated += OnManipulationUpdated;
            _recogniser.ManipulationCompleted += OnManipulationCompleted;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled)
                return;

            _target.CapturePointer(e.Pointer);
            _recogniser.ProcessDownEvent(e.GetCurrentPoint(_target));
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled)
                return;

            _recogniser.ProcessMoveEvents(e.GetIntermediatePoints(_reference));
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled)
                return;

            _recogniser.ProcessUpEvent(e.GetCurrentPoint(_target));
            _target.ReleasePointerCapture(e.Pointer);
        }

        private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled)
                return;

            _recogniser.CompleteGesture();
            _target.ReleasePointerCapture(e.Pointer);
        }

        private void OnManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
            if (!IsEnabled)
                return;

            _xTotal = _transform.X;
        }

        private void OnManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            if (!IsEnabled)
                return;

            _xTotal += args.Delta.Translation.X;

            if (_xTotal > 16)
            {
                var delta = Math.Max(Math.Abs(276 - _xTotal), 64) / 64d;
                _transform.X = Math.Min(_xTotal, 276);
            }
        }

        private void OnManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            if (!IsEnabled)
                return;

            if (_xTotal > 128)
            {
                _openAnimation.Begin();
            }
            else
            {
                _closeAnimation.Begin();
            }

            _xTotal = 0;
        }
    }
}
