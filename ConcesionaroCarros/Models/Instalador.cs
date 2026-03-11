using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
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

        public int Id { get; set; }

        public string Ruta
        {
            get => _ruta;
            set
            {
                _ruta = value;
                OnPropertyChanged(nameof(Ruta));
                OnPropertyChanged(nameof(Icono));
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

        // 🔥 ICONO REAL SIN SYSTEM.DRAWING
        public BitmapSource Icono
        {
            get
            {
                if (string.IsNullOrEmpty(Ruta) || !File.Exists(Ruta))
                    return null;

                try
                {
                    SHFILEINFO shinfo = new SHFILEINFO();

                    SHGetFileInfo(Ruta, 0, ref shinfo,
                        (uint)Marshal.SizeOf(shinfo),
                        SHGFI_ICON | SHGFI_LARGEICON);

                    if (shinfo.hIcon == IntPtr.Zero)
                        return null;

                    var icon = Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    DestroyIcon(shinfo.hIcon);

                    return icon;
                }
                catch
                {
                    return null;
                }
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
