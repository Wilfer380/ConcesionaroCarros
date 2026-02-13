using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

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

        public ICommand EliminarUsuarioCommand { get; }
        public ICommand AgregarUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }

        public GestionUsuarioViewModel()
        {
            _usuariosDb = new UsuariosDbService();
            _clientesDb = new ClientesDbService();
            _empleadosDb = new EmpleadosDbService();

   

            AgregarUsuarioCommand = new RelayCommand(_ =>
            {
                new FormularioUsuarioView().ShowDialog();
                CargarDatos();
            });

            EditarUsuarioCommand = new RelayCommand(usuario =>
            {
                if (usuario is Usuario u)
                {
                    string rolAnterior = u.Rol;

                    new FormularioUsuarioView(u).ShowDialog();

                    var usuarioActualizado =
                        _usuariosDb.ObtenerTodos().FirstOrDefault(x => x.Id == u.Id);

                    if (usuarioActualizado == null)
                        return;

                    string rolNuevo = usuarioActualizado.Rol;


                    if (rolNuevo == "CLIENTE")
                    {
                        var cliente = Clientes.FirstOrDefault(c => c.Correo == usuarioActualizado.Correo);

                        if (cliente != null)
                        {
                            cliente.Nombres = usuarioActualizado.Nombres;
                            cliente.Apellidos = usuarioActualizado.Apellidos;
                            cliente.Correo = usuarioActualizado.Correo;
                            cliente.Telefono = usuarioActualizado.Telefono;

                            _clientesDb.Actualizar(cliente);
                        }
                    }

                    if (rolNuevo == "EMPLEADO")
                    {
                        var empleado = Empleados.FirstOrDefault(e => e.Correo == usuarioActualizado.Correo);

                        if (empleado != null)
                        {
                            empleado.Nombres = usuarioActualizado.Nombres;
                            empleado.Apellidos = usuarioActualizado.Apellidos;
                            empleado.Correo = usuarioActualizado.Correo;
                            empleado.Telefono = usuarioActualizado.Telefono;

                            _empleadosDb.Actualizar(empleado);
                        }
                    }


                    if (rolAnterior != rolNuevo)
                    {
                        if (rolNuevo == "CLIENTE")
                        {
                            _empleadosDb.EliminarPorCorreo(usuarioActualizado.Correo);
                            _clientesDb.InsertarDesdeUsuario(usuarioActualizado);
                        }

                        if (rolNuevo == "EMPLEADO")
                        {
                            _clientesDb.EliminarPorCorreo(usuarioActualizado.Correo);
                            _empleadosDb.InsertarDesdeUsuario(usuarioActualizado);
                        }
                    }

                    CargarDatos();
                }
            });
            EliminarUsuarioCommand = new RelayCommand(usuario =>
            {
                if (usuario is Usuario u)
                {
                    if (u.Rol == "CLIENTE")
                    {
                        _clientesDb.EliminarPorCorreo(u.Correo);
                        var cliente = Clientes.FirstOrDefault(x => x.Correo == u.Correo);
                        if (cliente != null)
                            Clientes.Remove(cliente);
                    }

                    if (u.Rol == "EMPLEADO")
                    {
                        _empleadosDb.EliminarPorCorreo(u.Correo);
                        var empleado = Empleados.FirstOrDefault(x => x.Correo == u.Correo);
                        if (empleado != null)
                            Empleados.Remove(empleado);
                    }

                    _usuariosDb.Eliminar(u.Id);
                    Usuarios.Remove(u);
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
