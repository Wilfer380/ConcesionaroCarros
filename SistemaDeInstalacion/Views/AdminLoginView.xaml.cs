using ConcesionaroCarros.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace ConcesionaroCarros.Views
{
    public partial class AdminLoginView : Window
    {
        private bool _syncPasswordAdmin;

        public AdminLoginView(string usuarioPrefill = null, string passwordAdminPrefill = null)
        {
            InitializeComponent();
            var vm = new AdminLoginViewModel(usuarioPrefill, passwordAdminPrefill);
            DataContext = vm;
            txtPasswordAdmin.Password = vm.PasswordAdmin ?? string.Empty;
            txtPasswordAdminVisible.Text = vm.PasswordAdmin ?? string.Empty;
            txtUsuarioAdmin.Text = vm.Usuario ?? string.Empty;
            ActualizarPlaceholders();
        }

        private void PasswordAdminChanged(object sender, RoutedEventArgs e)
        {
            if (_syncPasswordAdmin)
                return;

            var valor = txtPasswordAdmin.Password ?? string.Empty;
            ((AdminLoginViewModel)DataContext).PasswordAdmin = valor;

            if (!string.Equals(txtPasswordAdminVisible.Text, valor))
            {
                _syncPasswordAdmin = true;
                txtPasswordAdminVisible.Text = valor;
                _syncPasswordAdmin = false;
            }

            ActualizarPasswordPlaceholder();
        }

        private void PasswordAdminVisibleChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncPasswordAdmin)
                return;

            var valor = txtPasswordAdminVisible.Text ?? string.Empty;
            ((AdminLoginViewModel)DataContext).PasswordAdmin = valor;

            if (!string.Equals(txtPasswordAdmin.Password, valor))
            {
                _syncPasswordAdmin = true;
                txtPasswordAdmin.Password = valor;
                _syncPasswordAdmin = false;
            }

            ActualizarPasswordPlaceholder();
        }

        private void ShowPasswordAdmin_Checked(object sender, RoutedEventArgs e)
        {
            _syncPasswordAdmin = true;
            txtPasswordAdminVisible.Text = txtPasswordAdmin.Password;
            _syncPasswordAdmin = false;

            txtPasswordAdmin.Visibility = Visibility.Collapsed;
            txtPasswordAdminVisible.Visibility = Visibility.Visible;
            ActualizarPasswordPlaceholder();
        }

        private void ShowPasswordAdmin_Unchecked(object sender, RoutedEventArgs e)
        {
            _syncPasswordAdmin = true;
            txtPasswordAdmin.Password = txtPasswordAdminVisible.Text ?? string.Empty;
            _syncPasswordAdmin = false;

            txtPasswordAdminVisible.Visibility = Visibility.Collapsed;
            txtPasswordAdmin.Visibility = Visibility.Visible;
            ActualizarPasswordPlaceholder();
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

        private void UsuarioAdminTextChanged(object sender, TextChangedEventArgs e)
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
            txtUsuarioAdminPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(txtUsuarioAdmin.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void ActualizarPasswordPlaceholder()
        {
            var valor = txtPasswordAdmin.Visibility == Visibility.Visible
                ? txtPasswordAdmin.Password
                : txtPasswordAdminVisible.Text;

            txtPasswordAdminPlaceholder.Visibility =
                string.IsNullOrEmpty(valor)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}
