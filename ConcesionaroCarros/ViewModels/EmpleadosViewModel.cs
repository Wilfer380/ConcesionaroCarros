using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EmpleadosViewModel : BaseViewModel
    {
        public ObservableCollection<Empleado> Empleados { get; }

        private object _modalView;
        public object ModalView
        {
            get => _modalView;
            set { _modalView = value; OnPropertyChanged(); }
        }

        public ICommand NuevoEmpleadoCommand { get; }
        public ICommand EditarEmpleadoCommand { get; }

        public EmpleadosViewModel()
        {
            Empleados = new ObservableCollection<Empleado>
            {
                new Empleado
                {
                    Id = 1,
                    Nombres = "Juan",
                    Apellidos = "Pérez",
                    Cargo = "Asesor Senior",
                    Correo = "juan.p@concesionario.com",
                    Telefono = "+57 300 000 0000",
                    Activo = true,
                    MetaVentas = 85
                }
            };

            NuevoEmpleadoCommand = new RelayCommand(_ =>
            {
                ModalView = new EditarEmpleadoView
                {
                    DataContext = new EditarEmpleadoViewModel(this, new Empleado())
                };
            });

            EditarEmpleadoCommand = new RelayCommand(e =>
            {
                ModalView = new EditarEmpleadoView
                {
                    DataContext = new EditarEmpleadoViewModel(this, (Empleado)e)
                };
            });
        }

        public void GuardarEmpleado(Empleado empleado)
        {
            if (!Empleados.Contains(empleado))
                Empleados.Add(empleado);

            ModalView = null;
        }

        public void CerrarModal()
        {
            ModalView = null;
        }
    }
}
