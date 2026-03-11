using ConcesionaroCarros.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConcesionaroCarros.Views
{
    public partial class AdminRegisterView : Window
    {
        private bool _syncPasswordNormal;
        private bool _syncPasswordAdmin;

        public AdminRegisterView()
        {
            InitializeComponent();
            DataContext = new AdminRegisterViewModel();
        }

        private void PasswordNormalChanged(object sender, RoutedEventArgs e)
        {
            if (_syncPasswordNormal)
                return;

            var valor = txtPasswordNormal.Password ?? string.Empty;
            txtPasswordNormal.Tag = valor;
            ((AdminRegisterViewModel)DataContext).PasswordNormal = valor;

            if (!string.Equals(txtPasswordNormalVisible.Text, valor))
            {
                _syncPasswordNormal = true;
                txtPasswordNormalVisible.Text = valor;
                _syncPasswordNormal = false;
            }
        }

        private void PasswordAdminChanged(object sender, RoutedEventArgs e)
        {
            if (_syncPasswordAdmin)
                return;

            var valor = txtPasswordAdmin.Password ?? string.Empty;
            txtPasswordAdmin.Tag = valor;
            ((AdminRegisterViewModel)DataContext).PasswordAdmin = valor;

            if (!string.Equals(txtPasswordAdminVisible.Text, valor))
            {
                _syncPasswordAdmin = true;
                txtPasswordAdminVisible.Text = valor;
                _syncPasswordAdmin = false;
            }
        }

        private void PasswordNormalVisibleChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncPasswordNormal)
                return;

            var valor = txtPasswordNormalVisible.Text ?? string.Empty;
            ((AdminRegisterViewModel)DataContext).PasswordNormal = valor;

            if (!string.Equals(txtPasswordNormal.Password, valor))
            {
                _syncPasswordNormal = true;
                txtPasswordNormal.Password = valor;
                txtPasswordNormal.Tag = valor;
                _syncPasswordNormal = false;
            }
        }

        private void PasswordAdminVisibleChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncPasswordAdmin)
                return;

            var valor = txtPasswordAdminVisible.Text ?? string.Empty;
            ((AdminRegisterViewModel)DataContext).PasswordAdmin = valor;

            if (!string.Equals(txtPasswordAdmin.Password, valor))
            {
                _syncPasswordAdmin = true;
                txtPasswordAdmin.Password = valor;
                txtPasswordAdmin.Tag = valor;
                _syncPasswordAdmin = false;
            }
        }

        private void ShowPasswordNormal_Checked(object sender, RoutedEventArgs e)
        {
            _syncPasswordNormal = true;
            txtPasswordNormalVisible.Text = txtPasswordNormal.Password;
            _syncPasswordNormal = false;

            txtPasswordNormal.Visibility = Visibility.Collapsed;
            txtPasswordNormalVisible.Visibility = Visibility.Visible;
        }

        private void ShowPasswordNormal_Unchecked(object sender, RoutedEventArgs e)
        {
            _syncPasswordNormal = true;
            txtPasswordNormal.Password = txtPasswordNormalVisible.Text ?? string.Empty;
            txtPasswordNormal.Tag = txtPasswordNormal.Password;
            _syncPasswordNormal = false;

            txtPasswordNormalVisible.Visibility = Visibility.Collapsed;
            txtPasswordNormal.Visibility = Visibility.Visible;
        }

        private void ShowPasswordAdmin_Checked(object sender, RoutedEventArgs e)
        {
            _syncPasswordAdmin = true;
            txtPasswordAdminVisible.Text = txtPasswordAdmin.Password;
            _syncPasswordAdmin = false;

            txtPasswordAdmin.Visibility = Visibility.Collapsed;
            txtPasswordAdminVisible.Visibility = Visibility.Visible;
        }

        private void ShowPasswordAdmin_Unchecked(object sender, RoutedEventArgs e)
        {
            _syncPasswordAdmin = true;
            txtPasswordAdmin.Password = txtPasswordAdminVisible.Text ?? string.Empty;
            txtPasswordAdmin.Tag = txtPasswordAdmin.Password;
            _syncPasswordAdmin = false;

            txtPasswordAdminVisible.Visibility = Visibility.Collapsed;
            txtPasswordAdmin.Visibility = Visibility.Visible;
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
    }
}
