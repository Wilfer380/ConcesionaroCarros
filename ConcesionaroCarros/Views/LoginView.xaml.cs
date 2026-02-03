using ConcesionaroCarros.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ConcesionaroCarros.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((LoginViewModel)DataContext).Password = txtPassword.Password;
        }

        // 🔹 NUEVO: abrir ventana de registro
        private void OpenRegister(object sender, RoutedEventArgs e)
        {
            var register = new RegisterView();
            register.Show();
            this.Close();
        }
     }
}
