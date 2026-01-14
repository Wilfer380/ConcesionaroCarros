using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EmpleadosViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        public ObservableCollection<Empleado> Empleados { get; set; }

        public ICommand SeleccionarEmpleadoCommand { get; }

        public EmpleadosViewModel(MainViewModel main)
        {
            _main = main;

            Empleados = new ObservableCollection<Empleado>
            {
                new Empleado{ Nombre="Juan Pérez", Cargo="Asesor Comercial", VentasMes=12 },
                new Empleado{ Nombre="María Rodríguez", Cargo="Asesor Senior", VentasMes=18 }
            };

            SeleccionarEmpleadoCommand = new RelayCommand(e =>
            {
                _main.EmpleadoActual = (Empleado)e;
                _main.ShowVentaCommand.Execute(null);
            });
        }
    }
}
