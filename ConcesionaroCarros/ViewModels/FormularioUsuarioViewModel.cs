using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace ConcesionaroCarros.ViewModels
{
    public class FormularioUsuarioViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private readonly EmpleadosDbService _empleadosDb = new EmpleadosDbService();

        private readonly Window _window;
        private readonly Usuario _usuarioEditando;

        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Rol { get; set; }
        public bool PuedeEditarRol { get; set; } = true;

        public ObservableCollection<string> Roles { get; } =
            new ObservableCollection<string>(RolesSistema.Todos);

        public FormularioUsuarioViewModel(Window window, Usuario usuario = null)
        {
            _window = window;
            _usuarioEditando = usuario;

            if (usuario == null)
                return;

            Nombres = usuario.Nombres;
            Apellidos = usuario.Apellidos;
            Correo = usuario.Correo;
            Telefono = usuario.Telefono;
            Rol = usuario.Rol;
            PuedeEditarRol = !RolesSistema.EsAdministrador(usuario.Rol);
        }

        public void Guardar(string password)
        {
            try
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
                    GuardarEdicion(password);
                    return;
                }

                GuardarNuevo(password);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
            {
                MessageBox.Show(
                    "La base de datos esta ocupada. Intente guardar nuevamente.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
                    "No fue posible guardar el usuario.\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GuardarEdicion(string password)
        {
            _usuarioEditando.Nombres = Nombres;
            _usuarioEditando.Apellidos = Apellidos;
            _usuarioEditando.Correo = Correo;
            _usuarioEditando.Telefono = Telefono;
            _usuarioEditando.Rol = Rol;

            if (!string.IsNullOrWhiteSpace(password))
                _usuariosDb.ActualizarPassword(_usuarioEditando.Id, password);

            _usuariosDb.Actualizar(_usuarioEditando);

            MessageBox.Show("Usuario actualizado");
            _window.Close();
        }

        private void GuardarNuevo(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Ingrese contrasena");
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
                MessageBox.Show("Correo ya registrado o base de datos ocupada.");
                return;
            }

            _empleadosDb.Insertar(new Empleado
            {
                Nombres = Nombres,
                Apellidos = Apellidos,
                Correo = Correo,
                Telefono = Telefono,
                Cargo = Rol,
                Activo = true
            });

            MessageBox.Show("Usuario creado correctamente");
            _window.Close();
        }
    }
}
