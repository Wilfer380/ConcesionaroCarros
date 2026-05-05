using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.Diagnostics;
using System.Windows;

namespace ConcesionaroCarros
{
    public partial class App : Application
    {
        private OperationalHealthService _operationalHealthService;

        public App()
        {
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            GlobalCopyContextService.Register();
            ThemeManager.Initialize(this);
            LocalizationService.Instance.Initialize();

            LogService.Info("AppLifecycle", "Inicio de aplicacion", "event=app_started");
            LogService.Health("AppLifecycle", "Aplicacion iniciada", "event=app_started");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                DatabaseInitializer.Initialize();
                stopwatch.Stop();
                LogService.Latency(
                    "App",
                    "Inicializacion de base de datos completada",
                    stopwatch.ElapsedMilliseconds,
                    DatabaseInitializer.CurrentDbPath);
                LogService.Health(
                    "AppLifecycle",
                    "Inicializacion principal completada",
                    "signal=startup|state=healthy|phase=database_init|db_path=" + DatabaseInitializer.CurrentDbPath + "|duration_ms=" + stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogService.Error("AppLifecycle", "Inicializacion principal fallo", ex, DatabaseInitializer.CurrentDbPath);
                LogService.Health(
                    "AppLifecycle",
                    "Inicializacion principal con falla observable",
                    "signal=startup|state=partial|phase=database_init|db_path=" + DatabaseInitializer.CurrentDbPath + "|duration_ms=" + stopwatch.ElapsedMilliseconds + "|reason=" + ex.GetType().Name);
                throw;
            }

            _operationalHealthService = new OperationalHealthService();
            _operationalHealthService.Start();

            var login = new LoginView();
            login.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Health(
                "AppHealth",
                "Heartbeat finalizado",
                "signal=heartbeat|state=stopped|reason=app_exit|exit_code=" + e.ApplicationExitCode);
            _operationalHealthService?.Dispose();
            LogService.Info("AppLifecycle", "Cierre de aplicacion", "event=app_closed|exit_code=" + e.ApplicationExitCode);
            LogService.Health("AppLifecycle", "Aplicacion finalizada", "event=app_closed|exit_code=" + e.ApplicationExitCode);
            base.OnExit(e);
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                LocalizationService.Instance.ApplyToWindow(window);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.Error("App", "Excepcion no controlada en interfaz", e.Exception);
            LogService.Health("AppHealth", "Excepcion no controlada en interfaz", "signal=exception|state=error|scope=ui|reason=" + e.Exception.GetType().Name);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            LogService.Error("App", "Excepcion no controlada del dominio", exception);
            LogService.Health("AppHealth", "Excepcion no controlada del dominio", "signal=exception|state=error|scope=app_domain|reason=" + (exception?.GetType().Name ?? "unknown"));
        }
    }
}
