using ConcesionaroCarros.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace ConcesionaroCarros.Views
{
    public partial class LoginView : Window
    {
        private bool _syncPassword;

        public LoginView(string usuarioPrefill = null, string passwordPrefill = null)
        {
            InitializeComponent();
            var vm = new LoginViewModel(usuarioPrefill, passwordPrefill);
            DataContext = vm;
            txtPassword.Password = vm.Password ?? string.Empty;
            txtPasswordVisible.Text = vm.Password ?? string.Empty;
            txtUsuario.Text = vm.Usuario ?? string.Empty;
            ActualizarPlaceholders();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncPassword)
                return;

            var valor = txtPassword.Password ?? string.Empty;
            ((LoginViewModel)DataContext).Password = valor;

            if (!string.Equals(txtPasswordVisible.Text, valor))
            {
                _syncPassword = true;
                txtPasswordVisible.Text = valor;
                _syncPassword = false;
            }

            ActualizarPasswordPlaceholder();
        }

        private void PasswordVisibleChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncPassword)
                return;

            var valor = txtPasswordVisible.Text ?? string.Empty;
            ((LoginViewModel)DataContext).Password = valor;

            if (!string.Equals(txtPassword.Password, valor))
            {
                _syncPassword = true;
                txtPassword.Password = valor;
                _syncPassword = false;
            }

            ActualizarPasswordPlaceholder();
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            _syncPassword = true;
            txtPasswordVisible.Text = txtPassword.Password;
            _syncPassword = false;

            txtPassword.Visibility = Visibility.Collapsed;
            txtPasswordVisible.Visibility = Visibility.Visible;
            ActualizarPasswordPlaceholder();
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            _syncPassword = true;
            txtPassword.Password = txtPasswordVisible.Text ?? string.Empty;
            _syncPassword = false;

            txtPasswordVisible.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            ActualizarPasswordPlaceholder();
        }

        private void OpenRegister(object sender, RoutedEventArgs e)
        {
            var register = new RegisterView();
            register.Show();
            Close();
        }

        private void OpenMicrosoftRecovery(object sender, RoutedEventArgs e)
        {
            var recoveryWindow = new MicrosoftRecoveryView
            {
                Owner = this
            };

            recoveryWindow.RecoveryCompleted += OnRecoveryCompleted;
            recoveryWindow.ShowDialog();
            recoveryWindow.RecoveryCompleted -= OnRecoveryCompleted;
        }

        private void OnRecoveryCompleted(string usuario, string passwordTemporal)
        {
            var vm = DataContext as LoginViewModel;
            if (vm == null)
                return;

            var usuarioSeguro = usuario ?? string.Empty;
            var passwordSegura = passwordTemporal ?? string.Empty;

            txtUsuario.Text = usuarioSeguro;
            txtPassword.Password = passwordSegura;
            txtPasswordVisible.Text = passwordSegura;

            vm.Usuario = usuarioSeguro;
            vm.Password = passwordSegura;
            vm.Recordarme = false;
            ActualizarPlaceholders();

            MessageBox.Show(
                "La nueva contrasena ya fue cargada en este login.",
                "Recuperacion completada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                e.Key == Key.M)
            {
                var adminLogin = new AdminLoginView();
                adminLogin.Show();
                Close();
                e.Handled = true;
            }
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoginShell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        private void UsuarioTextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarUsuarioPlaceholder();
        }

        private void ActualizarPlaceholders()
        {
            ActualizarUsuarioPlaceholder();
            ActualizarPasswordPlaceholder();
        }

        private void ActualizarUsuarioPlaceholder()
        {
            txtUsuarioPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(txtUsuario.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void ActualizarPasswordPlaceholder()
        {
            var valor = txtPassword.Visibility == Visibility.Visible
                ? txtPassword.Password
                : txtPasswordVisible.Text;

            txtPasswordPlaceholder.Visibility =
                string.IsNullOrEmpty(valor)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}
