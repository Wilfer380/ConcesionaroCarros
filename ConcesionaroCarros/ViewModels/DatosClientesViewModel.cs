using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Enums;
using ConcesionaroCarros.Views;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models; 
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class DatosClientesViewModel : BaseViewModel
    {
        private readonly PuntoVentaViewModel _puntoVenta;

        // ===== CAMPOS MOSTRADOS EN LA VISTA =====

        public string NombreCompleto { get; set; }
        public string Cedula { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Cargo { get; set; }
        public string CiudadDepartamento { get; set; }
        public string Direccion { get; set; }
        public string FechaNacimiento { get; set; }
        public string CodigoPostal { get; set; }



        public ObservableCollection<Empleado> Empleados { get; set; }

        private Empleado _empleadoSeleccionado;
        public Empleado EmpleadoSeleccionado
        {
            get => _empleadoSeleccionado;
            set
            {
                _empleadoSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public ICommand VolverAtrasCommand { get; }
        public ICommand ConfirmarDatosCommand { get; }

        public DatosClientesViewModel(PuntoVentaViewModel puntoVenta)
        {
            _puntoVenta = puntoVenta;

            CargarDatosUsuario();
            CargarEmpleados();

            VolverAtrasCommand = new RelayCommand(_ =>
            {
                _puntoVenta.PasoActual = PasoVenta.DetalleOperacion;
                _puntoVenta.ContenidoCentral = _puntoVenta;
            });

            ConfirmarDatosCommand = new RelayCommand(_ =>
            {
                _puntoVenta.IrAConfirmarVenta();
            });
        }

        // ===================================================
        // CARGA AUTOMÁTICA CLIENTE / EMPLEADO SEGÚN LOGIN
        // ===================================================

        private void CargarDatosUsuario()
        {
            var usuario = SesionUsuario.UsuarioActual;
            if (usuario == null) return;

            // ================= CLIENTE =================
            if (usuario.Rol == "CLIENTE")
            {
                var db = new ClientesDbService();
                var c = db.ObtenerPorCorreo(usuario.Correo);

                if (c == null) return;

                NombreCompleto = $"{c.Nombres} {c.Apellidos}";
                Cedula = c.Cedula;
                Telefono = c.Telefono;
                Correo = c.Correo;
                Cargo = c.CargoActual;
                CiudadDepartamento = c.CiudadDepartamento;
                Direccion = c.Direccion;
                CodigoPostal = c.CodigoPostal;

                FechaNacimiento = c.FechaNacimiento.HasValue
                    ? c.FechaNacimiento.Value.ToString("dd/MM/yyyy")
                    : "";
            }

            // ================= EMPLEADO / ADMIN =================
            else
            {
                var db = new EmpleadosDbService();
                var e = db.ObtenerPorCorreo(usuario.Correo);

                if (e == null) return;

                NombreCompleto = $"{e.Nombres} {e.Apellidos}";
                Cedula = e.Cedula;
                Telefono = e.Telefono;
                Correo = e.Correo;
                Cargo = e.Cargo;
                CiudadDepartamento = $"{e.Ciudad}, {e.Departamento}";
                Direccion = "";
            }

            OnPropertyChanged(nameof(NombreCompleto));
            OnPropertyChanged(nameof(Cedula));
            OnPropertyChanged(nameof(Telefono));
            OnPropertyChanged(nameof(Correo));
            OnPropertyChanged(nameof(Cargo));
            OnPropertyChanged(nameof(CiudadDepartamento));
            OnPropertyChanged(nameof(Direccion));
            OnPropertyChanged(nameof(FechaNacimiento));
            OnPropertyChanged(nameof(CodigoPostal));

        }

        private void CargarEmpleados()
        {
            var db = new EmpleadosDbService();

            var lista = db.ObtenerTodos();

            Empleados = new ObservableCollection<Empleado>(lista);

            OnPropertyChanged(nameof(Empleados));
        }
    }
}
    
