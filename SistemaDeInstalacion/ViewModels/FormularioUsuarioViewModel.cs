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
                    LogService.Warning("FormularioUsuario", "Intento de guardar usuario con campos incompletos", ConstruirDetalleUsuario());
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
                LogService.Warning("FormularioUsuario", "No se pudo guardar usuario por base ocupada", ConstruirDetalleUsuario());
                MessageBox.Show(
                    "La base de datos esta ocupada. Intente guardar nuevamente.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex)
            {
                LogService.Warning("FormularioUsuario", "Validacion de usuario rechazada", ConstruirDetalleUsuario() + " | " + ex.Message);
                MessageBox.Show(
                    ex.Message,
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                LogService.Error("FormularioUsuario", "Error al guardar usuario", ex, ConstruirDetalleUsuario());
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
            LogService.Info("FormularioUsuario", "Usuario actualizado", ConstruirDetalleUsuario(_usuarioEditando));

            MessageBox.Show("Usuario actualizado");
            _window.Close();
        }

        private void GuardarNuevo(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                LogService.Warning("FormularioUsuario", "Intento de crear usuario sin contrasena", ConstruirDetalleUsuario());
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
                LogService.Warning("FormularioUsuario", "No se pudo crear usuario por correo duplicado o bloqueo", ConstruirDetalleUsuario(usuario));
                MessageBox.Show("Correo ya registrado o base de datos ocupada.");
                return;
            }

            LogService.Info("FormularioUsuario", "Usuario creado correctamente", ConstruirDetalleUsuario(usuario));
            MessageBox.Show("Usuario creado correctamente");
            _window.Close();
        }

        private string ConstruirDetalleUsuario()
        {
            return ConstruirDetalleUsuario(new Usuario
            {
                Id = _usuarioEditando?.Id ?? 0,
                Nombres = Nombres,
                Apellidos = Apellidos,
                Correo = Correo,
                Telefono = Telefono,
                Rol = Rol
            });
        }

        private static string ConstruirDetalleUsuario(Usuario usuario)
        {
            if (usuario == null)
                return "Sin usuario";

            return $"Id={usuario.Id}; Correo={usuario.Correo}; Rol={usuario.Rol}; Nombre={(usuario.Nombres + " " + usuario.Apellidos).Trim()}";
        }
    }
}
