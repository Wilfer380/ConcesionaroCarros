using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EditarEmpleadoViewModel : BaseViewModel
    {
        private readonly EmpleadosViewModel _parent;

        public Empleado Empleado { get; }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public EditarEmpleadoViewModel(EmpleadosViewModel parent, Empleado empleado)
        {
            _parent = parent;
            Empleado = empleado;

            GuardarCommand = new RelayCommand(_ =>
            {
                _parent.GuardarEmpleado(Empleado);
            });

            CancelarCommand = new RelayCommand(_ =>
            {
                _parent.CerrarModal();
            });
        }
    }
}
