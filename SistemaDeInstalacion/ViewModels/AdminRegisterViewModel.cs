using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class AdminRegisterViewModel : BaseViewModel, ILocalizableViewModel
    {
        public string Correo { get; set; }
        public string Rol { get; set; }
        public string PasswordNormal { get; set; }
        public string PasswordAdmin { get; set; }
        public ObservableCollection<SelectionOption> RolesDisponibles { get; } =
            new ObservableCollection<SelectionOption>
            {
                new SelectionOption(null, "AdminRegister_RolePlaceholder"),
                new SelectionOption(RolesSistema.Administrador, "Role_Administrador")
            };

        public string WindowTitle => LocalizedText.Get("AdminRegister_WindowTitle", "WEG Installer Systems - Admin Register");
        public string BackLabel => LocalizedText.Get("AdminRegister_BackLabel", "< Back");
        public string EmailLabel => LocalizedText.Get("AdminRegister_EmailLabel", "Email");
        public string EmailPlaceholder => LocalizedText.Get("AdminRegister_EmailPlaceholder", "administrator@weg.net");
        public string RoleLabel => LocalizedText.Get("AdminRegister_RoleLabel", "Role");
        public string NormalPasswordLabel => LocalizedText.Get("AdminRegister_NormalPasswordLabel", "User password");
        public string AdminPasswordLabel => LocalizedText.Get("AdminRegister_AdminPasswordLabel", "Administrator password");
        public string RegisterAdminLabel => LocalizedText.Get("AdminRegister_RegisterButton", "Register administrator");

        public ICommand RegistrarAdminCommand { get; }
        public ICommand VolverLoginCommand { get; }

        public AdminRegisterViewModel()
        {
            RegistrarAdminCommand = new RelayCommand(_ => RegistrarAdmin());
            VolverLoginCommand = new RelayCommand(_ => VolverLogin());
        }

        private void RegistrarAdmin()
        {
            if (string.IsNullOrWhiteSpace(Correo) ||
                string.IsNullOrWhiteSpace(Rol) ||
                string.IsNullOrWhiteSpace(PasswordNormal) ||
                string.IsNullOrWhiteSpace(PasswordAdmin))
            {
                LogService.Warning("AdminRegister", "Intento de registro admin con datos incompletos");
                MessageBox.Show(LocalizedText.Get(
                    "AdminRegister_IncompleteDataMessage",
                    "Complete email, role, user password, and administrator password."));
                return;
            }

            var rolSeleccionado = (Rol ?? string.Empty).Trim().ToUpperInvariant();

            Correo = Correo.Trim();
            var usuarioLog = LogService.ResolveAuditUserName(Correo);
            if (!Correo.EndsWith("@weg.net", StringComparison.OrdinalIgnoreCase))
            {
                LogService.WarningForUser("AdminRegister", "Registro admin rechazado por dominio invalido", usuarioLog, Correo);
                MessageBox.Show(
                    LocalizedText.Get("AdminRegister_InvalidDomainMessage", "Email must end with @weg.net."),
                    LocalizedText.Get("Common_EmailValidationTitle", "Email validation"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var usuarioPc = Environment.UserName;
            if (string.IsNullOrWhiteSpace(usuarioPc))
            {
                LogService.WarningForUser("AdminRegister", "No se pudo detectar usuario del dispositivo", usuarioLog, Correo);
                MessageBox.Show(LocalizedText.Get("AdminRegister_DeviceUserMissingMessage", "Could not detect the device user."));
                return;
            }

            var usuarioCorreo = ObtenerUsuarioDesdeCorreo(Correo);
            if (string.IsNullOrWhiteSpace(usuarioCorreo))
            {
                LogService.WarningForUser("AdminRegister", "No fue posible derivar usuario desde el correo", usuarioLog, Correo);
                MessageBox.Show(
                    LocalizedText.Get("AdminRegister_InvalidDerivedUserMessage", "Could not derive a valid user from the email."),
                    LocalizedText.Get("Common_ValidationTitle", "Validation"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var nombreReal = WindowsProfileService.ObtenerNombreVisible();
            var usuariosDb = new UsuariosDbService();
            var adminsDb = new AdministradoresDbService();
            var nombres = string.Empty;
            var apellidos = string.Empty;
            var usuarioSistemaLogin = usuarioCorreo;

            var correoAsociadoAlPc = WindowsProfileService.ObtenerCorreoPrincipal();
            var correoEsDelPc =
                (!string.IsNullOrWhiteSpace(correoAsociadoAlPc) &&
                 string.Equals(correoAsociadoAlPc.Trim(), Correo, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(usuarioCorreo, usuarioPc, StringComparison.OrdinalIgnoreCase);

            if (correoEsDelPc)  
            {
                usuarioSistemaLogin = usuarioPc;
                ConstruirNombreDesdeWindows(nombreReal, usuarioPc, out nombres, out apellidos);
            }
            else
            {
                ConstruirNombreDesdeCorreo(Correo, out nombres, out apellidos);
            }

            try
            {
                var existente = usuariosDb.ObtenerPorCorreo(Correo);
                if (existente == null)
                {
                    var nuevo = new Usuario
                    {
                        Nombres = nombres,
                        Apellidos = apellidos,
                        Correo = Correo,
                        Telefono = string.Empty,
                        Rol = rolSeleccionado
                    };

                    usuariosDb.RegistrarYRetornarId(nuevo, PasswordNormal);
                }
                else
                {
                    existente.Nombres = nombres;
                    existente.Apellidos = apellidos;
                    existente.Rol = rolSeleccionado;
                    usuariosDb.Actualizar(existente);
                    usuariosDb.ActualizarPassword(existente.Id, PasswordNormal);
                }

                adminsDb.GuardarOActualizar(new Administrador
                {
                    Nombres = nombres,
                    Apellidos = apellidos,
                    Correo = Correo,
                    UsuarioSistema = usuarioSistemaLogin,
                    Rol = rolSeleccionado
                }, PasswordAdmin);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                LogService.WarningForUser("AdminRegister", "Registro admin rechazado por duplicado", usuarioLog, Correo);
                MessageBox.Show(
                    LocalizedText.Get("AdminRegister_AlreadyRegisteredMessage", "An administrator is already registered with that email."),
                    LocalizedText.Get("AdminRegister_ExistingRegistrationTitle", "Existing registration"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            catch (Exception ex)
            {
                LogService.ErrorForUser("AdminRegister", "Error al registrar administrador", usuarioLog, ex, $"{Correo}; Rol={rolSeleccionado}");
                MessageBox.Show(
                    LocalizedText.Get("AdminRegister_RegisterErrorMessage", "Could not complete the administrator registration right now."),
                    LocalizedText.Get("AdminRegister_RegisterErrorTitle", "Registration error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LogService.InfoForUser("AdminRegister", "Administrador registrado correctamente", usuarioLog, $"{Correo}; Rol={rolSeleccionado}; UsuarioSistema={usuarioSistemaLogin}");
            MessageBox.Show(
                LocalizedText.Get("AdminRegister_RegisterSuccessMessage", "Administrator registered successfully."),
                LocalizedText.Get("AdminRegister_RegisterSuccessTitle", "Successful registration"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            new AdminLoginView(usuarioSistemaLogin, PasswordAdmin).Show();
            CerrarVentanaActual();
        }

        private void VolverLogin()
        {
            new AdminLoginView().Show();
            CerrarVentanaActual();
        }

        private void CerrarVentanaActual()
        {
            var current = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => ReferenceEquals(w.DataContext, this));

            current?.Close();
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
                apellidos = string.Empty;
            }
            else if (partes.Length == 2)
            {
                nombres = string.Join(" ", partes, 0, 2);
                apellidos = string.Empty;
            }
            else
            {
                nombres = string.Join(" ", partes, 0, 2);
                apellidos = string.Join(" ", partes, 2, partes.Length - 2);
            }
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

        public override void RefreshLocalization()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(BackLabel));
            OnPropertyChanged(nameof(EmailLabel));
            OnPropertyChanged(nameof(EmailPlaceholder));
            OnPropertyChanged(nameof(RoleLabel));
            OnPropertyChanged(nameof(NormalPasswordLabel));
            OnPropertyChanged(nameof(AdminPasswordLabel));
            OnPropertyChanged(nameof(RegisterAdminLabel));

            foreach (var option in RolesDisponibles)
                option.RefreshLocalization();
        }

        public sealed class SelectionOption : BaseViewModel
        {
            private readonly string _resourceKey;

            public SelectionOption(string value, string resourceKey)
            {
                Value = value;
                _resourceKey = resourceKey;
            }

            public string Value { get; }
            public string DisplayName => LocalizedText.Get(_resourceKey, Value ?? string.Empty);

            public override void RefreshLocalization()
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }
}
