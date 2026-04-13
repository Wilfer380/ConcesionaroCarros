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
    public class GestionUsuarioViewModel : BaseViewModel, ILocalizableViewModel
    {
        private readonly UsuariosDbService _usuariosDb;
        private readonly AdministradoresDbService _administradoresDb;
        private readonly InstaladorDbService _instaladorDb;

        public ObservableCollection<Usuario> Usuarios { get; set; }

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

        public string UsersTabHeader => LocalizedText.Get("UserManagement_UsersTabHeader", "Users");
        public string AddUserLabel => LocalizedText.Get("UserManagement_AddUserButton", "Add user");
        public string FirstNameColumnHeader => LocalizedText.Get("UserManagement_FirstNameColumn", "First names");
        public string LastNameColumnHeader => LocalizedText.Get("UserManagement_LastNameColumn", "Last names");
        public string EmailColumnHeader => LocalizedText.Get("UserManagement_EmailColumn", "Email");
        public string PhoneColumnHeader => LocalizedText.Get("UserManagement_PhoneColumn", "Phone");
        public string RoleColumnHeader => LocalizedText.Get("UserManagement_RoleColumn", "Role");
        public string ApplicationsColumnHeader => LocalizedText.Get("UserManagement_ApplicationsColumn", "Applications");
        public string RegistrationDateColumnHeader => LocalizedText.Get("UserManagement_RegistrationDateColumn", "Registration date");
        public string ActionsColumnHeader => LocalizedText.Get("UserManagement_ActionsColumn", "Actions");
        public string EditLabel => LocalizedText.Get("Common_Edit", "Edit");
        public string DeleteLabel => LocalizedText.Get("Common_Delete", "Delete");
        public string AssignmentPanelTitle => LocalizedText.Get("UserManagement_AssignmentPanelTitle", "Application assignment by section");
        public string SelectedUserLabel => LocalizedText.Get("UserManagement_SelectedUserLabel", "Selected user");
        public string CurrentRoleLabel => LocalizedText.Get("UserManagement_CurrentRoleLabel", "Current role");
        public string AvailableRolesGroupHeader => LocalizedText.Get("UserManagement_AvailableRolesHeader", "Available roles");
        public string InstallerApplicationsGroupHeader => LocalizedText.Get("UserManagement_InstallerApplicationsHeader", "Installer applications");
        public string CloseLabel => LocalizedText.Get("Common_Close", "Close");
        public string SaveAssignmentLabel => LocalizedText.Get("UserManagement_SaveAssignmentButton", "Save assignment");

        public GestionUsuarioViewModel()
        {
            _usuariosDb = new UsuariosDbService();
            _administradoresDb = new AdministradoresDbService();
            _instaladorDb = new InstaladorDbService();

            AgregarUsuarioCommand = new RelayCommand(_ =>
            {
                LogService.Info("GestionUsuarios", "Apertura de formulario de usuario", "Nuevo usuario");
                MostrarFormularioUsuario();
                CargarDatos();
            });

            EditarUsuarioCommand = new RelayCommand(usuario =>
            {
                if (!(usuario is Usuario u))
                    return;

                try
                {
                    var correoAnterior = u.Correo;
                    LogService.Info("GestionUsuarios", "Apertura de formulario de edicion de usuario", ConstruirDetalleUsuario(u));
                    MostrarFormularioUsuario(u);

                    var usuarioActualizado = _usuariosDb
                        .ObtenerTodos()
                        .FirstOrDefault(x => x.Id == u.Id);

                    if (usuarioActualizado == null)
                        return;

                    SincronizarAdministrador(usuarioActualizado, correoAnterior, usuarioActualizado.Rol);
                    CargarDatos();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
                {
                    LogService.Warning("GestionUsuarios", "Edicion de usuario bloqueada por base ocupada", ConstruirDetalleUsuario(u));
                    MessageBox.Show(
                        LocalizedText.Get("Common_DatabaseBusyRetryMessage", "The database is busy. Please try again."),
                        LocalizedText.Get("Common_NoticeTitle", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    LogService.Error("GestionUsuarios", "Error al editar usuario", ex, ConstruirDetalleUsuario(u));
                    MessageBox.Show(
                        LocalizedText.Get("UserManagement_EditErrorMessage", "An error occurred while editing the user.") + "\n" + ex.Message,
                        LocalizedText.Get("Common_ErrorTitle", "Error"),
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
                    _administradoresDb.EliminarPorCorreo(u.Correo);
                    _usuariosDb.EliminarConDependencias(u.Id, u.Correo);
                    Usuarios.Remove(u);
                    LogService.Info("GestionUsuarios", "Usuario eliminado", ConstruirDetalleUsuario(u));

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
            LogService.Info("GestionUsuarios", "Panel de asignacion abierto", ConstruirDetalleUsuario(SelectedUsuario));

            CargarRoles(SelectedUsuario.Rol);
            CargarAplicativos(SelectedUsuario.ObtenerAplicativosAsignados());
        }

        private void CargarRoles(string rolActual)
        {
            var rolSeguro = string.IsNullOrWhiteSpace(rolActual)
                ? RolesSistema.Ventas
                : rolActual;

            foreach (var rol in RolesSistema.Todos)
            {
                RolesAsignables.Add(new RolAsignacionItem
                {
                    CodigoRol = rol,
                    IsChecked = string.Equals(rolSeguro, rol, StringComparison.OrdinalIgnoreCase),
                    EsEditable = false
                });
            }

            if (!RolesAsignables.Any(r => r.IsChecked))
            {
                RolesAsignables.Add(new RolAsignacionItem
                {
                    CodigoRol = rolSeguro,
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
                LogService.Info(
                    "GestionUsuarios",
                    "Asignacion de aplicativos actualizada",
                    ConstruirDetalleAsignacion(SelectedUsuario, rutasSeleccionadas));

                var idUsuario = SelectedUsuario.Id;
                CargarDatos();
                SelectedUsuario = Usuarios.FirstOrDefault(x => x.Id == idUsuario);

                MessageBox.Show(
                    LocalizedText.Get("UserManagement_AssignmentSavedMessage", "Assignment saved successfully."),
                    LocalizedText.Get("Common_InformationTitle", "Information"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
            {
                LogService.Warning(
                    "GestionUsuarios",
                    "No se pudo guardar la asignacion por base ocupada",
                    ConstruirDetalleUsuario(SelectedUsuario));
                MessageBox.Show(
                    LocalizedText.Get("Common_DatabaseBusySaveRetryMessage", "The database is busy. Please try saving again."),
                    LocalizedText.Get("Common_NoticeTitle", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                LogService.Error(
                    "GestionUsuarios",
                    "Error al guardar asignacion de aplicativos",
                    ex,
                    ConstruirDetalleUsuario(SelectedUsuario));
                MessageBox.Show(
                    LocalizedText.Get("UserManagement_SaveAssignmentErrorMessage", "An error occurred while saving the assignment.") + "\n" + ex.Message,
                    LocalizedText.Get("Common_ErrorTitle", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SeleccionarRol(RolAsignacionItem rolSeleccionado)
        {
            // Los roles en este panel son de solo lectura.
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

        public override void RefreshLocalization()
        {
            RaisePropertyChanges(
                nameof(UsersTabHeader),
                nameof(AddUserLabel),
                nameof(FirstNameColumnHeader),
                nameof(LastNameColumnHeader),
                nameof(EmailColumnHeader),
                nameof(PhoneColumnHeader),
                nameof(RoleColumnHeader),
                nameof(ApplicationsColumnHeader),
                nameof(RegistrationDateColumnHeader),
                nameof(ActionsColumnHeader),
                nameof(EditLabel),
                nameof(DeleteLabel),
                nameof(AssignmentPanelTitle),
                nameof(SelectedUserLabel),
                nameof(CurrentRoleLabel),
                nameof(AvailableRolesGroupHeader),
                nameof(InstallerApplicationsGroupHeader),
                nameof(CloseLabel),
                nameof(SaveAssignmentLabel));

            foreach (var role in RolesAsignables)
                role.RefreshLocalization();
        }

        public void OcultarPanelAsignacion()
        {
            if (SelectedUsuario != null || IsPanelAsignacionVisible)
                LogService.Info("GestionUsuarios", "Panel de asignacion cerrado", ConstruirDetalleUsuario(SelectedUsuario));

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
            OnPropertyChanged(nameof(Usuarios));
        }

        private static string ConstruirDetalleUsuario(Usuario usuario)
        {
            if (usuario == null)
                return "Sin usuario seleccionado";

            return $"Id={usuario.Id}; Correo={usuario.Correo}; Rol={usuario.Rol}";
        }

        private static string ConstruirDetalleAsignacion(Usuario usuario, IEnumerable<string> rutasSeleccionadas)
        {
            var rutas = (rutasSeleccionadas ?? Enumerable.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var aplicaciones = rutas.Count == 0
                ? "Sin aplicativos asignados"
                : string.Join(" | ", rutas);

            return $"{ConstruirDetalleUsuario(usuario)}; Aplicativos={rutas.Count}; Rutas={aplicaciones}";
        }
    }

    public class RolAsignacionItem : BaseViewModel
    {
        private bool _isChecked;
        private bool _esEditable = true;

        public string CodigoRol { get; set; }
        public string Nombre => LocalizedText.GetRoleDisplay(CodigoRol);
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

        public override void RefreshLocalization()
        {
            OnPropertyChanged(nameof(Nombre));
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
