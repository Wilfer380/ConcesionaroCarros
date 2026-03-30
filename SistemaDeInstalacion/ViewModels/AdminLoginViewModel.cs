using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class AdminLoginViewModel : BaseViewModel
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("ConcesionaroCarros.AdminLogin.v1");

        private readonly AdministradoresDbService _adminsDb = new AdministradoresDbService();
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private readonly string _rememberPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ConcesionaroCarros",
                "login.admin.remember");

        public string Usuario { get; set; }
        public string PasswordAdmin { get; set; }
        public bool Recordarme { get; set; }

        public ICommand LoginAdminCommand { get; }
        public ICommand IrRegistroAdminCommand { get; }
        public ICommand VolverLoginNormalCommand { get; }

        public AdminLoginViewModel(string usuarioPrefill = null, string passwordAdminPrefill = null)
        {
            LoginAdminCommand = new RelayCommand(_ => LoginAdmin());
            IrRegistroAdminCommand = new RelayCommand(_ => IrRegistroAdmin());
            VolverLoginNormalCommand = new RelayCommand(_ => VolverLoginNormal());

            Usuario = Environment.UserName ?? string.Empty;
            PasswordAdmin = string.Empty;

            if (!string.IsNullOrWhiteSpace(usuarioPrefill) || !string.IsNullOrWhiteSpace(passwordAdminPrefill))
            {
                Usuario = string.IsNullOrWhiteSpace(usuarioPrefill)
                    ? (Environment.UserName ?? string.Empty)
                    : usuarioPrefill;
                PasswordAdmin = passwordAdminPrefill ?? string.Empty;
                Recordarme = false;
                return;
            }

            CargarRecordado();
        }

        private void LoginAdmin()
        {
            var stopwatch = Stopwatch.StartNew();
            var usuarioIngreso = (Usuario ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(usuarioIngreso))
            {
                LogService.Warning("AdminLogin", "Intento de login admin sin usuario");
                MessageBox.Show(
                    "Debe ingresar el usuario de administrador.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var usuarioLog = ResolveLogUserName(usuarioIngreso, Environment.UserName ?? string.Empty, WindowsProfileService.ObtenerCorreoPrincipal());
            LogService.InfoForUser("AdminLogin", "Intento de login admin", usuarioLog, BuildLoginDetail(usuarioLog, usuarioIngreso));

            if (!_adminsDb.ExistePorUsuarioSistema(usuarioIngreso))
            {
                stopwatch.Stop();
                LogService.WarningForUser("AdminLogin", "Administrador no registrado", usuarioLog, BuildLoginDetail(usuarioLog, usuarioIngreso));
                LogService.LatencyForUser("AdminLogin", "Login admin rechazado por usuario no registrado", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, usuarioIngreso));
                MessageBox.Show(
                    "Este usuario administrador no esta registrado. Debe registrarse primero.",
                    "Registro requerido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var admin = _adminsDb.LoginPorUsuarioSistema(usuarioIngreso, PasswordAdmin);
            if (admin == null)
            {
                stopwatch.Stop();
                LogService.WarningForUser("AdminLogin", "Credenciales de administrador invalidas", usuarioLog, BuildLoginDetail(usuarioLog, usuarioIngreso));
                LogService.LatencyForUser("AdminLogin", "Login admin rechazado por credenciales invalidas", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, usuarioIngreso));
                MessageBox.Show(
                    "Credenciales de administrador incorrectas.",
                    "Acceso denegado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var usuarioNormal = _usuariosDb.ObtenerPorCorreo(admin.Correo);
            if (usuarioNormal == null)
            {
                stopwatch.Stop();
                usuarioLog = ResolveLogUserName(admin.Correo, Environment.UserName ?? string.Empty, WindowsProfileService.ObtenerCorreoPrincipal());
                LogService.WarningForUser("AdminLogin", "Administrador sin usuario base asociado", usuarioLog, BuildLoginDetail(usuarioLog, admin.Correo));
                LogService.LatencyForUser("AdminLogin", "Login admin rechazado por usuario base inexistente", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, admin.Correo));
                MessageBox.Show(
                    "No existe usuario base asociado a este administrador.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!RolesSistema.EsAdministrador(usuarioNormal.Rol))
                usuarioNormal.Rol = RolesSistema.Administrador;

            if (Recordarme)
                GuardarRecordado();
            else
                LimpiarRecordado();

            stopwatch.Stop();
            usuarioLog = ResolveLogUserName(admin.Correo, Environment.UserName ?? string.Empty, WindowsProfileService.ObtenerCorreoPrincipal());
            LogService.InfoForUser("AdminLogin", "Login admin exitoso", usuarioLog, BuildLoginDetail(usuarioLog, admin.Correo));
            LogService.LatencyForUser("AdminLogin", "Login admin exitoso", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, admin.Correo));
            SesionUsuario.UsuarioActual = usuarioNormal;
            SesionUsuario.ModoAdministrador = true;
            new MainWindow().Show();
            CerrarVentanaActual();
        }

        private void IrRegistroAdmin()
        {
            new AdminRegisterView().Show();
            CerrarVentanaActual();
        }

        private void VolverLoginNormal()
        {
            new LoginView().Show();
            CerrarVentanaActual();
        }

        private void CargarRecordado()
        {
            try
            {
                if (!File.Exists(_rememberPath))
                    return;

                var lineas = File.ReadAllLines(_rememberPath);
                if (lineas.Length < 2)
                    return;

                Usuario = lineas[0] ?? string.Empty;
                PasswordAdmin = Descifrar(lineas[1]);
                Recordarme = true;
            }
            catch
            {
                Usuario = string.Empty;
                PasswordAdmin = string.Empty;
                Recordarme = false;
            }
        }

        private void GuardarRecordado()
        {
            try
            {
                var carpeta = Path.GetDirectoryName(_rememberPath);
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                File.WriteAllLines(_rememberPath, new[]
                {
                    Usuario ?? string.Empty,
                    Cifrar(PasswordAdmin ?? string.Empty)
                });
            }
            catch
            {
                // No bloqueamos el login por fallo de persistencia.
            }
        }

        private void LimpiarRecordado()
        {
            try
            {
                if (File.Exists(_rememberPath))
                    File.Delete(_rememberPath);
            }
            catch
            {
                // No bloqueamos el login por fallo de limpieza.
            }
        }

        private static string Cifrar(string textoPlano)
        {
            var datos = Encoding.UTF8.GetBytes(textoPlano ?? string.Empty);
            var cifrado = ProtectedData.Protect(datos, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cifrado);
        }

        private static string Descifrar(string textoCifrado)
        {
            if (string.IsNullOrWhiteSpace(textoCifrado))
                return string.Empty;

            var cifrado = Convert.FromBase64String(textoCifrado);
            var datos = ProtectedData.Unprotect(cifrado, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(datos);
        }

        private void CerrarVentanaActual()
        {
            var current = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => ReferenceEquals(w.DataContext, this));

            current?.Close();
        }

        private static string ResolveLogUserName(string loginValue, string deviceUserName, string deviceEmail)
        {
            var candidate = (loginValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(candidate))
                return deviceUserName ?? string.Empty;

            if (candidate.Contains("@"))
            {
                if (!string.IsNullOrWhiteSpace(deviceEmail) &&
                    string.Equals(candidate, deviceEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return deviceUserName ?? string.Empty;
                }

                var at = candidate.IndexOf('@');
                return at > 0 ? candidate.Substring(0, at).Trim() : candidate;
            }

            return candidate;
        }

        private static string BuildLoginDetail(string userName, string emailOrLogin)
        {
            return string.Format(
                "Usuario={0}; Correo={1}",
                (userName ?? string.Empty).Trim(),
                (emailOrLogin ?? string.Empty).Trim());
        }
    }
}
