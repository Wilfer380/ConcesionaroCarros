using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _db = new UsuariosDbService();

        public string Correo { get; set; }
        public string Password { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand IrARegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Login());
            IrARegisterCommand = new RelayCommand(_ =>
            {
                new RegisterView().Show();
                Application.Current.Windows[0]?.Close();
            });
        }

        private void Login()
        {
            var usuario = _db.Login(Correo, Password);

            if (usuario == null)
            {
                MessageBox.Show("Correo o contraseña incorrectos", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SesionUsuario.UsuarioActual = usuario;

            new MainWindow().Show();
            Application.Current.Windows[0]?.Close();
        }
    }
}
