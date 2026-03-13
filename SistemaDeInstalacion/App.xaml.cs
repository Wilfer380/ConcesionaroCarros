using ConcesionaroCarros.Db;
using ConcesionaroCarros.Views;
using System.Windows;

namespace ConcesionaroCarros
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DatabaseInitializer.Initialize();

            var login = new LoginView();
            login.Show();
        }
    }
}
