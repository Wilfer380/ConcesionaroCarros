using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System;

namespace ConcesionaroCarros.Views
{
    public partial class HelpView : UserControl
    {
        private const double WheelScrollStep = 96d;
        private const double DefaultDocumentZoom = 100d;
        private const double MaxDocumentZoom = 170d;
        private const double ZoomStep = 10d;

        public HelpView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SectionsScrollViewer.PreviewMouseWheel -= OnAcceleratedMouseWheel;
            SectionsScrollViewer.PreviewMouseWheel += OnAcceleratedMouseWheel;

            DocumentViewer.Zoom = DefaultDocumentZoom;
            DocumentViewer.PreviewMouseWheel -= OnDocumentMouseWheel;
            DocumentViewer.PreviewMouseWheel += OnDocumentMouseWheel;

            var documentScrollViewer = FindDescendantScrollViewer(DocumentViewer);
            if (documentScrollViewer == null)
                return;

            documentScrollViewer.PreviewMouseWheel -= OnDocumentMouseWheel;
            documentScrollViewer.PreviewMouseWheel += OnDocumentMouseWheel;
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
    }
}
