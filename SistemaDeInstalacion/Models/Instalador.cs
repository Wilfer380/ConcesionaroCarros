using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ConcesionaroCarros.Models
{
    public class Instalador : INotifyPropertyChanged
    {
        private string _ruta;
        private string _nombre;
        private string _descripcion;
        private string _carpeta;
        private BitmapSource _icono;

        private static readonly object IconCacheLock = new object();
        private static readonly Dictionary<string, BitmapSource> IconosPorExtension =
            new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, BitmapSource> IconosPorRuta =
            new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);

        public int Id { get; set; }

        public string Ruta
        {
            get => _ruta;
            set
            {
                _ruta = value;
                _icono = ObtenerIconoGenerico(value);
                OnPropertyChanged(nameof(Ruta));
                OnPropertyChanged(nameof(Icono));
                _ = CargarIconoRealAsync(value);
            }
        }

        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged(nameof(Nombre));
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set
            {
                _descripcion = value;
                OnPropertyChanged(nameof(Descripcion));
            }
        }

        public string Carpeta
        {
            get => _carpeta;
            set
            {
                _carpeta = value;
                OnPropertyChanged(nameof(Carpeta));
            }
        }

        public BitmapSource Icono => _icono;

        private static BitmapSource ObtenerIconoGenerico(string ruta)
        {
            var extension = Path.GetExtension(ruta);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".exe";

            if (!extension.StartsWith(".", StringComparison.Ordinal))
                extension = "." + extension;

            lock (IconCacheLock)
            {
                if (IconosPorExtension.TryGetValue(extension, out var iconoCacheado))
                    return iconoCacheado;
            }

            var icono = CrearIconoPorExtension(extension);

            lock (IconCacheLock)
            {
                IconosPorExtension[extension] = icono;
            }

            return icono;
        }

        private async Task CargarIconoRealAsync(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return;

            BitmapSource iconoReal;

            lock (IconCacheLock)
            {
                if (IconosPorRuta.TryGetValue(ruta, out iconoReal))
                {
                    AplicarIconoReal(ruta, iconoReal);
                    return;
                }
            }

            iconoReal = await Task.Run(() => CrearIconoDesdeRuta(ruta));
            if (iconoReal == null)
                return;

            lock (IconCacheLock)
            {
                IconosPorRuta[ruta] = iconoReal;
            }

            AplicarIconoReal(ruta, iconoReal);
        }

        private void AplicarIconoReal(string ruta, BitmapSource iconoReal)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
                return;

            dispatcher.BeginInvoke(new Action(() =>
            {
                if (!string.Equals(_ruta, ruta, StringComparison.OrdinalIgnoreCase))
                    return;

                _icono = iconoReal;
                OnPropertyChanged(nameof(Icono));
            }));
        }

        private static BitmapSource CrearIconoDesdeRuta(string ruta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                    return null;

                return CrearBitmapDesdeShell(ruta, FILE_ATTRIBUTE_NORMAL, SHGFI_ICON | SHGFI_LARGEICON);
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource CrearIconoPorExtension(string extension)
        {
            try
            {
                return CrearBitmapDesdeShell(
                    extension,
                    FILE_ATTRIBUTE_NORMAL,
                    SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource CrearBitmapDesdeShell(string path, uint atributos, uint flags)
        {
            var shinfo = new SHFILEINFO();

            SHGetFileInfo(
                path,
                atributos,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                flags);

            if (shinfo.hIcon == IntPtr.Zero)
                return null;

            try
            {
                var icono = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                if (icono.CanFreeze)
                    icono.Freeze();

                return icono;
            }
            finally
            {
                DestroyIcon(shinfo.hIcon);
            }
        }

        #region Windows API

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbSizeFileInfo,
            uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
