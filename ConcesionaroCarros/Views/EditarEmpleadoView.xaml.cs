using System.Windows;
using System.Windows.Controls;
using ConcesionaroCarros.ViewModels;

namespace ConcesionaroCarros.Views
{
    public partial class EditarEmpleadoView : UserControl
    {
        public EditarEmpleadoView()
        {
            InitializeComponent();
        }

        // 🔥 CLAVE ABSOLUTA
        private void RadioButton_Inactivo_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditarEmpleadoViewModel vm)
            {
                vm.Empleado.Activo = false;
            }
        }
    }
}
