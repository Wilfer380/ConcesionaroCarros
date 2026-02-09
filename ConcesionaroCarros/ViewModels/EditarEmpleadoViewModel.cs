using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using Microsoft.Win32;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EditarEmpleadoViewModel : BaseViewModel
    {
        private readonly EmpleadosViewModel _parent;

        public Empleado Empleado { get; }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand CambiarFotoCommand { get; }

        public EditarEmpleadoViewModel(EmpleadosViewModel parent, Empleado empleado)
        {
            _parent = parent;
            Empleado = empleado;

            GuardarCommand = new RelayCommand(_ =>
            {
                _parent.GuardarEmpleado(Empleado);
                if (SesionUsuario.UsuarioActual != null &&
                        Empleado.Correo == SesionUsuario.UsuarioActual.Correo)
                {
                    SesionUsuario.ActualizarFoto(Empleado.FotoPerfil);
                }
            });

            CancelarCommand = new RelayCommand(_ =>
            {
                _parent.CerrarModal();
            });

            CambiarFotoCommand = new RelayCommand(_ =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "Imagen (*.png;*.jpg)|*.png;*.jpg";

                if (dlg.ShowDialog() == true)
                {
                    Empleado.FotoPerfil = dlg.FileName;
                    OnPropertyChanged(nameof(Empleado));
                }
            });
        }
    }
}
