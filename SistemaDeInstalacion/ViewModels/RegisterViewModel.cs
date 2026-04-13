using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        public string Correo { get; set; }
        public string Password { get; set; }

        public ICommand RegistrarCommand { get; }
        public ICommand VolverLoginCommand { get; }

        public RegisterViewModel()
        {
            RegistrarCommand = new RelayCommand(_ => Registrar());
            VolverLoginCommand = new RelayCommand(_ => VolverLogin());
        }

        private void Registrar()
        {
            if (string.IsNullOrWhiteSpace(Correo) || string.IsNullOrWhiteSpace(Password))
            {
                LogService.Warning("Register", "Intento de registro sin credenciales completas");
                MessageBox.Show(LocalizedText.Get("Register_IncompleteDataMessage", "Debe ingresar correo y contrasena."));
                return;
            }

            Correo = Correo.Trim();
            var usuarioLog = LogService.ResolveAuditUserName(Correo);
            if (!Correo.EndsWith("@weg.net", StringComparison.OrdinalIgnoreCase))
            {
                LogService.WarningForUser("Register", "Registro rechazado por dominio invalido", usuarioLog, Correo);
                MessageBox.Show(
                    LocalizedText.Get("Register_InvalidDomainMessage", "El correo debe terminar en @weg.net."),
                    LocalizedText.Get("Common_EmailValidationTitle", "Validacion de correo"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var usuarioPc = Environment.UserName;
            if (string.IsNullOrWhiteSpace(usuarioPc))
            {
                LogService.WarningForUser("Register", "No se pudo detectar el usuario del dispositivo", usuarioLog, Correo);
                MessageBox.Show(LocalizedText.Get("Register_DeviceUserValidationMessage", "No se pudo validar el usuario del dispositivo."));
                return;
            }

            var nombreReal = WindowsProfileService.ObtenerNombreVisible();
            var correoDispositivo = WindowsProfileService.ObtenerCorreoPrincipal();
            var usuarioCorreo = ObtenerUsuarioDesdeCorreo(Correo);
            var nombres = string.Empty;
            var apellidos = string.Empty;
            var usuarioLogin = usuarioCorreo;

            var correoEsDelPc =
                (!string.IsNullOrWhiteSpace(correoDispositivo) &&
                 string.Equals(correoDispositivo, Correo, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(usuarioCorreo, usuarioPc, StringComparison.OrdinalIgnoreCase);

            if (correoEsDelPc)
            {
                usuarioLogin = usuarioPc;
                ConstruirNombreDesdeWindows(nombreReal, usuarioPc, out nombres, out apellidos);
            }
            else
            {
                ConstruirNombreDesdeCorreo(Correo, out nombres, out apellidos);
            }

            var usuario = new Usuario
            {
                Nombres = nombres,
                Apellidos = apellidos,
                Correo = Correo,
                Telefono = "",
                Rol = RolesSistema.Ventas
            };

            var usuarios = new UsuariosDbService();

            var existeCorreo = false;
            try
            {
                existeCorreo = usuarios
                    .ObtenerTodos()
                    .Any(u => string.Equals(u.Correo, Correo, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // Si falla la validacion previa, dejamos que el INSERT valide con UNIQUE.
            }

            if (existeCorreo)
            {
                LogService.WarningForUser("Register", "Registro rechazado por correo existente", usuarioLog, Correo);
                MessageBox.Show(
                    LocalizedText.Get("Register_ExistingUserMessage", "El usuario ya se encuentra registrado con ese correo."),
                    LocalizedText.Get("Register_ExistingUserTitle", "Registro existente"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                usuarios.RegistrarYRetornarId(usuario, Password);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                LogService.WarningForUser("Register", "Registro rechazado por correo duplicado", usuarioLog, Correo);
                MessageBox.Show(
                    LocalizedText.Get("Register_ExistingUserMessage", "El usuario ya se encuentra registrado con ese correo."),
                    LocalizedText.Get("Register_ExistingUserTitle", "Registro existente"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            catch (Exception ex)
            {
                LogService.ErrorForUser("Register", "Error al registrar usuario", usuarioLog, ex, Correo);
                MessageBox.Show(
                    LocalizedText.Get("Register_ErrorMessage", "No fue posible completar el registro en este momento. Intente nuevamente."),
                    LocalizedText.Get("Register_ErrorTitle", "Error de registro"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LogService.InfoForUser("Register", "Usuario registrado correctamente", usuarioLog, Correo);
            MessageBox.Show(LocalizedText.Get("Register_SuccessMessage", "Registro exitoso"));

            // Se precargan credenciales solo para este paso.
            // El usuario debe marcar "Recordarme" en login para persistirlas.
            if (string.IsNullOrWhiteSpace(usuarioLogin))
                usuarioLogin = usuarioPc;

            new LoginView(usuarioLogin, Password).Show();
            Application.Current.Windows[0]?.Close();
        }

        private void VolverLogin()
        {
            new LoginView().Show();
            Application.Current.Windows[0]?.Close();
        }

        private static string ObtenerUsuarioDesdeCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return string.Empty;

            var trimmed = correo.Trim();
            var at = trimmed.IndexOf('@');
            if (at <= 0)
                return string.Empty;

            return trimmed.Substring(0, at).Trim();
        }

        private static void ConstruirNombreDesdeWindows(
            string nombreVisible,
            string usuarioPc,
            out string nombres,
            out string apellidos)
        {
            nombres = usuarioPc ?? string.Empty;
            apellidos = string.Empty;

            if (string.IsNullOrWhiteSpace(nombreVisible))
                return;

            var partes = nombreVisible
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 1)
            {
                nombres = partes[0];
                return;
            }

            if (partes.Length == 2)
            {
                nombres = string.Join(" ", partes, 0, 2);
                return;
            }

            nombres = string.Join(" ", partes, 0, 2);
            apellidos = string.Join(" ", partes, 2, partes.Length - 2);
        }

        private static void ConstruirNombreDesdeCorreo(
            string correo,
            out string nombres,
            out string apellidos)
        {
            var usuarioCorreo = ObtenerUsuarioDesdeCorreo(correo)
                .Replace(".", " ")
                .Replace("_", " ")
                .Replace("-", " ");

            var partes = usuarioCorreo
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 0)
            {
                nombres = LocalizedText.Get("Common_DefaultUserName", "Usuario");
                apellidos = string.Empty;
                return;
            }

            if (partes.Length == 1)
            {
                nombres = partes[0];
                apellidos = string.Empty;
                return;
            }

            if (partes.Length == 2)
            {
                nombres = partes[0];
                apellidos = partes[1];
                return;
            }

            nombres = string.Join(" ", partes, 0, 2);
            apellidos = string.Join(" ", partes, 2, partes.Length - 2);
        }
    }
}
