using ConcesionaroCarros.Models;
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
    /// Lógica de interacción para FormularioUsuarioView.xaml
    /// </summary>
    public partial class FormularioUsuarioView : Window
    {
        public FormularioUsuarioView(Models.Usuario usuario = null)
        {
            InitializeComponent();
            var vm = new FormularioUsuarioViewModel(this, usuario);
            DataContext = vm;

            Guardar.Click += (_, __) =>
            {
                vm.Guardar(pwd.Password);
            };
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
