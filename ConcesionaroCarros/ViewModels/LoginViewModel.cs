using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.IO;
using System.Security.Cryptography;
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
            var usuarioIngreso = (Usuario ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(usuarioIngreso))
            {
                MessageBox.Show("Debe ingresar usuario.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                MessageBox.Show("No existe un registro para ese usuario. Registrese primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var usuario = _db.Login(correoLogin, Password);

            if (usuario == null)
            {
                MessageBox.Show("Usuario o contrasena incorrectos", "Error",
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

    }
}
