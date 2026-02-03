using ConcesionaroCarros.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ConcesionaroCarros.Views
{
    /// <summary>
    /// Lógica de interacción para RegisterView.xaml
    /// </summary>
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
            DataContext = new RegisterViewModel();
        }

        // 🔹 NUEVO: Captura contraseña
        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((RegisterViewModel)DataContext).Password =
                ((PasswordBox)sender).Password;
        }

        // 🔹 NUEVO: Captura confirmar contraseña
        private void ConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((RegisterViewModel)DataContext).ConfirmPassword =
                ((PasswordBox)sender).Password;
        }
    }
}
