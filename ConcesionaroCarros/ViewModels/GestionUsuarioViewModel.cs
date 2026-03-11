using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcesionaroCarros.ViewModels
{
    public class GestionUsuarioViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _usuariosDb;
        private readonly ClientesDbService _clientesDb;
        private readonly EmpleadosDbService _empleadosDb;
        private readonly AdministradoresDbService _administradoresDb;
        private readonly InstaladorDbService _instaladorDb;

        public ObservableCollection<Usuario> Usuarios { get; set; }
        public ObservableCollection<Cliente> Clientes { get; set; }
        public ObservableCollection<Empleado> Empleados { get; set; }

        private Usuario _selectedUsuario;
        private bool _suprimirAperturaPanelPorAccion;
        public Usuario SelectedUsuario
        {
            get => _selectedUsuario;
            set
            {
                _selectedUsuario = value;
                OnPropertyChanged();

                if (_suprimirAperturaPanelPorAccion)
                {
                    _suprimirAperturaPanelPorAccion = false;
                    LimpiarPanelAsignacion();
                    return;
                }

                PrepararPanelAsignacion();
            }
        }

        private bool _isPanelAsignacionVisible;
        public bool IsPanelAsignacionVisible
        {
            get => _isPanelAsignacionVisible;
            set
            {
                _isPanelAsignacionVisible = value;
                OnPropertyChanged();
            }
        }

        private string _nombreUsuarioAsignacion;
        public string NombreUsuarioAsignacion
        {
            get => _nombreUsuarioAsignacion;
            set
            {
                _nombreUsuarioAsignacion = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RolAsignacionItem> RolesAsignables { get; } =
            new ObservableCollection<RolAsignacionItem>();

        public ObservableCollection<AplicativoAsignacionItem> AplicativosAsignables { get; } =
            new ObservableCollection<AplicativoAsignacionItem>();

        public ICommand EliminarUsuarioCommand { get; }
        public ICommand AgregarUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }
        public ICommand SeleccionarRolCommand { get; }
        public ICommand GuardarAsignacionCommand { get; }
        public ICommand OcultarPanelAsignacionCommand { get; }

        public GestionUsuarioViewModel()
        {
            _usuariosDb = new UsuariosDbService();
            _clientesDb = new ClientesDbService();
            _empleadosDb = new EmpleadosDbService();
            _administradoresDb = new AdministradoresDbService();
            _instaladorDb = new InstaladorDbService();

            AgregarUsuarioCommand = new RelayCommand(_ =>
            {
                MostrarFormularioUsuario();
                CargarDatos();
            });

            EditarUsuarioCommand = new RelayCommand(usuario =>
            {
                if (!(usuario is Usuario u))
                    return;

                try
                {
                    var rolAnterior = u.Rol;
                    var correoAnterior = u.Correo;

                    MostrarFormularioUsuario(u);

                    var usuarioActualizado = _usuariosDb
                        .ObtenerTodos()
                        .FirstOrDefault(x => x.Id == u.Id);

                    if (usuarioActualizado == null)
                        return;

                    var rolNuevo = usuarioActualizado.Rol;

                    SincronizarEntidadesSegunRol(usuarioActualizado, rolAnterior, rolNuevo);
                    SincronizarAdministrador(usuarioActualizado, correoAnterior, rolNuevo);

                    CargarDatos();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
                {
                    MessageBox.Show(
                        "La base de datos esta ocupada. Intente nuevamente.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Ocurrio un error al editar el usuario.\n" + ex.Message,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });

            GuardarAsignacionCommand = new RelayCommand(_ => GuardarAsignacion());
            SeleccionarRolCommand = new RelayCommand(rol => SeleccionarRol(rol as RolAsignacionItem));
            OcultarPanelAsignacionCommand = new RelayCommand(_ => OcultarPanelAsignacion());

            EliminarUsuarioCommand = new RelayCommand(usuario =>
            {
                if (usuario is Usuario u)
                {
                    _clientesDb.EliminarPorCorreo(u.Correo);
                    _empleadosDb.EliminarPorCorreo(u.Correo);
                    _administradoresDb.EliminarPorCorreo(u.Correo);

                    var cliente = Clientes.FirstOrDefault(x => x.Correo == u.Correo);
                    if (cliente != null)
                        Clientes.Remove(cliente);

                    var empleado = Empleados.FirstOrDefault(x => x.Correo == u.Correo);
                    if (empleado != null)
                        Empleados.Remove(empleado);

                    _usuariosDb.EliminarConDependencias(u.Id, u.Correo);
                    Usuarios.Remove(u);

                    if (SelectedUsuario != null && SelectedUsuario.Id == u.Id)
                        LimpiarPanelAsignacion();
                }
            });

            CargarDatos();
            LimpiarPanelAsignacion();
        }

        private void MostrarFormularioUsuario(Usuario usuario = null)
        {
            var owner = Application.Current?.MainWindow;
            var formulario = new FormularioUsuarioView(usuario);

            using (var overlay = new ModalOverlayScope(owner))
            {
                if (overlay.OverlayWindow != null)
                {
                    formulario.Owner = overlay.OverlayWindow;
                    formulario.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else if (owner != null)
                {
                    formulario.Owner = owner;
                    formulario.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                formulario.ShowDialog();
            }
        }

        private void PrepararPanelAsignacion()
        {
            RolesAsignables.Clear();
            AplicativosAsignables.Clear();

            if (SelectedUsuario == null)
            {
                IsPanelAsignacionVisible = false;
                NombreUsuarioAsignacion = "";
                return;
            }

            IsPanelAsignacionVisible = true;
            NombreUsuarioAsignacion = $"{SelectedUsuario.Nombres} {SelectedUsuario.Apellidos}";

            CargarRoles(SelectedUsuario.Rol);
            CargarAplicativos(SelectedUsuario.ObtenerAplicativosAsignados());
        }

        private void CargarRoles(string rolActual)
        {
            var rolSeguro = string.IsNullOrWhiteSpace(rolActual)
                ? RolesSistema.Ventas
                : rolActual;

            foreach (var rol in RolesSistema.AsignablesSinAdmin)
            {
                RolesAsignables.Add(new RolAsignacionItem
                {
                    CodigoRol = rol,
                    Nombre = rol,
                    IsChecked = string.Equals(rolSeguro, rol, StringComparison.OrdinalIgnoreCase),
                    EsEditable = false
                });
            }

            // Respaldo para datos historicos con roles fuera del catalogo actual.
            if (!RolesAsignables.Any(r => r.IsChecked))
            {
                RolesAsignables.Add(new RolAsignacionItem
                {
                    CodigoRol = rolSeguro,
                    Nombre = rolSeguro,
                    IsChecked = true,
                    EsEditable = false
                });
            }
        }

        private void CargarAplicativos(IEnumerable<string> rutasAsignadas)
        {
            var set = new HashSet<string>(
                rutasAsignadas ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var app in _instaladorDb.ObtenerTodos())
            {
                var nombre = string.IsNullOrWhiteSpace(app.Nombre)
                    ? Path.GetFileNameWithoutExtension(app.Ruta)
                    : app.Nombre;

                AplicativosAsignables.Add(new AplicativoAsignacionItem
                {
                    Nombre = nombre,
                    Ruta = app.Ruta,
                    IsChecked = set.Contains(app.Ruta)
                });
            }
        }

        private void GuardarAsignacion()
        {
            try
            {
                if (SelectedUsuario == null)
                    return;

                var rutasSeleccionadas = AplicativosAsignables
                    .Where(a => a.IsChecked)
                    .Select(a => a.Ruta)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct()
                    .ToList();

                SelectedUsuario.EstablecerAplicativosAsignados(rutasSeleccionadas);

                _usuariosDb.ActualizarAplicativosJson(SelectedUsuario.Id, SelectedUsuario.AplicativosJson);

                var idUsuario = SelectedUsuario.Id;
                CargarDatos();
                SelectedUsuario = Usuarios.FirstOrDefault(x => x.Id == idUsuario);

                MessageBox.Show(
                    "Asignacion guardada correctamente.",
                    "Informacion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
            {
                MessageBox.Show(
                    "La base de datos esta ocupada. Intente guardar nuevamente.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ocurrio un error al guardar la asignacion.\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SeleccionarRol(RolAsignacionItem rolSeleccionado)
        {
            // Los roles en este panel son de solo lectura.
        }

        private void SincronizarEntidadesSegunRol(Usuario usuario, string rolAnterior, string rolNuevo)
        {
            _clientesDb.EliminarPorCorreo(usuario.Correo);

            var empleado = _empleadosDb.ObtenerPorCorreo(usuario.Correo);
            if (empleado != null)
            {
                empleado.Nombres = usuario.Nombres;
                empleado.Apellidos = usuario.Apellidos;
                empleado.Correo = usuario.Correo;
                empleado.Telefono = usuario.Telefono;
                empleado.Cargo = rolNuevo;
                _empleadosDb.Actualizar(empleado);
                return;
            }

            try
            {
                _empleadosDb.Insertar(new Empleado
                {
                    Nombres = usuario.Nombres,
                    Apellidos = usuario.Apellidos,
                    Correo = usuario.Correo,
                    Telefono = usuario.Telefono,
                    Cargo = rolNuevo,
                    Activo = true
                });
            }
            catch
            {
                // Si ya existe o hay datos historicos inconsistentes, no bloqueamos flujo.
            }
        }

        private void SincronizarAdministrador(Usuario usuario, string correoAnterior, string rolNuevo)
        {
            if (usuario == null)
                return;

            if (RolesSistema.EsAdministrador(rolNuevo))
            {
                _administradoresDb.SincronizarDesdeUsuario(correoAnterior, usuario);
                return;
            }

            _administradoresDb.EliminarPorCorreo(correoAnterior);
            if (!string.Equals(correoAnterior, usuario.Correo, StringComparison.OrdinalIgnoreCase))
                _administradoresDb.EliminarPorCorreo(usuario.Correo);
        }

        private void LimpiarPanelAsignacion()
        {
            IsPanelAsignacionVisible = false;
            NombreUsuarioAsignacion = "";
            RolesAsignables.Clear();
            AplicativosAsignables.Clear();
        }

        public void OcultarPanelAsignacion()
        {
            SelectedUsuario = null;
        }

        public void MarcarSiguienteSeleccionComoAccion()
        {
            _suprimirAperturaPanelPorAccion = true;
            LimpiarPanelAsignacion();

            Application.Current?.Dispatcher.BeginInvoke(
                new Action(() => _suprimirAperturaPanelPorAccion = false),
                DispatcherPriority.Background);
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

    public class RolAsignacionItem : BaseViewModel
    {
        private bool _isChecked;
        private bool _esEditable = true;

        public string CodigoRol { get; set; }
        public string Nombre { get; set; }
        public bool EsEditable
        {
            get => _esEditable;
            set
            {
                _esEditable = value;
                OnPropertyChanged();
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }

    public class AplicativoAsignacionItem : BaseViewModel
    {
        private bool _isChecked;

        public string Nombre { get; set; }
        public string Ruta { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }
}
