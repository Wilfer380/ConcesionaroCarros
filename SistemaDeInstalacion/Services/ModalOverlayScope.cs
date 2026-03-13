using System;
using System.Windows;
using System.Windows.Media;

namespace ConcesionaroCarros.Services
{
    public sealed class ModalOverlayScope : IDisposable
    {
        private readonly Window _owner;
        private readonly Window _overlay;

        public Window OverlayWindow => _overlay;

        public ModalOverlayScope(Window owner)
        {
            _owner = owner;
            if (_owner == null)
                return;

            _overlay = new Window
            {
                Owner = _owner,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                ShowInTaskbar = false,
                ShowActivated = false,
                Background = new SolidColorBrush(Color.FromArgb(140, 25, 32, 43)),
                Left = _owner.Left,
                Top = _owner.Top,
                Width = Math.Max(_owner.ActualWidth, _owner.Width),
                Height = Math.Max(_owner.ActualHeight, _owner.Height)
            };

            _owner.LocationChanged += OwnerBoundsChanged;
            _owner.SizeChanged += OwnerBoundsChanged;

            _overlay.Show();
        }

        private void OwnerBoundsChanged(object sender, EventArgs e)
        {
            if (_overlay == null || _owner == null)
                return;

            _overlay.Left = _owner.Left;
            _overlay.Top = _owner.Top;
            _overlay.Width = Math.Max(_owner.ActualWidth, _owner.Width);
            _overlay.Height = Math.Max(_owner.ActualHeight, _owner.Height);
        }

        public void Dispose()
        {
            if (_owner != null)
            {
                _owner.LocationChanged -= OwnerBoundsChanged;
                _owner.SizeChanged -= OwnerBoundsChanged;
            }

            if (_overlay != null)
                _overlay.Close();
        }
    }
}
