using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.Windows;
using System.Windows.Input;

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
            }
        }

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
        public ICommand ShowInstaladoresCommand { get; }
        public ICommand ShowGestionUsuariosCommand { get; }
        public ICommand ShowAyudaCommand { get; }

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

            ShowInstaladoresCommand = new RelayCommand(_ => MostrarInstaladores());

            ShowGestionUsuariosCommand = new RelayCommand(_ =>
            {
                if (!EsAdministrador)
                    return;

                VistaActiva = "Usuarios";
                CurrentView = new GestionUsuarioView();
            });

            ShowAyudaCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Ayuda";
                CurrentView = new HelpView
                {
                    DataContext = new HelpViewModel(EsAdministrador)
                };
            });

            CerrarSesionCommand = new RelayCommand(_ =>
            {
                SesionUsuario.UsuarioActual = null;
                SesionUsuario.ModoAdministrador = false;
                new LoginView().Show();
                Application.Current.Windows[0]?.Close();
            });

            MostrarInstaladores();
        }

        private void MostrarInstaladores()
        {
            VistaActiva = "Instaladores";
            CurrentView = new InstaladoresView
            {
                DataContext = new InstaladoresViewModel(this)
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
