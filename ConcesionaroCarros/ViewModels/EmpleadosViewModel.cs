using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EmpleadosViewModel : BaseViewModel
    {
        private readonly EmpleadosDbService _db;

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
            _db = new EmpleadosDbService();

            Empleados = new ObservableCollection<Empleado>();

            CargarEmpleados();

            NuevoEmpleadoCommand = new RelayCommand(_ =>
            {
                ModalView = new EditarEmpleadoView
                {
                    DataContext = new EditarEmpleadoViewModel(
                        this,
                        new Empleado
                        {
                            Nombres = "",
                            Apellidos = "",
                            Correo = "",
                            Telefono = "",
                            Cargo = "Asesor Junior",
                            Activo = true,
                            MetaVentas = 0
                        })
                };
            });

            EditarEmpleadoCommand = new RelayCommand(e =>
            {
                var empleado = e as Empleado;
                if (empleado == null)
                    return;

                ModalView = new EditarEmpleadoView
                {
                    DataContext = new EditarEmpleadoViewModel(this, empleado)
                };
            });
        }

        private void CargarEmpleados()
        {
            Empleados.Clear();

            foreach (var e in _db.ObtenerTodos())
                Empleados.Add(e);
        }

        public void GuardarEmpleado(Empleado empleado)
        {
            if (empleado.Id == 0)
                _db.Insertar(empleado);
            else
                _db.Actualizar(empleado);

            CargarEmpleados();
            ModalView = null;
        }

        public void CerrarModal()
        {
            ModalView = null;
        }
    }
}
