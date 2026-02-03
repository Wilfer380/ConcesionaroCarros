using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Commands;          // 🔹 AGREGADO
using System.Collections.ObjectModel;
using System.Windows.Input;                 // 🔹 AGREGADO

namespace ConcesionaroCarros.ViewModels
{
    public class GestionUsuarioViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _usuariosDb;
        private readonly ClientesDbService _clientesDb;
        private readonly EmpleadosDbService _empleadosDb;

        public ObservableCollection<Usuario> Usuarios { get; set; }
        public ObservableCollection<Cliente> Clientes { get; set; }
        public ObservableCollection<Empleado> Empleados { get; set; }

        // 🔹 AGREGADO — COMANDOS
        public ICommand EliminarUsuarioCommand { get; }
        public ICommand EliminarClienteCommand { get; }
        public ICommand EliminarEmpleadoCommand { get; }

        public GestionUsuarioViewModel()
        {
            _usuariosDb = new UsuariosDbService();
            _clientesDb = new ClientesDbService();
            _empleadosDb = new EmpleadosDbService();

            // 🔹 AGREGADO — INICIALIZACIÓN DE COMANDOS
            EliminarUsuarioCommand = new RelayCommand(usuario =>
            {
                if (usuario is Usuario u)
                {
                    _usuariosDb.Eliminar(u.Id);
                    Usuarios.Remove(u);
                }
            });

            EliminarClienteCommand = new RelayCommand(cliente =>
            {
                if (cliente is Cliente c)
                {
                    _clientesDb.Eliminar(c.Id);
                    Clientes.Remove(c);
                }
            });

            EliminarEmpleadoCommand = new RelayCommand(empleado =>
            {
                if (empleado is Empleado e)
                {
                    _empleadosDb.Eliminar(e.Id);
                    Empleados.Remove(e);
                }
            });

            CargarDatos();
        }

        private void CargarDatos()
        {
            Usuarios = new ObservableCollection<Usuario>(_usuariosDb.ObtenerTodos());
            Clientes = new ObservableCollection<Cliente>(_clientesDb.ObtenerTodos());
            Empleados = new ObservableCollection<Empleado>(_empleadosDb.ObtenerTodos());

            OnPropertyChanged(nameof(Usuarios));
            OnPropertyChanged(nameof(Clientes));
            OnPropertyChanged(nameof(Empleados));
        }
    }
}
