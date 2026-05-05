using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public sealed class DeveloperAccountsViewModel : BaseViewModel, ILocalizableViewModel
    {
        private readonly DeveloperAccountsDbService _developerDb = new DeveloperAccountsDbService();
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private string _email;
        private string _notes;

        public ObservableCollection<DeveloperAccountEntry> Developers { get; } = new ObservableCollection<DeveloperAccountEntry>();

        public string NewDeveloperEmail
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string NewDeveloperNotes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public string Title => LocalizedText.Get("DeveloperAccounts_Title", "Gestión de Developers");
        public string Subtitle => LocalizedText.Get("DeveloperAccounts_Subtitle", "Habilitá o deshabilitá accesos Developer. La contraseña se define creando primero el usuario base.");
        public string EmailLabel => LocalizedText.Get("DeveloperAccounts_EmailLabel", "Correo developer");
        public string NotesLabel => LocalizedText.Get("DeveloperAccounts_NotesLabel", "Notas");
        public string AddLabel => LocalizedText.Get("DeveloperAccounts_AddLabel", "Agregar");

        public ICommand AddDeveloperCommand { get; }

        public DeveloperAccountsViewModel()
        {
            AddDeveloperCommand = new RelayCommand(_ => AddDeveloper());
            Refresh();
        }

        private void Refresh()
        {
            Developers.Clear();
            foreach (var row in _developerDb.ListAll() ?? Array.Empty<DeveloperAccountRow>())
                Developers.Add(new DeveloperAccountEntry(this, row));
        }

        private void AddDeveloper()
        {
            var email = (NewDeveloperEmail ?? string.Empty).Trim();
            if (!SesionUsuario.EsSuperAdmin || string.IsNullOrWhiteSpace(email))
                return;

            if (!email.EndsWith("@weg.net", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(LocalizedText.Get("DeveloperAccounts_InvalidDomainMessage", "El correo debe terminar en @weg.net."));
                return;
            }

            if (SuperAdminPolicy.IsSuperAdminEmail(email))
            {
                MessageBox.Show(LocalizedText.Get("DeveloperAccounts_SuperAdminForbidden", "El Super Admin no puede registrarse como Developer."));
                return;
            }

            if (_usuariosDb.ObtenerPorCorreo(email) == null)
            {
                MessageBox.Show(LocalizedText.Get("DeveloperAccounts_MissingBaseUser", "Primero creá el usuario base desde Gestión de usuarios para definir su contraseña."));
                return;
            }

            try
            {
                _developerDb.AddOrEnable(email, SesionUsuario.UsuarioActual?.Correo ?? "superadmin", NewDeveloperNotes);
                LogService.Info("DeveloperAccounts", "Developer habilitado", email);
                NewDeveloperEmail = string.Empty;
                NewDeveloperNotes = string.Empty;
                Refresh();
            }
            catch (Exception ex)
            {
                LogService.Error("DeveloperAccounts", "Error al guardar developer", ex, email);
                MessageBox.Show(LocalizedText.Get("DeveloperAccounts_SaveError", "No fue posible guardar el developer."));
            }
        }

        internal void SetEnabled(DeveloperAccountEntry entry, bool enabled)
        {
            if (!SesionUsuario.EsSuperAdmin || entry == null)
                return;

            try
            {
                if (enabled)
                    _developerDb.AddOrEnable(entry.Email, SesionUsuario.UsuarioActual?.Correo ?? "superadmin", entry.Notes);
                else
                    _developerDb.Disable(entry.Email);

                LogService.Info("DeveloperAccounts", enabled ? "Developer habilitado" : "Developer deshabilitado", entry.Email);
            }
            catch (Exception ex)
            {
                LogService.Error("DeveloperAccounts", "Error al cambiar developer", ex, entry.Email);
                MessageBox.Show(LocalizedText.Get("DeveloperAccounts_SaveError", "No fue posible guardar el developer."));
            }

            Refresh();
        }

        public override void RefreshLocalization()
        {
            RaisePropertyChanges(nameof(Title), nameof(Subtitle), nameof(EmailLabel), nameof(NotesLabel), nameof(AddLabel));
        }
    }

    public sealed class DeveloperAccountEntry : BaseViewModel
    {
        private readonly DeveloperAccountsViewModel _owner;
        private bool _enabled;

        public DeveloperAccountEntry(DeveloperAccountsViewModel owner, DeveloperAccountRow row)
        {
            _owner = owner;
            Email = row?.Email ?? string.Empty;
            CreatedBy = row?.CreatedBy ?? string.Empty;
            Notes = row?.Notes ?? string.Empty;
            _enabled = row?.Enabled ?? false;
        }

        public string Email { get; }
        public string CreatedBy { get; }
        public string Notes { get; }
        public string StatusLabel => Enabled ? LocalizedText.Get("DeveloperAccounts_StatusEnabled", "Habilitado") : LocalizedText.Get("DeveloperAccounts_StatusDisabled", "Deshabilitado");

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;

                _enabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusLabel));
                _owner.SetEnabled(this, value);
            }
        }
    }
}
