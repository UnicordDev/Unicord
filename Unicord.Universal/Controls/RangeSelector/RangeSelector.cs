// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Unicord.Universal.Controls
{
    /// <summary>
    /// RangeSelector is a "double slider" control for range values.
    /// </summary>
    [TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
    [TemplateVisualState(Name = "MinPressed", GroupName = "CommonStates")]
    [TemplateVisualState(Name = "MaxPressed", GroupName = "CommonStates")]
    [TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
    [TemplatePart(Name = "OutOfRangeContentContainer", Type = typeof(Border))]
    [TemplatePart(Name = "ActiveRectangle", Type = typeof(Rectangle))]
    [TemplatePart(Name = "MinThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "MaxThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "CurrentThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "ContainerCanvas", Type = typeof(Canvas))]
    [TemplatePart(Name = "ControlGrid", Type = typeof(Grid))]

    public partial class RangeSelector : Control
    {
        private const double Epsilon = 0.01;
        private const double DefaultMinimum = 0.0;
        private const double DefaultMaximum = 1.0;
        private const double DefaultStepFrequency = 1;

        /// <summary>
        /// Identifies the Minimum dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(RangeSelector), new PropertyMetadata(DefaultMinimum, MinimumChangedCallback));

        /// <summary>
        /// Identifies the Maximum dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(RangeSelector), new PropertyMetadata(DefaultMaximum, MaximumChangedCallback));

        /// <summary>
        /// Identifies the RangeMin dependency property.
        /// </summary>
        public static readonly DependencyProperty RangeMinProperty = DependencyProperty.Register(nameof(RangeMin), typeof(double), typeof(RangeSelector), new PropertyMetadata(DefaultMinimum, RangeMinChangedCallback));

        /// <summary>
        /// Identifies the RangeMax dependency property.
        /// </summary>
        public static readonly DependencyProperty RangeMaxProperty = DependencyProperty.Register(nameof(RangeMax), typeof(double), typeof(RangeSelector), new PropertyMetadata(DefaultMaximum, RangeMaxChangedCallback));



        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private bool _pointerManipulatingValue;

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(RangeSelector), new PropertyMetadata(DefaultMinimum, ValueChangedCallback));



        /// <summary>
        /// Identifies the StepFrequency dependency property.
        /// </summary>
        public static readonly DependencyProperty StepFrequencyProperty = DependencyProperty.Register(nameof(StepFrequency), typeof(double), typeof(RangeSelector), new PropertyMetadata(DefaultStepFrequency));

        private Border _outOfRangeContentContainer;
        private Rectangle _activeRectangle;
        private Thumb _minThumb;
        private Thumb _maxThumb;
        private Thumb _currentThumb;
        private Canvas _containerCanvas;
        private Grid _controlGrid;
        private double _oldValue;
        private bool _valuesAssigned;
        private bool _minSet;
        private bool _maxSet;
        private bool _valueSet;
        private bool _pointerManipulatingMin;
        private bool _pointerManipulatingMax;
        private double _absolutePosition;

        /// <summary>
        /// Event raised when lower or upper range values are changed.
        /// </summary>
        public event EventHandler<RangeChangedEventArgs> RangeChanged;

        /// <summary>
        /// Event raised when lower or upper range values are changed.
        /// </summary>
        public event EventHandler<double> ValueChanged;

        /// <summary>
        /// Event raised when lower or upper range thumbs start being dragged.
        /// </summary>
        public event DragStartedEventHandler ThumbDragStarted;

        /// <summary>
        /// Event raised when lower or upper range thumbs end being dragged.
        /// </summary>
        public event DragCompletedEventHandler ThumbDragCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeSelector"/> class.
        /// Create a default range selector control.
        /// </summary>
        public RangeSelector()
        {
            DefaultStyleKey = typeof(RangeSelector);
        }

        /// <summary>
        /// Update the visual state of the control when its template is changed.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            if (_minThumb != null)
            {
                _minThumb.DragCompleted -= Thumb_DragCompleted;
                _minThumb.DragDelta -= MinThumb_DragDelta;
                _minThumb.DragStarted -= MinThumb_DragStarted;
                _minThumb.KeyDown -= MinThumb_KeyDown;
            }

            if (_currentThumb != null)
            {
                _currentThumb.DragCompleted -= Thumb_DragCompleted;
                _currentThumb.DragDelta -= CurrentThumb_DragDelta;
                _currentThumb.DragStarted -= CurrentThumb_DragStarted;
                _currentThumb.KeyDown -= CurrentThumb_KeyDown;
            }

            if (_maxThumb != null)
            {
                _maxThumb.DragCompleted -= Thumb_DragCompleted;
                _maxThumb.DragDelta -= MaxThumb_DragDelta;
                _maxThumb.DragStarted -= MaxThumb_DragStarted;
                _maxThumb.KeyDown -= MaxThumb_KeyDown;
            }

            if (_containerCanvas != null)
            {
                _containerCanvas.SizeChanged -= ContainerCanvas_SizeChanged;
                _containerCanvas.PointerPressed -= ContainerCanvas_PointerPressed;
                _containerCanvas.PointerMoved -= ContainerCanvas_PointerMoved;
                _containerCanvas.PointerReleased -= ContainerCanvas_PointerReleased;
                _containerCanvas.PointerExited -= ContainerCanvas_PointerExited;
            }

            IsEnabledChanged -= RangeSelector_IsEnabledChanged;

            // Need to make sure the values can be set in XAML and don't overwrite each other
            VerifyValues();
            _valuesAssigned = true;

            _outOfRangeContentContainer = GetTemplateChild("OutOfRangeContentContainer") as Border;
            _activeRectangle = GetTemplateChild("ActiveRectangle") as Rectangle;
            _minThumb = GetTemplateChild("MinThumb") as Thumb;
            _maxThumb = GetTemplateChild("MaxThumb") as Thumb;
            _currentThumb = GetTemplateChild("CurrentThumb") as Thumb;
            _containerCanvas = GetTemplateChild("ContainerCanvas") as Canvas;
            _controlGrid = GetTemplateChild("ControlGrid") as Grid;

            if (_minThumb != null)
            {
                _minThumb.DragCompleted += Thumb_DragCompleted;
                _minThumb.DragDelta += MinThumb_DragDelta;
                _minThumb.DragStarted += MinThumb_DragStarted;
                _minThumb.KeyDown += MinThumb_KeyDown;
            }

            if (_currentThumb != null)
            {
                _currentThumb.DragCompleted += Thumb_DragCompleted;
                _currentThumb.DragDelta += CurrentThumb_DragDelta;
                _currentThumb.DragStarted += CurrentThumb_DragStarted;
                _currentThumb.KeyDown += CurrentThumb_KeyDown;
            }

            if (_maxThumb != null)
            {
                _maxThumb.DragCompleted += Thumb_DragCompleted;
                _maxThumb.DragDelta += MaxThumb_DragDelta;
                _maxThumb.DragStarted += MaxThumb_DragStarted;
                _maxThumb.KeyDown += MaxThumb_KeyDown;
            }

            if (_containerCanvas != null)
            {
                _containerCanvas.SizeChanged += ContainerCanvas_SizeChanged;
                _containerCanvas.PointerEntered += ContainerCanvas_PointerEntered;
                _containerCanvas.PointerPressed += ContainerCanvas_PointerPressed;
                _containerCanvas.PointerMoved += ContainerCanvas_PointerMoved;
                _containerCanvas.PointerReleased += ContainerCanvas_PointerReleased;
                _containerCanvas.PointerExited += ContainerCanvas_PointerExited;
            }

            VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", false);

            IsEnabledChanged += RangeSelector_IsEnabledChanged;

            // Measure our min/max text longest value so we can avoid the length of the scrolling reason shifting in size during use.
            var tb = new TextBlock { Text = Maximum.ToString() };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            base.OnApplyTemplate();
        }

        private void CurrentThumb_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Left:
                    Value -= StepFrequency;
                    SyncThumbs();
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    Value += StepFrequency;
                    SyncThumbs();
                    e.Handled = true;
                    break;
            }
        }

        private void MinThumb_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Left:
                    RangeMin -= StepFrequency;
                    SyncThumbs();
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    RangeMin += StepFrequency;
                    SyncThumbs();
                    e.Handled = true;
                    break;
            }
        }

        private void MaxThumb_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Left:
                    RangeMax -= StepFrequency;
                    SyncThumbs();
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    RangeMax += StepFrequency;
                    SyncThumbs();
                    e.Handled = true;
                    break;
            }
        }

        private void ContainerCanvas_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "PointerOver", false);
        }

        private void ContainerCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint(_containerCanvas).Position.X;
            var normalizedPosition = ((position / DragWidth()) * (Maximum - Minimum)) + Minimum;

            if (_pointerManipulatingMin)
            {
                _pointerManipulatingMin = false;
                _containerCanvas.IsHitTestVisible = true;
                RangeChanged?.Invoke(this, new RangeChangedEventArgs(RangeMin, normalizedPosition, RangeSelectorProperty.MinimumValue));
            }
            else if (_pointerManipulatingMax)
            {
                _pointerManipulatingMax = false;
                _containerCanvas.IsHitTestVisible = true;
                RangeChanged?.Invoke(this, new RangeChangedEventArgs(RangeMax, normalizedPosition, RangeSelectorProperty.MaximumValue));
            }
            else if (_pointerManipulatingValue)
            {
                ValueChanged.Invoke(this, Value);
            }

            VisualStateManager.GoToState(this, "Normal", false);
        }

        private void ContainerCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint(_containerCanvas).Position.X;
            var normalizedPosition = ((position / DragWidth()) * (Maximum - Minimum)) + Minimum;

            if (_pointerManipulatingMin)
            {
                _pointerManipulatingMin = false;
                _containerCanvas.IsHitTestVisible = true;
                RangeChanged?.Invoke(this, new RangeChangedEventArgs(RangeMin, normalizedPosition, RangeSelectorProperty.MinimumValue));
            }
            else if (_pointerManipulatingMax)
            {
                _pointerManipulatingMax = false;
                _containerCanvas.IsHitTestVisible = true;
                RangeChanged?.Invoke(this, new RangeChangedEventArgs(RangeMax, normalizedPosition, RangeSelectorProperty.MaximumValue));
            }
            else if (_pointerManipulatingValue)
            {
                _pointerManipulatingValue = false;
                _containerCanvas.IsHitTestVisible = true;
                ValueChanged.Invoke(this, Value);
            }

            SyncThumbs();
        }

        private void ContainerCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint(_containerCanvas).Position.X;
            var normalizedPosition = ((position / DragWidth()) * (Maximum - Minimum)) + Minimum;

            if (_pointerManipulatingMin && normalizedPosition < RangeMax)
            {
                RangeMin = DragThumb(_minThumb, 0, Canvas.GetLeft(_maxThumb), position);
                RangeChanged?.Invoke(this, new RangeChangedEventArgs(0, RangeMin, RangeSelectorProperty.MinimumValue));
            }
            else if (_pointerManipulatingMax && normalizedPosition > RangeMin)
            {
                RangeMax = DragThumb(_maxThumb, Canvas.GetLeft(_minThumb), DragWidth(), position);
                RangeChanged?.Invoke(this, new RangeChangedEventArgs(0, RangeMax, RangeSelectorProperty.MaximumValue));
            }
            else if (_pointerManipulatingValue && normalizedPosition > RangeMin && normalizedPosition < RangeMax)
            {
                Value = DragThumb(_currentThumb, Canvas.GetLeft(_minThumb), Canvas.GetLeft(_maxThumb), position);
                ValueChanged?.Invoke(this, Value);
            }
        }

        private void ContainerCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint(_containerCanvas).Position.X;
            var normalizedPosition = position * Math.Abs(Maximum - Minimum) / DragWidth();
            var upperValueDiff = Math.Abs(RangeMax - normalizedPosition);
            var lowerValueDiff = Math.Abs(RangeMin - normalizedPosition);

            if (upperValueDiff < lowerValueDiff)
            {
                RangeMax = normalizedPosition;
                _pointerManipulatingMax = true;
                Thumb_DragStarted(_maxThumb);
            }
            else
            {
                RangeMin = normalizedPosition;
                _pointerManipulatingMin = true;
                Thumb_DragStarted(_minThumb);
            }

            SyncThumbs();
        }

        private void ContainerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncThumbs();
        }

        private void VerifyValues()
        {
            if (Minimum > Maximum)
            {
                Minimum = Maximum;
                Maximum = Maximum;
            }

            if (Minimum == Maximum)
            {
                Maximum += Epsilon;
            }

            if (!_maxSet)
            {
                RangeMax = Maximum;
            }

            if (!_valueSet)
            {
                Value = Minimum;
            }

            if (!_minSet)
            {
                RangeMin = Minimum;
            }

            Minimum = Minimum.Clamp(Minimum, RangeMax);
            Maximum = Maximum.Clamp(RangeMin, Maximum);
            Value = Value.Clamp(RangeMin, RangeMax);
        }

        /// <summary>
        /// Gets or sets the minimum value of the range.
        /// </summary>
        /// <value>
        /// The minimum.
        /// </value>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);

            set => SetValue(MinimumProperty, value);
        }

        private static void MinimumChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (!(d is RangeSelector rangeSelector) || !rangeSelector._valuesAssigned)
            {
                return;
            }

            var newValue = (double)e.NewValue;
            var oldValue = (double)e.OldValue;

            if (rangeSelector.Maximum < newValue)
            {
                rangeSelector.Maximum = newValue + Epsilon;
            }

            if (rangeSelector.RangeMin < newValue)
            {
                rangeSelector.RangeMin = newValue;
            }

            if (rangeSelector.RangeMax < newValue)
            {
                rangeSelector.RangeMax = newValue;
            }

            if (rangeSelector.Value < newValue)
            {
                rangeSelector.Value = newValue;
            }

            if (newValue != oldValue)
            {
                rangeSelector.SyncThumbs();
            }
        }

        /// <summary>
        /// Gets or sets the maximum value of the range.
        /// </summary>
        /// <value>
        /// The maximum.
        /// </value>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);

            set => SetValue(MaximumProperty, value);
        }

        private static void MaximumChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (!(d is RangeSelector rangeSelector) || !rangeSelector._valuesAssigned)
            {
                return;
            }

            var newValue = (double)e.NewValue;
            var oldValue = (double)e.OldValue;

            if (rangeSelector.Minimum > newValue)
            {
                rangeSelector.Minimum = newValue - Epsilon;
            }

            if (rangeSelector.RangeMax > newValue)
            {
                rangeSelector.RangeMax = newValue;
            }

            if (rangeSelector.RangeMin > newValue)
            {
                rangeSelector.RangeMin = newValue;
            }

            if (rangeSelector.Value > newValue)
            {
                rangeSelector.Value = newValue;
            }

            if (newValue != oldValue)
            {
                rangeSelector.SyncThumbs();
            }
        }

        /// <summary>
        /// Gets or sets the current lower limit value of the range.
        /// </summary>
        /// <value>
        /// The current lower limit.
        /// </value>
        public double RangeMin
        {
            get => (double)GetValue(RangeMinProperty);

            set => SetValue(RangeMinProperty, value);
        }

        private static void RangeMinChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (!(d is RangeSelector rangeSelector))
            {
                return;
            }

            rangeSelector._minSet = true;

            if (!rangeSelector._valuesAssigned)
            {
                return;
            }

            var newValue = (double)e.NewValue;
            rangeSelector.RangeMinToStepFrequency();

            if (rangeSelector._valuesAssigned)
            {
                if (newValue < rangeSelector.Minimum)
                {
                    rangeSelector.RangeMin = rangeSelector.Minimum;
                }
                else if (newValue > rangeSelector.Maximum)
                {
                    rangeSelector.RangeMin = rangeSelector.Maximum;
                }

                rangeSelector.SyncActiveRectangle();

                // If the new value is greater than the old max, move the max also
                if (newValue > rangeSelector.RangeMax)
                {
                    rangeSelector.RangeMax = newValue;
                }
                if (newValue > rangeSelector.Value)
                {
                    rangeSelector.Value = newValue;
                }
            }

            rangeSelector.SyncThumbs();
        }

        /// <summary>
        /// Gets or sets the current upper limit value of the range.
        /// </summary>
        /// <value>
        /// The current upper limit.
        /// </value>
        public double RangeMax
        {
            get => (double)GetValue(RangeMaxProperty);

            set => SetValue(RangeMaxProperty, value);
        }

        private static void ValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (!(d is RangeSelector rangeSelector))
            {
                return;
            }

            rangeSelector._valueSet = true;

            if (!rangeSelector._valuesAssigned)
            {
                return;
            }

            var newValue = (double)e.NewValue;

            if (rangeSelector._valuesAssigned)
            {
                if (newValue < rangeSelector.Minimum)
                {
                    rangeSelector.Value = rangeSelector.Minimum;
                }
                else if (newValue > rangeSelector.Maximum)
                {
                    rangeSelector.Value = rangeSelector.Maximum;
                }

                rangeSelector.SyncActiveRectangle();
            }

            rangeSelector.SyncThumbs();
        }

        private static void RangeMaxChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (!(d is RangeSelector rangeSelector))
            {
                return;
            }

            rangeSelector._maxSet = true;

            if (!rangeSelector._valuesAssigned)
            {
                return;
            }

            var newValue = (double)e.NewValue;
            rangeSelector.RangeMaxToStepFrequency();

            if (rangeSelector._valuesAssigned)
            {
                if (newValue < rangeSelector.Minimum)
                {
                    rangeSelector.RangeMax = rangeSelector.Minimum;
                }
                else if (newValue > rangeSelector.Maximum)
                {
                    rangeSelector.RangeMax = rangeSelector.Maximum;
                }

                rangeSelector.SyncActiveRectangle();

                // If the new max is less than the old minimum then move the minimum
                if (newValue < rangeSelector.RangeMin)
                {
                    rangeSelector.RangeMin = newValue;
                }

                if (newValue < rangeSelector.Value)
                {
                    rangeSelector.Value = newValue;
                }
            }

            rangeSelector.SyncThumbs();
        }

        /// <summary>
        /// Gets or sets the value part of a value range that steps should be created for.
        /// </summary>
        /// <value>
        /// The value part of a value range that steps should be created for.
        /// </value>
        public double StepFrequency
        {
            get => (double)GetValue(StepFrequencyProperty);

            set => SetValue(StepFrequencyProperty, value);
        }

        private void RangeMinToStepFrequency()
        {
            RangeMin = MoveToStepFrequency(RangeMin);
        }

        private void RangeMaxToStepFrequency()
        {
            RangeMax = MoveToStepFrequency(RangeMax);
        }

        private double MoveToStepFrequency(double rangeValue)
        {
            var newValue = Minimum + (((int)Math.Round((rangeValue - Minimum) / StepFrequency)) * StepFrequency);

            if (newValue < Minimum)
            {
                return Minimum;
            }
            else if (newValue > Maximum || Maximum - newValue < StepFrequency)
            {
                return Maximum;
            }
            else
            {
                return newValue;
            }
        }

        private void SyncThumbs()
        {
            if (_containerCanvas == null)
            {
                return;
            }

            var relativeLeft = ((RangeMin - Minimum) / (Maximum - Minimum)) * DragWidth();
            var relativeRight = ((RangeMax - Minimum) / (Maximum - Minimum)) * DragWidth();
            var relativeValue = ((Value) / (Maximum - Minimum)) * DragWidth();

            Canvas.SetLeft(_minThumb, relativeLeft);
            Canvas.SetLeft(_currentThumb, relativeValue);
            Canvas.SetLeft(_maxThumb, relativeRight);

            SyncActiveRectangle();
        }

        private void SyncActiveRectangle()
        {
            if (_containerCanvas == null)
            {
                return;
            }

            if (_minThumb == null)
            {
                return;
            }

            if (_maxThumb == null)
            {
                return;
            }

            var relativeLeft = Canvas.GetLeft(_minThumb);
            Canvas.SetLeft(_activeRectangle, relativeLeft);
            Canvas.SetTop(_activeRectangle, (_containerCanvas.ActualHeight - _activeRectangle.ActualHeight) / 2);
            _activeRectangle.Width = Math.Max(0, Canvas.GetLeft(_maxThumb) - Canvas.GetLeft(_minThumb));
        }

        private double DragWidth()
        {
            return _containerCanvas.ActualWidth - _maxThumb.Width;
        }

        private void CurrentThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _absolutePosition += e.HorizontalChange;

            Value = DragThumb(_currentThumb, Canvas.GetLeft(_minThumb), Canvas.GetLeft(_maxThumb), _absolutePosition);
            ValueChanged?.Invoke(this, Value);
        }

        private void MinThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _absolutePosition += e.HorizontalChange;

            RangeMin = DragThumb(_minThumb, 0, Canvas.GetLeft(_maxThumb), _absolutePosition);
            RangeChanged?.Invoke(this, new RangeChangedEventArgs(0, RangeMin, RangeSelectorProperty.MinimumValue));

        }

        private void MaxThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _absolutePosition += e.HorizontalChange;

            RangeMax = DragThumb(_maxThumb, Canvas.GetLeft(_minThumb), DragWidth(), _absolutePosition);
            RangeChanged?.Invoke(this, new RangeChangedEventArgs(0, RangeMax, RangeSelectorProperty.MaximumValue));
        }

        private double DragThumb(Thumb thumb, double min, double max, double nextPos)
        {
            nextPos = Math.Max(min, nextPos);
            nextPos = Math.Min(max, nextPos);

            Canvas.SetLeft(thumb, nextPos);

            return Minimum + ((nextPos / DragWidth()) * (Maximum - Minimum));
        }

        private void Thumb_DragStarted(Thumb thumb)
        {
            _absolutePosition = Canvas.GetLeft(thumb);
            Canvas.SetZIndex(thumb, 10);

            if (thumb == _minThumb)
            {
                VisualStateManager.GoToState(this, "MinPressed", true);
                Canvas.SetZIndex(_maxThumb, 0);
                Canvas.SetZIndex(_currentThumb, 5);
            }
            else if (thumb == _maxThumb)
            {
                VisualStateManager.GoToState(this, "MaxPressed", true);
                Canvas.SetZIndex(_minThumb, 0);
                Canvas.SetZIndex(_currentThumb, 5);
            }
            else
            {
                VisualStateManager.GoToState(this, "CurrentPressed", true);
                Canvas.SetZIndex(_minThumb, 0);
                Canvas.SetZIndex(_maxThumb, 5);
            }

            _oldValue = RangeMin;
        }

        private void MinThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            ThumbDragStarted?.Invoke(this, e);
            Thumb_DragStarted(_minThumb);
        }

        private void MaxThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            ThumbDragStarted?.Invoke(this, e);
            Thumb_DragStarted(_maxThumb);
        }

        private void CurrentThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            ThumbDragStarted?.Invoke(this, e);
            Thumb_DragStarted(_currentThumb);
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            ThumbDragCompleted?.Invoke(this, e);

            if (sender != _currentThumb)
            {
                RangeChanged?.Invoke(this, sender.Equals(_minThumb) ? new RangeChangedEventArgs(_oldValue, RangeMin, RangeSelectorProperty.MinimumValue) : new RangeChangedEventArgs(_oldValue, RangeMax, RangeSelectorProperty.MaximumValue));
            }
            else
            {
                ValueChanged?.Invoke(this, Value);
            }

            SyncThumbs();

            VisualStateManager.GoToState(this, "Normal", true);
        }

        private void RangeSelector_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", true);
        }
    }
}
