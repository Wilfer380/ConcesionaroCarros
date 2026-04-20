using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace ConcesionaroCarros.Controls
{
    public partial class ThemeModeSlider : UserControl
    {
        private const double KnobHorizontalMargin = 10d;
        private const double DragCommitThreshold = 0.5d;

        private bool _isDragging;
        private bool _suppressAnimation;
        private bool _initialLayoutApplied;
        private double _dragStartMouseX;
        private double _dragStartOffset;
        private double _currentOffset;

        public static readonly DependencyProperty IsDarkThemeSelectedProperty =
            DependencyProperty.Register(
                nameof(IsDarkThemeSelected),
                typeof(bool),
                typeof(ThemeModeSlider),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDarkThemeSelectedChanged));

        public static readonly DependencyProperty LightLabelProperty =
            DependencyProperty.Register(nameof(LightLabel), typeof(string), typeof(ThemeModeSlider), new PropertyMetadata("Light", OnLabelChanged));

        public static readonly DependencyProperty DarkLabelProperty =
            DependencyProperty.Register(nameof(DarkLabel), typeof(string), typeof(ThemeModeSlider), new PropertyMetadata("Dark", OnLabelChanged));

        public ThemeModeSlider()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        public bool IsDarkThemeSelected
        {
            get => (bool)GetValue(IsDarkThemeSelectedProperty);
            set => SetValue(IsDarkThemeSelectedProperty, value);
        }

        public string LightLabel
        {
            get => (string)GetValue(LightLabelProperty);
            set => SetValue(LightLabelProperty, value);
        }

        public string DarkLabel
        {
            get => (string)GetValue(DarkLabelProperty);
            set => SetValue(DarkLabelProperty, value);
        }

        private static void OnIsDarkThemeSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ThemeModeSlider)d;
            if (control._isDragging)
                return;

            // If layout isn't ready yet (ActualWidth = 0), applying the state will place the knob at 0.
            // Defer once to the layout/render pass so the knob lands correctly on first navigation to Configuración.
            if (!control.IsLoaded || !control.IsLayoutReadyForOffsets())
            {
                control.Dispatcher.BeginInvoke(new Action(() =>
                {
                    control.ApplySelectionState(false);
                    control.FinalizeInitialLayoutIfNeeded();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            control.ApplySelectionState(!control._suppressAnimation);
            control.StartGestureHintAnimation();
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ThemeModeSlider)d).UpdateKnobText();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // IMPORTANT: first time this control appears, Track/Knob widths can still be 0.
            // If we compute offsets too early, the knob stays on the left even when dark theme is selected.
            _suppressAnimation = true;
            _initialLayoutApplied = false;

            // Apply what we can without relying on ActualWidth.
            UpdateVisualState();

            if (!TryApplyInitialLayoutDependentState())
            {
                LayoutUpdated += OnLayoutUpdatedApplyInitial;
                return;
            }

            FinalizeInitialLayoutIfNeeded();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isDragging)
                return;

            ApplySelectionState(false);
            StartGestureHintAnimation(); // recompute distance when the control size changes
        }

        private void OnLayoutUpdatedApplyInitial(object sender, EventArgs e)
        {
            if (!TryApplyInitialLayoutDependentState())
                return;

            LayoutUpdated -= OnLayoutUpdatedApplyInitial;
            FinalizeInitialLayoutIfNeeded();
        }

        private bool TryApplyInitialLayoutDependentState()
        {
            if (_initialLayoutApplied)
                return true;

            if (!IsLayoutReadyForOffsets())
                return false;

            // Now widths are valid -> knob lands in the correct place for the current theme.
            ApplySelectionState(false);
            _initialLayoutApplied = true;
            return true;
        }

        private bool IsLayoutReadyForOffsets()
        {
            return TrackBorder != null
                   && Knob != null
                   && TrackBorder.ActualWidth > 0d
                   && Knob.ActualWidth > 0d;
        }

        private void FinalizeInitialLayoutIfNeeded()
        {
            if (!_suppressAnimation)
                return;

            _suppressAnimation = false;
            UpdateSunVisualState();
            UpdateMoonVisualState();
            StartGestureHintAnimation();
        }

        private void OnTrackMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, Knob) || IsKnobDescendant(e.OriginalSource as DependencyObject))
                return;

            var clickPosition = e.GetPosition(TrackBorder).X;
            var selectDark = clickPosition >= TrackBorder.ActualWidth / 2d;
            CommitSelection(selectDark, true);
            e.Handled = true;
        }

        private void OnKnobMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartMouseX = e.GetPosition(this).X;
            _dragStartOffset = _currentOffset;
            Knob.CaptureMouse();
            KnobTransform.BeginAnimation(TranslateTransform.XProperty, null);
            UpdateSunVisualState();
            UpdateMoonVisualState();
            StopGestureHintAnimation();
            e.Handled = true;
        }

        private void OnRootPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            var currentPosition = e.GetPosition(this).X;
            var requestedOffset = _dragStartOffset + (currentPosition - _dragStartMouseX);
            SetKnobOffset(requestedOffset);
            UpdateSelectionDuringDrag();
        }

        private void OnRootPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging)
                return;

            ReleaseDrag();
            e.Handled = true;
        }

        private void OnRootLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_isDragging)
                ReleaseDrag();
        }

        private void ReleaseDrag()
        {
            var maxOffset = GetMaxOffset();
            var normalizedOffset = maxOffset <= 0d ? 0d : _currentOffset / maxOffset;
            var selectDark = normalizedOffset >= DragCommitThreshold;
            var selectionUnchanged = IsDarkThemeSelected == selectDark;

            _isDragging = false;
            if (Knob.IsMouseCaptured)
                Knob.ReleaseMouseCapture();

            CommitSelection(selectDark, true);

            // If the selection didn't change, OnIsDarkThemeSelectedChanged won't fire, so we must restart the hint here.
            if (selectionUnchanged)
                StartGestureHintAnimation();
        }

        private void StartGestureHintAnimation()
        {
            if (GestureGuideHandTranslate == null || GestureGuideHandScale == null)
                return;

            // The overlay stays fixed; we animate ONLY the internal hand direction based on theme state.
            // User request: one direction only (no ping-pong). Dark -> move left. Light -> move right.
            var distance = ComputeGestureHintDistance();
            // IMPORTANT: In dark mode we mirror the hand with ScaleX=-1 (XAML). That mirroring also flips the
            // visual direction of X translation. So we always animate to +distance:
            // - Light (not mirrored) => +X looks like moving right
            // - Dark (mirrored)      => +X looks like moving left
            var toX = distance;

            GestureGuideHandTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            GestureGuideHandScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            GestureGuideHandScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            GestureGuideHandTranslate.X = 0d;
            GestureGuideHandScale.ScaleX = 1d;
            GestureGuideHandScale.ScaleY = 1d;

            var move = CreateOneWayLoopAnimation(toX);

            var scale = new DoubleAnimation
            {
                From = 0.98d,
                To = 1.02d,
                Duration = TimeSpan.FromMilliseconds(550),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            GestureGuideHandTranslate.BeginAnimation(TranslateTransform.XProperty, move, HandoffBehavior.SnapshotAndReplace);
            GestureGuideHandScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale, HandoffBehavior.SnapshotAndReplace);
            GestureGuideHandScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale, HandoffBehavior.SnapshotAndReplace);
        }

        private double ComputeGestureHintDistance()
        {
            // We want a "long" movement roughly along the slider track, but without leaving the overlay bounds.
            var width = TrackBorder?.ActualWidth ?? GestureGuideHandOverlay?.ActualWidth ?? 0d;
            if (double.IsNaN(width) || width <= 0d)
                width = 520d; // safe fallback: matches the typical track size in this control

            // Start at center; travel near the edge, leaving some padding so it doesn't clip.
            var half = width / 2d;
            return Math.Max(90d, half - 90d);
        }

        private static DoubleAnimationUsingKeyFrames CreateOneWayLoopAnimation(double toX)
        {
            var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

            // Loop behavior:
            // - stay at 0 briefly
            // - move to toX
            // - jump back to 0 (discrete)
            // - repeat forever
            var anim = new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            // Slower, more readable guidance motion (user request).
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0d, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0d, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(350))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(toX, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1550))) { EasingFunction = easing });
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0d, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1850))));
            return anim;
        }

        private void StopGestureHintAnimation()
        {
            if (GestureGuideHandTranslate == null || GestureGuideHandScale == null)
                return;

            GestureGuideHandTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            GestureGuideHandScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            GestureGuideHandScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            GestureGuideHandTranslate.X = 0d;
            GestureGuideHandScale.ScaleX = 1d;
            GestureGuideHandScale.ScaleY = 1d;
        }

        private void CommitSelection(bool selectDark, bool animate)
        {
            if (IsDarkThemeSelected == selectDark)
            {
                ApplySelectionState(animate);
                // When the user dragged but didn't cross the threshold, the hint was stopped on mouse-down.
                // Restart it so it keeps guiding after the knob is already positioned.
                if (!_isDragging)
                    StartGestureHintAnimation();
                return;
            }

            _suppressAnimation = !animate;
            SetCurrentValue(IsDarkThemeSelectedProperty, selectDark);
            _suppressAnimation = false;
        }

        private void ApplySelectionState(bool animate)
        {
            UpdateVisualState();
            AnimateKnobTo(IsDarkThemeSelected ? GetMaxOffset() : 0d, animate);
        }

        private void AnimateKnobTo(double targetOffset, bool animate)
        {
            targetOffset = CoerceOffset(targetOffset);

            if (!animate)
            {
                SetKnobOffset(targetOffset);
                return;
            }

            var animation = new DoubleAnimation
            {
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.CurrentTimeInvalidated += (sender, args) =>
            {
                _currentOffset = KnobTransform.X;
            };

            animation.Completed += (sender, args) =>
            {
                SetKnobOffset(targetOffset);
            };

            KnobTransform.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private void SetKnobOffset(double offset)
        {
            _currentOffset = CoerceOffset(offset);
            KnobTransform.X = _currentOffset;
        }

        private double CoerceOffset(double offset)
        {
            return Math.Max(0d, Math.Min(GetMaxOffset(), offset));
        }

        private double GetMaxOffset()
        {
            var availableWidth = TrackBorder.ActualWidth - Knob.ActualWidth - (KnobHorizontalMargin * 2d);
            return Math.Max(0d, availableWidth);
        }

        private void UpdateKnobText()
        {
            if (KnobText == null)
                return;

            KnobText.Text = IsDarkThemeSelected ? DarkLabel : LightLabel;
        }

        private void UpdateSelectionDuringDrag()
        {
            var maxOffset = GetMaxOffset();
            var normalizedOffset = maxOffset <= 0d ? 0d : _currentOffset / maxOffset;
            var selectDark = normalizedOffset >= DragCommitThreshold;

            if (IsDarkThemeSelected != selectDark)
            {
                SetCurrentValue(IsDarkThemeSelectedProperty, selectDark);
            }

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            UpdateKnobText();

            if (LightKnobIcon != null)
                LightKnobIcon.Visibility = IsDarkThemeSelected ? Visibility.Collapsed : Visibility.Visible;

            if (DarkKnobIcon != null)
                DarkKnobIcon.Visibility = IsDarkThemeSelected ? Visibility.Visible : Visibility.Collapsed;

            if (LightStateHost != null)
                LightStateHost.Opacity = IsDarkThemeSelected ? 0.48d : 0.82d;

            if (DarkStateHost != null)
                DarkStateHost.Opacity = IsDarkThemeSelected ? 0.82d : 0.48d;

            UpdateSunVisualState();
            UpdateMoonVisualState();
        }

        private void UpdateSunVisualState()
        {
            var animateSun = !_suppressAnimation && (_isDragging || !IsDarkThemeSelected);

            UpdateSunAnimation(LightStateSunRaysRotate, LightStateSunGlowScale, animateSun, 4000d, 1.16d);
            UpdateSunAnimation(LightKnobSunRaysRotate, LightKnobSunGlowScale, animateSun, 5500d, 1.2d);
        }

        private void UpdateMoonVisualState()
        {
            var animateMoon = !_suppressAnimation && (_isDragging || IsDarkThemeSelected);

            UpdateMoonAnimation(DarkStateMoonTiltTransform, DarkStateMoonFloatTransform, DarkStateMoonGlowScale, DarkStateMoonSparkles, animateMoon, 2400d, 1.12d, -1.4d, 1.8d, 0.9d);
            UpdateMoonAnimation(DarkKnobMoonTiltTransform, DarkKnobMoonFloatTransform, DarkKnobMoonGlowScale, DarkKnobMoonSparkles, animateMoon, 1900d, 1.16d, -1.8d, 2.4d, 1d);
        }

        private static void UpdateSunAnimation(RotateTransform raysRotate, ScaleTransform glowScale, bool animate, double rotationDurationMs, double glowScaleTo)
        {
            if (raysRotate == null || glowScale == null)
                return;

            if (!animate)
            {
                raysRotate.BeginAnimation(RotateTransform.AngleProperty, null);
                glowScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                glowScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                raysRotate.Angle = 0d;
                glowScale.ScaleX = 1d;
                glowScale.ScaleY = 1d;
                return;
            }

            var rotationAnimation = new DoubleAnimation
            {
                From = 0d,
                To = 360d,
                Duration = TimeSpan.FromMilliseconds(rotationDurationMs),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var glowAnimation = new DoubleAnimation
            {
                From = 1d,
                To = glowScaleTo,
                Duration = TimeSpan.FromMilliseconds(880),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            raysRotate.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation, HandoffBehavior.SnapshotAndReplace);
            glowScale.BeginAnimation(ScaleTransform.ScaleXProperty, glowAnimation, HandoffBehavior.SnapshotAndReplace);
            glowScale.BeginAnimation(ScaleTransform.ScaleYProperty, glowAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private static void UpdateMoonAnimation(RotateTransform moonTilt, TranslateTransform moonFloat, ScaleTransform glowScale, UIElement sparkles, bool animate, double durationMs, double glowScaleTo, double floatToY, double tiltTo, double sparkleOpacityTo)
        {
            if (moonTilt == null || moonFloat == null || glowScale == null || sparkles == null)
                return;

            if (!animate)
            {
                moonTilt.BeginAnimation(RotateTransform.AngleProperty, null);
                moonFloat.BeginAnimation(TranslateTransform.YProperty, null);
                glowScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                glowScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                sparkles.BeginAnimation(UIElement.OpacityProperty, null);
                moonTilt.Angle = 0d;
                moonFloat.Y = 0d;
                glowScale.ScaleX = 1d;
                glowScale.ScaleY = 1d;
                sparkles.Opacity = 0.72d;
                return;
            }

            var tiltAnimation = new DoubleAnimation
            {
                From = -tiltTo,
                To = tiltTo,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var floatAnimation = new DoubleAnimation
            {
                From = 0d,
                To = floatToY,
                Duration = TimeSpan.FromMilliseconds(durationMs * 0.72d),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var glowAnimation = new DoubleAnimation
            {
                From = 1d,
                To = glowScaleTo,
                Duration = TimeSpan.FromMilliseconds(durationMs * 0.6d),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var sparkleAnimation = new DoubleAnimation
            {
                From = 0.62d,
                To = sparkleOpacityTo,
                Duration = TimeSpan.FromMilliseconds(durationMs * 0.5d),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            moonTilt.BeginAnimation(RotateTransform.AngleProperty, tiltAnimation, HandoffBehavior.SnapshotAndReplace);
            moonFloat.BeginAnimation(TranslateTransform.YProperty, floatAnimation, HandoffBehavior.SnapshotAndReplace);
            glowScale.BeginAnimation(ScaleTransform.ScaleXProperty, glowAnimation, HandoffBehavior.SnapshotAndReplace);
            glowScale.BeginAnimation(ScaleTransform.ScaleYProperty, glowAnimation, HandoffBehavior.SnapshotAndReplace);
            sparkles.BeginAnimation(UIElement.OpacityProperty, sparkleAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private bool IsKnobDescendant(DependencyObject source)
        {
            while (source != null)
            {
                if (ReferenceEquals(source, Knob))
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }
    }
}
