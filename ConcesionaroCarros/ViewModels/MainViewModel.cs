using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ConcesionaroCarros.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentView;
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private readonly string _nombreVisibleDispositivo;
        private readonly string _correoDispositivo;

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDashboardVisible));
            }
        }

        public bool IsDashboardVisible => CurrentView == null;

        private string _vistaActiva;
        public string VistaActiva
        {
            get => _vistaActiva;
            set
            {
                _vistaActiva = value;
                OnPropertyChanged();
            }
        }

        private bool _soloInventario = true;
        public bool SoloInventario
        {
            get => _soloInventario;
            set
            {
                _soloInventario = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Carro> Carrito { get; } =
            new ObservableCollection<Carro>();

        public int CarritoCount => Carrito.Count;

        public string NombreUsuario
        {
            get
            {
                var usuarioActual = SesionUsuario.UsuarioActual;
                if (usuarioActual == null)
                    return string.Empty;

                var correoSesion = (usuarioActual.Correo ?? string.Empty).Trim();
                var esCuentaDelDispositivo =
                    !string.IsNullOrWhiteSpace(_correoDispositivo) &&
                    string.Equals(correoSesion, _correoDispositivo, StringComparison.OrdinalIgnoreCase);

                if (esCuentaDelDispositivo && !string.IsNullOrWhiteSpace(_nombreVisibleDispositivo))
                    return _nombreVisibleDispositivo;

                var usuarioCorreo = ObtenerUsuarioDesdeCorreo(correoSesion);
                if (!string.IsNullOrWhiteSpace(usuarioCorreo))
                    return usuarioCorreo;

                return (usuarioActual.Nombres + " " + usuarioActual.Apellidos).Trim();
            }
        }

        public bool EsAdministrador => SesionUsuario.EsAdmin;

        public ICommand CerrarSesionCommand { get; }
        public ICommand CambiarFotoPerfilCommand { get; }

        public void ActualizarCarrito()
        {
            OnPropertyChanged(nameof(CarritoCount));
        }

        public Carro CarroSeleccionado { get; set; }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowCarrosCommand { get; }
        public ICommand ShowClientesCommand { get; }
        public ICommand ShowEmpleadosCommand { get; }
        public ICommand ShowGestionUsuariosCommand { get; }
        public ICommand ShowVentaCommand { get; }

        private BitmapImage _fotoPerfil;
        public BitmapImage FotoPerfil
        {
            get => _fotoPerfil;
            set
            {
                _fotoPerfil = value;
                OnPropertyChanged();
            }
        }

        private BitmapImage CargarImagen(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private void CambiarFotoPerfil()
        {
            var usuario = SesionUsuario.UsuarioActual;
            if (usuario == null)
                return;

            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar foto de perfil",
                Filter = "Imagenes|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var directorioPerfiles = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ConcesionaroCarros",
                    "Perfiles");

                if (!Directory.Exists(directorioPerfiles))
                    Directory.CreateDirectory(directorioPerfiles);

                var extension = Path.GetExtension(dialog.FileName);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                var rutaDestino = Path.Combine(
                    directorioPerfiles,
                    $"usuario_{usuario.Id}{extension.ToLowerInvariant()}");

                foreach (var archivo in Directory.GetFiles(directorioPerfiles, $"usuario_{usuario.Id}.*"))
                {
                    if (!string.Equals(archivo, rutaDestino, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(archivo); } catch { }
                    }
                }

                File.Copy(dialog.FileName, rutaDestino, true);

                _usuariosDb.ActualizarFotoPerfil(usuario.Id, rutaDestino);
                SesionUsuario.ActualizarFoto(rutaDestino);
                FotoPerfil = CargarImagen(rutaDestino);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No fue posible actualizar la foto de perfil.\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public MainViewModel()
        {
            _nombreVisibleDispositivo = WindowsProfileService.ObtenerNombreVisible();
            _correoDispositivo = WindowsProfileService.ObtenerCorreoPrincipal();
            if (string.IsNullOrWhiteSpace(_correoDispositivo))
            {
                _correoDispositivo = _usuariosDb.ObtenerCorreoPorUsuarioDispositivo(
                    Environment.UserName ?? string.Empty,
                    _nombreVisibleDispositivo);
            }

            SoloInventario = true;
            ShowDashboardCommand = new RelayCommand(_ =>
            {
                VistaActiva = null;
                CurrentView = null;
            });

            ShowCarrosCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Carros";
                CurrentView = new CarrosView
                {
                    DataContext = new CarrosViewModel(this)
                };
            });

            ShowClientesCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Clientes";
                CurrentView = new ClientesView
                {
                    DataContext = new ClientesViewModel()
                };
            });

            ShowEmpleadosCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Empleados";
                CurrentView = new EmpleadosView
                {
                    DataContext = new EmpleadosViewModel()
                };
            });

            ShowGestionUsuariosCommand = new RelayCommand(_ =>
            {
                if (!EsAdministrador)
                    return;

                CurrentView = new GestionUsuarioView();
            });

            ShowVentaCommand = new RelayCommand(_ =>
            {
                if (Carrito.Count > 0)
                {
                    VistaActiva = "Venta";
                    CurrentView = new PuntoVentaView
                    {
                        DataContext = new PuntoVentaViewModel(this)
                    };
                }
            });

            CerrarSesionCommand = new RelayCommand(_ =>
            {
                SesionUsuario.UsuarioActual = null;
                SesionUsuario.ModoAdministrador = false;
                new LoginView().Show();
                Application.Current.Windows[0]?.Close();
            });

            CambiarFotoPerfilCommand = new RelayCommand(_ => CambiarFotoPerfil());

            FotoPerfil = CargarImagen(SesionUsuario.UsuarioActual?.FotoPerfil);

            SesionUsuario.FotoPerfilActualizada += ruta =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FotoPerfil = CargarImagen(ruta);
                });
            };

            VistaActiva = "Carros";
            CurrentView = new CarrosView
            {
                DataContext = new CarrosViewModel(this)
            };
        }

        private static string ObtenerUsuarioDesdeCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return string.Empty;

            var at = correo.IndexOf('@');
            if (at <= 0)
                return string.Empty;

            return correo.Substring(0, at).Trim();
        }
    }
}
