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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            GlobalCopyContextService.Register();
            ThemeManager.Initialize(this);

            LogService.Info("App", "Inicio de aplicacion");

            var stopwatch = Stopwatch.StartNew();
            DatabaseInitializer.Initialize();
            stopwatch.Stop();
            LogService.Latency(
                "App",
                "Inicializacion de base de datos completada",
                stopwatch.ElapsedMilliseconds,
                DatabaseInitializer.CurrentDbPath);

            var login = new LoginView();
            login.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.Error("App", "Excepcion no controlada en interfaz", e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogService.Error("App", "Excepcion no controlada del dominio", e.ExceptionObject as Exception);
        }
    }
}
