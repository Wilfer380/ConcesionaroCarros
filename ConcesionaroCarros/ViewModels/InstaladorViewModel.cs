using ConcesionaroCarros.Commands;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ConcesionaroCarros.ViewModels
{
    public class InstaladorViewModel : BaseViewModel
    {
        private string _ruta;

        public string NombreArchivo { get; set; }
        public BitmapImage Icono { get; set; }

        public RelayCommand BuscarExeCommand { get; }
        public RelayCommand EjecutarCommand { get; }

        public InstaladorViewModel()
        {
            BuscarExeCommand = new RelayCommand(_ => Buscar());
            EjecutarCommand = new RelayCommand(_ => Ejecutar());
        }

        void Buscar()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Instaladores (*.exe)|*.exe";

            if (dlg.ShowDialog() == true)
            {
                _ruta = dlg.FileName;

                NombreArchivo = Path.GetFileName(_ruta);
                OnPropertyChanged(nameof(NombreArchivo));

                // Solución alternativa para extraer el icono sin System.Drawing.Icon
                Icono = null;
                try
                {
                    using (var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/YourIcon.ico"))?.Stream)
                    {
                        if (iconStream != null)
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = iconStream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            Icono = bitmap;
                        }
                    }
                }
                catch
                {
                    // Manejo de errores si no se puede cargar el icono
                    Icono = null;
                }

                OnPropertyChanged(nameof(Icono));
            }
        }

        void Ejecutar()
        {
            if (string.IsNullOrEmpty(_ruta) || !File.Exists(_ruta))
            {
                MessageBox.Show("Primero debes seleccionar un instalador (.exe)");
                return;
            }

            Process.Start(new ProcessStartInfo()
            {
                FileName = _ruta,
                UseShellExecute = true
            });
        }

    }
}
