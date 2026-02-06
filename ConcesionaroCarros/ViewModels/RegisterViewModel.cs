using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _db = new UsuariosDbService();

        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public ICommand RegistrarCommand { get; }

        public RegisterViewModel()
        {
            RegistrarCommand = new RelayCommand(_ => Registrar());
        }

        private void Registrar()
        {
            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden");
                return;
            }

            var usuario = new Usuario
            {
                Nombres = Nombres,
                Apellidos = Apellidos,
                Correo = Correo,
                Telefono = Telefono,
                Rol = "CLIENTE"
            };

            var usuarios = new UsuariosDbService();
            var clientes = new ClientesDbService();

            usuarios.RegistrarYRetornarId(usuario, Password);
            clientes.InsertarDesdeUsuario(usuario);

            MessageBox.Show("Registro exitoso");

            new LoginView().Show();
            Application.Current.Windows[0]?.Close();
        }

    }
}
