using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using System;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class MicrosoftRecoveryViewModel : BaseViewModel
    {
        private readonly UsuariosDbService _usuariosDb = new UsuariosDbService();
        private Usuario _usuarioEncontrado;
        private string _correo;
        private string _mensajeEstado;
        private string _codigoVerificacion;
        private string _nuevaPassword;
        private string _confirmarPassword;
        private string _codigoGenerado;
        private bool _noSoyRobot;
        private bool _isValidationStep = true;
        private bool _isPasswordStep;
        private bool _isBusy;

        public MicrosoftRecoveryViewModel()
        {
            ValidarIdentidadCommand = new RelayCommand(_ => ValidarIdentidad());
            CambiarPasswordCommand = new RelayCommand(_ => CambiarPassword());
            VolverCommand = new RelayCommand(_ => Volver());
            CancelarCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }

        public event Action RequestClose;
        public event Action<string, string> RecoveryCompleted;
        public event Action<string> ShowCodeRequested;

        public ICommand ValidarIdentidadCommand { get; }
        public ICommand CambiarPasswordCommand { get; }
        public ICommand VolverCommand { get; }
        public ICommand CancelarCommand { get; }

        public string Correo
        {
            get => _correo;
            set
            {
                _correo = value;
                OnPropertyChanged();
            }
        }

        public bool NoSoyRobot
        {
            get => _noSoyRobot;
            set
            {
                _noSoyRobot = value;
                OnPropertyChanged();
            }
        }

        public string NuevaPassword
        {
            get => _nuevaPassword;
            set
            {
                _nuevaPassword = value;
                OnPropertyChanged();
            }
        }

        public string CodigoVerificacion
        {
            get => _codigoVerificacion;
            set
            {
                _codigoVerificacion = value;
                OnPropertyChanged();
            }
        }

        public string ConfirmarPassword
        {
            get => _confirmarPassword;
            set
            {
                _confirmarPassword = value;
                OnPropertyChanged();
            }
        }

        public string MensajeEstado
        {
            get => _mensajeEstado;
            set
            {
                _mensajeEstado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(MensajeEstado);

        public bool IsValidationStep
        {
            get => _isValidationStep;
            set
            {
                _isValidationStep = value;
                OnPropertyChanged();
            }
        }

        public bool IsPasswordStep
        {
            get => _isPasswordStep;
            set
            {
                _isPasswordStep = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private void ValidarIdentidad()
        {
            if (IsBusy)
                return;

            MensajeEstado = string.Empty;
            var correo = (Correo ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(correo))
            {
                MensajeEstado = LocalizedText.Get("Recovery_MissingEmailMessage", "Ingresa el correo registrado para continuar.");
                return;
            }

            if (!EsCorreoValido(correo))
            {
                MensajeEstado = LocalizedText.Get("Recovery_InvalidEmailMessage", "Ingresa un correo valido.");
                return;
            }

            if (!NoSoyRobot)
            {
                MensajeEstado = LocalizedText.Get("Recovery_NotRobotRequiredMessage", "Debes confirmar la validacion 'No soy un robot'.");
                return;
            }

            try
            {
                IsBusy = true;
                _usuarioEncontrado = _usuariosDb.ObtenerPorCorreo(correo);

                if (_usuarioEncontrado == null)
                {
                    MensajeEstado = LocalizedText.Get("Recovery_UserNotFoundMessage", "No existe un usuario registrado con ese correo.");
                    return;
                }

                Correo = _usuarioEncontrado.Correo;
                _codigoGenerado = GenerarCodigoTemporal();
                MensajeEstado = LocalizedText.Get("Recovery_IdentityValidatedMessage", "Identidad validada. Espera el codigo visual para continuar.");
                ShowCodeRequested?.Invoke(_codigoGenerado);
            }
            catch (Exception ex)
            {
                MensajeEstado = LocalizedText.Get("Recovery_ValidationErrorPrefix", "No se pudo validar el usuario. ") + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CambiarPassword()
        {
            if (IsBusy)
                return;

            if (_usuarioEncontrado == null)
            {
                MensajeEstado = LocalizedText.Get("Recovery_ValidateEmailFirstMessage", "Primero valida el correo registrado.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_codigoGenerado))
            {
                MensajeEstado = LocalizedText.Get("Recovery_RequestCodeFirstMessage", "Primero solicita y visualiza el codigo de validacion.");
                return;
            }

            if (string.IsNullOrWhiteSpace(CodigoVerificacion))
            {
                MensajeEstado = LocalizedText.Get("Recovery_MissingCodeMessage", "Debes ingresar el codigo de validacion mostrado.");
                return;
            }

            if (!string.Equals((CodigoVerificacion ?? string.Empty).Trim(), _codigoGenerado, StringComparison.Ordinal))
            {
                MensajeEstado = LocalizedText.Get("Recovery_InvalidCodeMessage", "El codigo de validacion no es correcto.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevaPassword) || string.IsNullOrWhiteSpace(ConfirmarPassword))
            {
                MensajeEstado = LocalizedText.Get("Recovery_MissingPasswordsMessage", "Debes completar la nueva contrasena y su confirmacion.");
                return;
            }

            if (NuevaPassword.Length < 6)
            {
                MensajeEstado = LocalizedText.Get("Recovery_PasswordLengthMessage", "La nueva contrasena debe tener al menos 6 caracteres.");
                return;
            }

            if (!string.Equals(NuevaPassword, ConfirmarPassword, StringComparison.Ordinal))
            {
                MensajeEstado = LocalizedText.Get("Recovery_PasswordMismatchMessage", "La confirmacion no coincide con la nueva contrasena.");
                return;
            }

            try
            {
                IsBusy = true;
                _usuariosDb.ActualizarPassword(_usuarioEncontrado.Id, NuevaPassword);
                _usuariosDb.RegistrarLogRecuperacionPassword(
                    _usuarioEncontrado.Id,
                    _usuarioEncontrado.Correo,
                    null,
                    false);

                _codigoGenerado = null;
                CodigoVerificacion = string.Empty;
                RecoveryCompleted?.Invoke(_usuarioEncontrado.Correo, NuevaPassword);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MensajeEstado = LocalizedText.Get("Recovery_UpdatePasswordErrorPrefix", "No se pudo actualizar la contrasena. ") + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Volver()
        {
            _usuarioEncontrado = null;
            _codigoGenerado = null;
            NoSoyRobot = false;
            CodigoVerificacion = string.Empty;
            NuevaPassword = string.Empty;
            ConfirmarPassword = string.Empty;
            IsPasswordStep = false;
            IsValidationStep = true;
            MensajeEstado = string.Empty;
        }

        public void ActivarPasoCambioPassword()
        {
            if (_usuarioEncontrado == null)
                return;

            CodigoVerificacion = string.Empty;
            NuevaPassword = string.Empty;
            ConfirmarPassword = string.Empty;
            IsValidationStep = false;
            IsPasswordStep = true;
            MensajeEstado = LocalizedText.Get("Recovery_CodeDisplayedMessage", "Codigo mostrado correctamente. Ingresa ese codigo y define tu nueva contrasena.");
        }

        private static bool EsCorreoValido(string correo)
        {
            try
            {
                var mail = new MailAddress(correo);
                return string.Equals(mail.Address, correo, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string GenerarCodigoTemporal()
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var numero = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return numero.ToString("D6");
        }
    }
}
