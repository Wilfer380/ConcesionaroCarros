using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace ConcesionaroCarros.ViewModels
{
    public class FormularioUsuarioViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private readonly ClientesDbService _clientesDb = new ClientesDbService();
        private readonly EmpleadosDbService _empleadosDb = new EmpleadosDbService();

        private readonly Window _window;
        private Usuario _usuarioEditando;

        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Rol { get; set; }

        public ObservableCollection<string> Roles { get; } =
            new ObservableCollection<string> { "CLIENTE", "EMPLEADO" };

        public FormularioUsuarioViewModel(Window window, Usuario usuario = null)
        {
            _window = window;
            _usuarioEditando = usuario;

            if (usuario != null)
            {
                Nombres = usuario.Nombres;
                Apellidos = usuario.Apellidos;
                Correo = usuario.Correo;
                Telefono = usuario.Telefono;
                Rol = usuario.Rol;
            }
        }

       
        public void Guardar(string password)
        {
            if (string.IsNullOrWhiteSpace(Nombres) ||
                string.IsNullOrWhiteSpace(Correo) ||
                string.IsNullOrWhiteSpace(Rol))
            {
                MessageBox.Show("Complete todos los campos");
                return;
            }

            if (_usuarioEditando != null)
            {
                _usuarioEditando.Nombres = Nombres;
                _usuarioEditando.Apellidos = Apellidos;
                _usuarioEditando.Correo = Correo;
                _usuarioEditando.Telefono = Telefono;
                _usuarioEditando.Rol = Rol;

               
                if (!string.IsNullOrWhiteSpace(password))
                {
                    _usuariosDb.ActualizarPassword(_usuarioEditando.Id, password);
                }

                _usuariosDb.Actualizar(_usuarioEditando);

                MessageBox.Show("Usuario actualizado");
                _window.Close();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Ingrese contraseña");
                return;
            }

            var usuario = new Usuario
            {
                Nombres = Nombres,
                Apellidos = Apellidos,
                Correo = Correo,
                Telefono = Telefono,
                Rol = Rol
            };

            if (!_usuariosDb.Registrar(usuario, password))
            {
                MessageBox.Show("Correo ya registrado");
                return;
            }

            if (Rol == "CLIENTE")
            {
                _clientesDb.Insertar(new Cliente
                {
                    Nombres = Nombres,
                    Apellidos = Apellidos,
                    Correo = Correo,
                    Telefono = Telefono,
                    FechaRegistro = System.DateTime.Now
                });
            }

            if (Rol == "EMPLEADO")
            {
                _empleadosDb.Insertar(new Empleado
                {
                    Nombres = Nombres,
                    Apellidos = Apellidos,
                    Correo = Correo,
                    Telefono = Telefono,
                    Activo = true
                });
            }

            MessageBox.Show("Usuario creado correctamente");
            _window.Close();
        }
    }
}
