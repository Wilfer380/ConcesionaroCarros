using ConcesionaroCarros.Views;
using System.Windows;

namespace ConcesionaroCarros
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 🔐 SIEMPRE EMPEZAMOS POR LOGIN
            var login = new LoginView();
            login.Show();
        }
    }
}
