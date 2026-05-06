using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcesionaroCarros.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private const string ReleaseChannelFallback = "LOCAL";
        private static readonly TimeSpan ReleaseChannelRefreshInterval = TimeSpan.FromSeconds(3);

        private static readonly HashSet<string> AllowedLogViewerEmails =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "wandica@weg.net",
                "maicolj@weg.net"
            };

        private object _currentView;
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private readonly LocalizationService _localizationService = LocalizationService.Instance;
        private readonly DispatcherTimer _releaseChannelRefreshTimer;
        private readonly string _nombreVisibleDispositivo;
        private readonly string _correoDispositivo;
        private string _releaseChannelLabel;

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
        public bool EsSuperAdmin => SesionUsuario.EsSuperAdmin;
        public bool PuedeVerLogs =>
            SesionUsuario.EsDeveloper &&
            AllowedLogViewerEmails.Contains((SesionUsuario.UsuarioActual?.Correo ?? string.Empty).Trim());
        public string ReleaseChannelLabel => _releaseChannelLabel;
        public string DeveloperAccountsLabel => LocalizedText.Get("Shell_DeveloperAccountsLabel", "Developers");
        public string DeveloperAccountsTooltip => LocalizedText.Get("Shell_DeveloperAccountsTooltip", "Gestión de developers (solo Super Admin).");
        public ReadOnlyObservableCollection<LocalizationService.LanguageOption> AvailableLanguages { get; }

        public LocalizationService.LanguageOption SelectedLanguage
        {
            get => _localizationService.SelectedLanguage;
            set => _localizationService.SelectedLanguage = value;
        }

        public ICommand CerrarSesionCommand { get; }
        public ICommand ShowInstaladoresCommand { get; }
        public ICommand ShowGestionUsuariosCommand { get; }
        public ICommand ShowLogsCommand { get; }
        public ICommand ShowAyudaCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowDeveloperAccountsCommand { get; }

        public MainViewModel()
        {
            AvailableLanguages = _localizationService.AvailableLanguages;
            _releaseChannelLabel = ResolveReleaseChannelLabel();
            _releaseChannelRefreshTimer = new DispatcherTimer
            {
                Interval = ReleaseChannelRefreshInterval
            };
            _releaseChannelRefreshTimer.Tick += ReleaseChannelRefreshTimer_Tick;
            _releaseChannelRefreshTimer.Start();

            _nombreVisibleDispositivo = WindowsProfileService.ObtenerNombreVisible();
            _correoDispositivo = WindowsProfileService.ObtenerCorreoPrincipal();
            if (string.IsNullOrWhiteSpace(_correoDispositivo))
            {
                _correoDispositivo = _usuariosDb.ObtenerCorreoPorUsuarioDispositivo(
                    Environment.UserName ?? string.Empty,
                    _nombreVisibleDispositivo);
            }

            PropertyChangedEventManager.AddHandler(_localizationService, OnLocalizationPropertyChanged, nameof(LocalizationService.SelectedLanguage));

            ShowInstaladoresCommand = new RelayCommand(_ => MostrarInstaladores());

            ShowGestionUsuariosCommand = new RelayCommand(_ =>
            {
                if (!EsAdministrador)
                    return;

                LogService.Info("MainWindow", "Navegacion a gestion de usuarios");
                VistaActiva = "Usuarios";
                CurrentView = new GestionUsuarioView();
            });

            ShowAyudaCommand = new RelayCommand(_ =>
            {
                LogService.Info("MainWindow", "Navegacion a ayuda");
                VistaActiva = "Ayuda";
                CurrentView = new HelpView
                {
                    DataContext = new HelpViewModel(SesionUsuario.PerfilPrivilegiado)
                };
            });

            ShowLogsCommand = new RelayCommand(_ =>
            {
                if (!PuedeVerLogs)
                    return;

                LogService.Info("MainWindow", "Navegacion a centro de logs");
                VistaActiva = "Logs";
                CurrentView = new LogsView
                {
                    DataContext = new LogsViewModel()
                };
            });

            ShowDeveloperAccountsCommand = new RelayCommand(_ =>
            {
                if (!EsSuperAdmin)
                    return;

                LogService.Info("MainWindow", "Navegacion a gestion de developers");
                VistaActiva = "Developers";
                CurrentView = new DeveloperAccountsView
                {
                    DataContext = new DeveloperAccountsViewModel()
                };
            });

            ShowSettingsCommand = new RelayCommand(_ =>
            {
                LogService.Info("MainWindow", "Navegacion a configuracion");
                VistaActiva = "Configuracion";
                CurrentView = new SettingsView
                {
                    DataContext = new SettingsViewModel()
                };
            });

            CerrarSesionCommand = new RelayCommand(_ =>
            {
                LogService.Info("MainWindow", "Cierre de sesion solicitado");
                SesionUsuario.UsuarioActual = null;
                SesionUsuario.ModoAdministrador = false;
                new LoginView().Show();
                Application.Current.Windows[0]?.Close();
            });

            MostrarInstaladores();
        }

        public void Dispose()
        {
            _releaseChannelRefreshTimer.Stop();
            _releaseChannelRefreshTimer.Tick -= ReleaseChannelRefreshTimer_Tick;
            PropertyChangedEventManager.RemoveHandler(_localizationService, OnLocalizationPropertyChanged, nameof(LocalizationService.SelectedLanguage));
        }

        private void OnLocalizationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SelectedLanguage));
        }

        private void ReleaseChannelRefreshTimer_Tick(object sender, EventArgs e)
        {
            var currentLabel = ResolveReleaseChannelLabel();
            if (string.Equals(_releaseChannelLabel, currentLabel, StringComparison.Ordinal))
                return;

            _releaseChannelLabel = currentLabel;
            OnPropertyChanged(nameof(ReleaseChannelLabel));
        }

        private static string ResolveReleaseChannelLabel()
        {
            return GitBranchService.GetCurrentBranchLabel(ReleaseChannelFallback);
        }

        private void MostrarInstaladores()
        {
            LogService.Info("MainWindow", "Navegacion a instaladores");
            VistaActiva = "Instaladores";
            CurrentView = new InstaladoresView
            {
                DataContext = new InstaladoresViewModel(this)
            };
        }

        private static string ObtenerUsuarioDesdeCorreo(string correo) {
            if (string.IsNullOrWhiteSpace(correo))
                return string.Empty;

            var at = correo.IndexOf('@');
            if (at <= 0)
                return string.Empty;

            return correo.Substring(0, at).Trim();
        }

    }
}
