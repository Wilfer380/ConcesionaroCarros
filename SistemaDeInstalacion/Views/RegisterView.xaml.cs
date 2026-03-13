using ConcesionaroCarros.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConcesionaroCarros.Views
{
    public partial class RegisterView : Window
    {
        private bool _syncPassword;

        public RegisterView()
        {
            InitializeComponent();
            DataContext = new RegisterViewModel();
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncPassword)
                return;

            var valor = txtPassword.Password ?? string.Empty;
            txtPassword.Tag = valor;
            ((RegisterViewModel)DataContext).Password = valor;

            if (!string.Equals(txtPasswordVisible.Text, valor))
            {
                _syncPassword = true;
                txtPasswordVisible.Text = valor;
                _syncPassword = false;
            }
        }

        private void PasswordVisibleChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncPassword)
                return;

            var valor = txtPasswordVisible.Text ?? string.Empty;
            ((RegisterViewModel)DataContext).Password = valor;

            if (!string.Equals(txtPassword.Password, valor))
            {
                _syncPassword = true;
                txtPassword.Password = valor;
                txtPassword.Tag = valor;
                _syncPassword = false;
            }
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            _syncPassword = true;
            txtPasswordVisible.Text = txtPassword.Password;
            _syncPassword = false;

            txtPassword.Visibility = Visibility.Collapsed;
            txtPasswordVisible.Visibility = Visibility.Visible;
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            _syncPassword = true;
            txtPassword.Password = txtPasswordVisible.Text ?? string.Empty;
            txtPassword.Tag = txtPassword.Password;
            _syncPassword = false;

            txtPasswordVisible.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RegisterShell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
    }
}
