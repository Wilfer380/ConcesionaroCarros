using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("ConcesionaroCarros.Login.v1");

        private readonly UsuariosDbService _db = new UsuariosDbService();
        private readonly string _credencialesPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ConcesionaroCarros",
                "login.remember");

        public string Usuario { get; set; }
        public string Password { get; set; }
        public bool Recordarme { get; set; }

        public ICommand LoginCommand { get; }
        public ICommand IrARegisterCommand { get; }

        public LoginViewModel(string usuarioPrefill = null, string passwordPrefill = null)
        {
            LoginCommand = new RelayCommand(_ => Login());
            IrARegisterCommand = new RelayCommand(_ =>
            {
                new RegisterView().Show();
                Application.Current.Windows[0]?.Close();
            });

            Usuario = string.Empty;
            Password = string.Empty;

            var hayPrefill = !string.IsNullOrWhiteSpace(usuarioPrefill) ||
                             !string.IsNullOrWhiteSpace(passwordPrefill);

            if (hayPrefill)
            {
                Usuario = usuarioPrefill ?? string.Empty;
                Password = passwordPrefill ?? string.Empty;
                Recordarme = false;
                return;
            }

            CargarCredencialesGuardadas();
        }

        private void Login()
        {
            var stopwatch = Stopwatch.StartNew();
            var usuarioIngreso = (Usuario ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(usuarioIngreso))
            {
                LogService.Warning("Login", "Intento de login sin usuario");
                MessageBox.Show("Debe ingresar su usuario o correo.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var usuarioLog = ResolveLogUserName(usuarioIngreso, Environment.UserName ?? string.Empty, WindowsProfileService.ObtenerCorreoPrincipal());
            LogService.InfoForUser("Login", "Intento de login usuario", usuarioLog, BuildLoginDetail(usuarioLog, usuarioIngreso));

            var usuarioPc = Environment.UserName ?? string.Empty;
            var nombreVisible = WindowsProfileService.ObtenerNombreVisible();
            var correoPrincipalDispositivo = WindowsProfileService.ObtenerCorreoPrincipal();
            var correoLogin =
                string.Equals(usuarioIngreso, usuarioPc, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(correoPrincipalDispositivo)
                    ? _db.ObtenerCorreoPorUsuarioLogin(correoPrincipalDispositivo, usuarioPc, nombreVisible)
                    : _db.ObtenerCorreoPorUsuarioLogin(usuarioIngreso, usuarioPc, nombreVisible);
            if (string.IsNullOrWhiteSpace(correoLogin))
            {
                stopwatch.Stop();
                LogService.WarningForUser("Login", "Usuario no registrado", usuarioLog, BuildLoginDetail(usuarioLog, usuarioIngreso));
                LogService.LatencyForUser("Login", "Login rechazado por usuario no registrado", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, usuarioIngreso));
                MessageBox.Show("El usuario o correo ingresado no se encuentra registrado. Verifique el dato e intente nuevamente.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var usuario = _db.Login(correoLogin, Password);

            if (usuario == null)
            {
                stopwatch.Stop();
                usuarioLog = ResolveLogUserName(correoLogin, usuarioPc, correoPrincipalDispositivo);
                LogService.WarningForUser("Login", "Credenciales invalidas", usuarioLog, BuildLoginDetail(usuarioLog, correoLogin));
                LogService.LatencyForUser("Login", "Login rechazado por credenciales invalidas", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, correoLogin));
                MessageBox.Show("La contrasena ingresada es incorrecta. Verifique el dato e intente nuevamente.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Evita mostrar nombre duplicado en cabecera cuando el registro antiguo
            // se guardo como "Nombres = Apellidos = usuarioPC".
            if (!string.IsNullOrWhiteSpace(usuario.Nombres) &&
                string.Equals(usuario.Nombres, usuario.Apellidos, StringComparison.OrdinalIgnoreCase))
            {
                usuario.Apellidos = string.Empty;
            }

            if (Recordarme)
                GuardarCredenciales();
            else
                LimpiarCredencialesGuardadas();

            stopwatch.Stop();
            usuarioLog = ResolveLogUserName(usuario.Correo, usuarioPc, correoPrincipalDispositivo);
            LogService.InfoForUser("Login", "Login exitoso", usuarioLog, BuildLoginDetail(usuarioLog, usuario.Correo));
            LogService.LatencyForUser("Login", "Login exitoso", usuarioLog, stopwatch.ElapsedMilliseconds, BuildLoginDetail(usuarioLog, usuario.Correo));
            AbrirSesionUsuario(usuario);
        }

        private void AbrirSesionUsuario(Models.Usuario usuario)
        {
            SesionUsuario.UsuarioActual = usuario;
            SesionUsuario.ModoAdministrador = false;

            new MainWindow().Show();
            Application.Current.Windows[0]?.Close();
        }

        private void CargarCredencialesGuardadas()
        {
            try
            {
                if (!File.Exists(_credencialesPath))
                    return;

                var lineas = File.ReadAllLines(_credencialesPath);
                if (lineas.Length < 2)
                    return;

                Usuario = lineas[0] ?? string.Empty;
                Password = Descifrar(lineas[1]);
                Recordarme = true;
            }
            catch
            {
                Usuario = string.Empty;
                Password = string.Empty;
                Recordarme = false;
            }
        }

        private void GuardarCredenciales()
        {
            try
            {
                var carpeta = Path.GetDirectoryName(_credencialesPath);
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var passwordCifrada = Cifrar(Password ?? string.Empty);
                File.WriteAllLines(_credencialesPath, new[] { Usuario ?? string.Empty, passwordCifrada });
            }
            catch
            {
                // Si falla el guardado, no detenemos el login.
            }
        }

        private void LimpiarCredencialesGuardadas()
        {
            try
            {
                if (File.Exists(_credencialesPath))
                    File.Delete(_credencialesPath);
            }
            catch
            {
                // Si falla el borrado, no detenemos el login.
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
