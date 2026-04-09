using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Threading;
using ConcesionaroCarros.ViewModels;
using ConcesionaroCarros.Services;
using Microsoft.Win32;

namespace ConcesionaroCarros.Views
{
    public partial class HelpView : UserControl
    {
        private HelpViewModel _viewModel;
        private ScrollViewer _documentScrollViewer;
        private const double WheelScrollStep = 96d;
        private const double DefaultDocumentZoom = 100d;
        private const double MaxDocumentZoom = 170d;
        private const double ZoomStep = 10d;
        private const double HorizontalPanActivationThreshold = 12d;
        private const double HorizontalPanDominanceRatio = 1.35d;
        private const double HorizontalPanSpeedFactor = 0.9d;
        private const int MaxNavigationRestoreAttempts = 3;
        private bool _isRestoringNavigationState;
        private bool _isApplyingDocumentTheme;
        private bool _areDocumentMouseHandlersAttached;
        private bool _areThemeObserversAttached;
        private bool _areSystemThemeEventsAttached;
        private bool _isDocumentPanPending;
        private bool _isPanningDocumentHorizontally;
        private Point _documentPanStartPoint;
        private double _documentPanStartHorizontalOffset;
        private Brush _documentViewerBackgroundBrush;
        private Brush _documentViewerForegroundBrush;

        public HelpView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SectionsScrollViewer.PreviewMouseWheel -= OnAcceleratedMouseWheel;
            SectionsScrollViewer.PreviewMouseWheel += OnAcceleratedMouseWheel;

            DocumentViewer.Zoom = DefaultDocumentZoom;
            ConectarEventosDeMouseDocumento();
            DocumentViewer.PreviewMouseWheel -= OnDocumentMouseWheel;
            DocumentViewer.PreviewMouseWheel += OnDocumentMouseWheel;
            ConectarScrollViewerDocumento();
            ConectarObservadoresDeTema();
            ConectarEventosDeTemaDelSistema();
            AplicarRecursosDeTema();
            AplicarTemaAlDocumentoActual();
            ProgramarRestauracionDeNavegacion();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DesconectarEventosDeTemaDelSistema();
            DesconectarObservadoresDeTema();
        }

        private void ConectarEventosDeMouseDocumento()
        {
            if (_areDocumentMouseHandlersAttached)
                return;

            DocumentViewer.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnDocumentMouseLeftButtonDown), true);
            DocumentViewer.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnDocumentMouseMove), true);
            DocumentViewer.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnDocumentMouseLeftButtonUp), true);
            _areDocumentMouseHandlersAttached = true;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = e.NewValue as HelpViewModel;

            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            AplicarRecursosDeTema();
            ConectarScrollViewerDocumento();
        }

        private void ConectarEventosDeTemaDelSistema()
        {
            if (_areSystemThemeEventsAttached)
                return;

            SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
            _areSystemThemeEventsAttached = true;
        }

        private void DesconectarEventosDeTemaDelSistema()
        {
            if (!_areSystemThemeEventsAttached)
                return;

            SystemEvents.UserPreferenceChanged -= OnSystemUserPreferenceChanged;
            _areSystemThemeEventsAttached = false;
        }

        private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.Color &&
                e.Category != UserPreferenceCategory.General &&
                e.Category != UserPreferenceCategory.VisualStyle)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                AplicarRecursosDeTema();
                AplicarTemaAlDocumentoActual();
            }), DispatcherPriority.Background);
        }

        private void AplicarRecursosDeTema()
        {
            var theme = MarkdownRenderTheme.CreateForCurrentSystemTheme();

            Resources["HelpNavigationPanelBrush"] = theme.HelpNavigationPanelBrush;
            Resources["HelpNavigationCardBrush"] = theme.HelpNavigationCardBrush;
            Resources["HelpSurfaceBrush"] = theme.HelpSurfaceBrush;
            Resources["HelpPanelBrush"] = theme.HelpPanelBrush;
            Resources["HelpBorderBrush"] = theme.HelpBorderBrush;
            Resources["HelpTextBrush"] = theme.HelpTextBrush;
            Resources["HelpMutedBrush"] = theme.HelpMutedBrush;
            Resources["HelpAccentBrush"] = theme.HelpAccentBrush;
            Resources["HelpAccentSoftBrush"] = theme.HelpAccentSoftBrush;
            Resources["HelpToolbarHoverBrush"] = theme.HelpToolbarHoverBrush;
            Resources["HelpPlaceholderSurfaceBrush"] = theme.HelpPlaceholderSurfaceBrush;
            Resources["HelpPlaceholderAccentBrush"] = theme.HelpPlaceholderBorderBrush;
            Resources["DocumentationComboBoxBackgroundBrush"] = theme.HelpComboBoxBackgroundBrush;
            Resources["DocumentationComboBoxHoverBrush"] = theme.HelpComboBoxHoverBrush;
            Resources["DocumentationComboBoxSelectedBrush"] = theme.HelpComboBoxSelectedBrush;
            Resources["DocumentationComboBoxPopupBrush"] = theme.HelpComboBoxPopupBrush;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(HelpViewModel.SelectedDocumentFlow))
                return;

            AplicarTemaAlDocumentoActual();
            ConectarScrollViewerDocumento();
            ProgramarRestauracionDeNavegacion();
        }

        private void ConectarObservadoresDeTema()
        {
            if (_areThemeObserversAttached)
                return;

            var backgroundDescriptor = DependencyPropertyDescriptor.FromProperty(Control.BackgroundProperty, typeof(FlowDocumentScrollViewer));
            backgroundDescriptor?.AddValueChanged(DocumentViewer, OnDocumentThemeBrushPropertyChanged);

            var foregroundDescriptor = DependencyPropertyDescriptor.FromProperty(Control.ForegroundProperty, typeof(FlowDocumentScrollViewer));
            foregroundDescriptor?.AddValueChanged(DocumentViewer, OnDocumentThemeBrushPropertyChanged);

            _areThemeObserversAttached = true;
            ActualizarSuscripcionesDeBrushDeTema();
        }

        private void DesconectarObservadoresDeTema()
        {
            if (!_areThemeObserversAttached)
                return;

            var backgroundDescriptor = DependencyPropertyDescriptor.FromProperty(Control.BackgroundProperty, typeof(FlowDocumentScrollViewer));
            backgroundDescriptor?.RemoveValueChanged(DocumentViewer, OnDocumentThemeBrushPropertyChanged);

            var foregroundDescriptor = DependencyPropertyDescriptor.FromProperty(Control.ForegroundProperty, typeof(FlowDocumentScrollViewer));
            foregroundDescriptor?.RemoveValueChanged(DocumentViewer, OnDocumentThemeBrushPropertyChanged);

            if (_documentViewerBackgroundBrush is Freezable backgroundFreezable && !backgroundFreezable.IsFrozen)
                backgroundFreezable.Changed -= OnDocumentThemeBrushInstanceChanged;

            if (_documentViewerForegroundBrush is Freezable foregroundFreezable && !foregroundFreezable.IsFrozen)
                foregroundFreezable.Changed -= OnDocumentThemeBrushInstanceChanged;

            _documentViewerBackgroundBrush = null;
            _documentViewerForegroundBrush = null;
            _areThemeObserversAttached = false;
        }

        private void OnDocumentThemeBrushPropertyChanged(object sender, EventArgs e)
        {
            ActualizarSuscripcionesDeBrushDeTema();
            AplicarTemaAlDocumentoActual();
        }

        private void OnDocumentThemeBrushInstanceChanged(object sender, EventArgs e)
        {
            AplicarTemaAlDocumentoActual();
        }

        private void ActualizarSuscripcionesDeBrushDeTema()
        {
            SuscribirBrushDeTema(ref _documentViewerBackgroundBrush, DocumentViewer.Background);
            SuscribirBrushDeTema(ref _documentViewerForegroundBrush, DocumentViewer.Foreground);
        }

        private void SuscribirBrushDeTema(ref Brush currentBrush, Brush nextBrush)
        {
            if (ReferenceEquals(currentBrush, nextBrush))
                return;

            if (currentBrush is Freezable currentFreezable && !currentFreezable.IsFrozen)
                currentFreezable.Changed -= OnDocumentThemeBrushInstanceChanged;

            currentBrush = nextBrush;

            if (currentBrush is Freezable nextFreezable && !nextFreezable.IsFrozen)
                nextFreezable.Changed += OnDocumentThemeBrushInstanceChanged;
        }

        private void AplicarTemaAlDocumentoActual()
        {
            if (_isApplyingDocumentTheme || _viewModel == null || !_viewModel.HasSelectedDocument)
                return;

            var renderTheme = MarkdownRenderTheme.FromBrushes(DocumentViewer.Background, DocumentViewer.Foreground);

            _isApplyingDocumentTheme = true;
            try
            {
                _viewModel.RegenerarSelectedDocumentFlow(renderTheme);
            }
            finally
            {
                _isApplyingDocumentTheme = false;
            }
        }

        private void ProgramarRestauracionDeNavegacion(int attempt = 0)
        {
            Dispatcher.BeginInvoke(
                new Action(() => RestaurarNavegacionSeleccionada(attempt)),
                DispatcherPriority.Loaded);
        }

        private void RestaurarNavegacionSeleccionada(int attempt)
        {
            var viewModel = DataContext as HelpViewModel;
            var document = DocumentViewer.Document;
            var scrollViewer = ConectarScrollViewerDocumento();
            if (viewModel == null || document == null || scrollViewer == null)
                return;

            var navigationState = viewModel.PendingNavigationState;
            if (navigationState != null &&
                navigationState.HasVisualPosition &&
                scrollViewer.ScrollableHeight <= 0 &&
                attempt < MaxNavigationRestoreAttempts)
            {
                ProgramarRestauracionDeNavegacion(attempt + 1);
                return;
            }

            _isRestoringNavigationState = true;

            try
            {
                if (TryRestoreSavedOffset(scrollViewer, navigationState))
                {
                    CapturarEstadoVisualActual();
                    return;
                }

                if (string.IsNullOrWhiteSpace(viewModel.SelectedDocumentAnchor))
                {
                    scrollViewer.ScrollToHome();
                    CapturarEstadoVisualActual();
                    return;
                }

                var heading = FindHeadingByAnchor(document, viewModel.SelectedDocumentAnchor);
                if (heading != null)
                {
                    heading.BringIntoView();
                    CapturarEstadoVisualActual();
                    return;
                }

                scrollViewer.ScrollToHome();
                CapturarEstadoVisualActual();
            }
            finally
            {
                _isRestoringNavigationState = false;
            }
        }

        private void OnAcceleratedMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null)
                return;

            var steps = Math.Max(1, Math.Abs(e.Delta) / 120);
            var offsetDelta = WheelScrollStep * steps;
            var newOffset = e.Delta > 0
                ? scrollViewer.VerticalOffset - offsetDelta
                : scrollViewer.VerticalOffset + offsetDelta;

            if (newOffset < 0)
                newOffset = 0;

            scrollViewer.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }

        private void OnDocumentMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                AjustarZoomDocumento(e.Delta);
                e.Handled = true;
                return;
            }

            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null)
            {
                scrollViewer = FindDescendantScrollViewer(DocumentViewer);
                if (scrollViewer == null)
                    return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && scrollViewer.ScrollableWidth > 0)
                ScrollHorizontally(scrollViewer, e.Delta);
            else
                ScrollVertically(scrollViewer, e.Delta);

            e.Handled = true;
        }

        private static void ScrollVertically(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer == null)
                return;

            var steps = Math.Max(1, Math.Abs(delta) / 120);
            var offsetDelta = WheelScrollStep * steps;
            var newOffset = delta > 0
                ? scrollViewer.VerticalOffset - offsetDelta
                : scrollViewer.VerticalOffset + offsetDelta;

            if (newOffset < 0)
                newOffset = 0;

            scrollViewer.ScrollToVerticalOffset(newOffset);
        }

        private static void ScrollHorizontally(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer == null)
                return;

            var steps = Math.Max(1, Math.Abs(delta) / 120);
            var offsetDelta = WheelScrollStep * steps;
            var newOffset = delta > 0
                ? scrollViewer.HorizontalOffset - offsetDelta
                : scrollViewer.HorizontalOffset + offsetDelta;

            if (newOffset < 0)
                newOffset = 0;
            else if (newOffset > scrollViewer.ScrollableWidth)
                newOffset = scrollViewer.ScrollableWidth;

            scrollViewer.ScrollToHorizontalOffset(newOffset);
        }

        private ScrollViewer ConectarScrollViewerDocumento()
        {
            var scrollViewer = FindDescendantScrollViewer(DocumentViewer);
            if (ReferenceEquals(_documentScrollViewer, scrollViewer))
                return _documentScrollViewer;

            if (_documentScrollViewer != null)
            {
                CancelHorizontalPan(_documentScrollViewer);
                _documentScrollViewer.PreviewMouseWheel -= OnDocumentMouseWheel;
                _documentScrollViewer.ScrollChanged -= OnDocumentScrollChanged;
                _documentScrollViewer.LostMouseCapture -= OnDocumentLostMouseCapture;
            }

            _documentScrollViewer = scrollViewer;

            if (_documentScrollViewer != null)
            {
                _documentScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                _documentScrollViewer.PreviewMouseWheel -= OnDocumentMouseWheel;
                _documentScrollViewer.PreviewMouseWheel += OnDocumentMouseWheel;
                _documentScrollViewer.ScrollChanged -= OnDocumentScrollChanged;
                _documentScrollViewer.ScrollChanged += OnDocumentScrollChanged;
                _documentScrollViewer.LostMouseCapture -= OnDocumentLostMouseCapture;
                _documentScrollViewer.LostMouseCapture += OnDocumentLostMouseCapture;
            }

            return _documentScrollViewer;
        }

        private void OnDocumentScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isRestoringNavigationState)
                return;

            CapturarEstadoVisualActual();
        }

        private void OnDocumentMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var scrollViewer = _documentScrollViewer ?? ConectarScrollViewerDocumento();
            if (!CanPanHorizontally(scrollViewer) ||
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                CancelHorizontalPan(scrollViewer);
                return;
            }

            _isDocumentPanPending = true;
            _isPanningDocumentHorizontally = false;
            _documentPanStartPoint = e.GetPosition(DocumentViewer);
            _documentPanStartHorizontalOffset = scrollViewer.HorizontalOffset;
        }

        private void OnDocumentMouseMove(object sender, MouseEventArgs e)
        {
            var scrollViewer = _documentScrollViewer ?? ConectarScrollViewerDocumento();
            if (scrollViewer == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                CancelHorizontalPan(scrollViewer);
                return;
            }

            if (!_isDocumentPanPending && !_isPanningDocumentHorizontally)
                return;

            var currentPoint = e.GetPosition(DocumentViewer);
            var horizontalDelta = currentPoint.X - _documentPanStartPoint.X;

            if (!_isPanningDocumentHorizontally)
            {
                var verticalDelta = currentPoint.Y - _documentPanStartPoint.Y;
                var absoluteHorizontalDelta = Math.Abs(horizontalDelta);
                var absoluteVerticalDelta = Math.Abs(verticalDelta);
                if (absoluteHorizontalDelta < HorizontalPanActivationThreshold ||
                    absoluteHorizontalDelta <= absoluteVerticalDelta * HorizontalPanDominanceRatio)
                    return;

                _isPanningDocumentHorizontally = true;
                _documentPanStartPoint = currentPoint;
                _documentPanStartHorizontalOffset = scrollViewer.HorizontalOffset;
                if (!scrollViewer.IsMouseCaptured)
                    scrollViewer.CaptureMouse();

                horizontalDelta = 0;
            }

            Mouse.OverrideCursor = Cursors.ScrollWE;

            var nextOffset = _documentPanStartHorizontalOffset - (horizontalDelta * HorizontalPanSpeedFactor);
            if (nextOffset < 0)
                nextOffset = 0;
            else if (nextOffset > scrollViewer.ScrollableWidth)
                nextOffset = scrollViewer.ScrollableWidth;

            scrollViewer.ScrollToHorizontalOffset(nextOffset);
            e.Handled = true;
        }

        private void OnDocumentMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var scrollViewer = _documentScrollViewer ?? ConectarScrollViewerDocumento();
            var wasPanning = _isPanningDocumentHorizontally;
            CancelHorizontalPan(scrollViewer);

            if (wasPanning)
                e.Handled = true;
        }

        private void OnDocumentLostMouseCapture(object sender, MouseEventArgs e)
        {
            CancelHorizontalPan(sender as ScrollViewer);
        }

        private void CapturarEstadoVisualActual()
        {
            if (_viewModel == null)
                return;

            var scrollViewer = _documentScrollViewer ?? ConectarScrollViewerDocumento();
            var document = DocumentViewer.Document;
            if (scrollViewer == null || document == null)
                return;

            var anchor = FindNearestVisibleAnchor(document) ?? _viewModel.SelectedDocumentAnchor;
            _viewModel.ActualizarEstadoVisualDocumentoActual(anchor, scrollViewer.VerticalOffset, scrollViewer.ScrollableHeight);
        }

        private static bool TryRestoreSavedOffset(ScrollViewer scrollViewer, HelpDocumentNavigationState navigationState)
        {
            if (scrollViewer == null || navigationState == null || !navigationState.HasVisualPosition)
                return false;

            var maxOffset = scrollViewer.ScrollableHeight;
            if (maxOffset <= 0)
                return false;

            var targetOffset = navigationState.VerticalOffset;
            if (navigationState.ScrollableHeight > 0 &&
                Math.Abs(navigationState.ScrollableHeight - maxOffset) > 1)
                targetOffset = navigationState.VerticalRatio * maxOffset;

            if (targetOffset < 0)
                targetOffset = 0;
            else if (targetOffset > maxOffset)
                targetOffset = maxOffset;

            scrollViewer.ScrollToVerticalOffset(targetOffset);
            return true;
        }

        private void AjustarZoomDocumento(int delta)
        {
            var currentZoom = DocumentViewer.Zoom;
            double nextZoom;

            if (delta > 0)
            {
                nextZoom = currentZoom + ZoomStep;
                if (nextZoom > MaxDocumentZoom)
                    nextZoom = MaxDocumentZoom;
            }
            else
            {
                nextZoom = currentZoom - ZoomStep;
                if (nextZoom < DefaultDocumentZoom)
                    nextZoom = DefaultDocumentZoom;
            }

            DocumentViewer.Zoom = nextZoom;
        }

        private static bool CanPanHorizontally(ScrollViewer scrollViewer)
        {
            return scrollViewer != null && scrollViewer.ScrollableWidth > 0;
        }

        private void CancelHorizontalPan(ScrollViewer scrollViewer)
        {
            _isDocumentPanPending = false;
            _isPanningDocumentHorizontally = false;
            Mouse.OverrideCursor = null;

            if (scrollViewer != null && scrollViewer.IsMouseCaptured)
                scrollViewer.ReleaseMouseCapture();
        }

        private static ScrollViewer FindDescendantScrollViewer(DependencyObject root)
        {
            if (root == null)
                return null;

            var children = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < children; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var scrollViewer = child as ScrollViewer;
                if (scrollViewer != null)
                    return scrollViewer;

                var nested = FindDescendantScrollViewer(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static Paragraph FindHeadingByAnchor(FlowDocument document, string anchor)
        {
            return document.Blocks
                .OfType<Paragraph>()
                .FirstOrDefault(x => string.Equals(x.Tag as string, anchor, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindNearestVisibleAnchor(FlowDocument document)
        {
            if (document == null)
                return null;

            string currentAnchor = null;
            double currentTop = double.NegativeInfinity;
            string nearestAnchor = null;
            double nearestDistance = double.MaxValue;

            foreach (var heading in document.Blocks.OfType<Paragraph>())
            {
                var anchor = heading.Tag as string;
                if (string.IsNullOrWhiteSpace(anchor))
                    continue;

                var rect = heading.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                if (rect.IsEmpty)
                    continue;

                if (rect.Top <= 1 && rect.Top > currentTop)
                {
                    currentTop = rect.Top;
                    currentAnchor = anchor;
                }

                var distance = Math.Abs(rect.Top);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestAnchor = anchor;
                }
            }

            return currentAnchor ?? nearestAnchor;
        }
    }
}
