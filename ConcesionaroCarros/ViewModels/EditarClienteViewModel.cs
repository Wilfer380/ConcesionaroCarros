using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System;
using System.Windows.Input;
using Microsoft.Win32;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Services;

namespace ConcesionaroCarros.ViewModels
{
    public class EditarClienteViewModel : BaseViewModel
    {
        private readonly ClientesViewModel _parent;

        public Cliente Cliente { get; set; }

        public ICommand GuardarCommand { get; }
        public ICommand CerrarCommand { get; }
        public ICommand CambiarFotoCommand { get; }

        public EditarClienteViewModel(ClientesViewModel parent, Cliente cliente)
        {
            _parent = parent;

            Cliente = cliente;

            
            if (Cliente.Id == 0)
            {
                Cliente.FechaRegistro = DateTime.Now;
            }

            GuardarCommand = new RelayCommand(_ =>
            {
                _parent.GuardarCliente(Cliente);

                if (SesionUsuario.UsuarioActual != null &&
                    !string.IsNullOrEmpty(Cliente.Correo) &&
                    Cliente.Correo == SesionUsuario.UsuarioActual.Correo)
                {
                    SesionUsuario.ActualizarFoto(Cliente.FotoPerfil);
                }
            });


            CerrarCommand = new RelayCommand(_ =>
            {
                _parent.CerrarModal();
            });

            CambiarFotoCommand = new RelayCommand(_ =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "Imagen (*.png;*.jpg)|*.png;*.jpg";

                if (dlg.ShowDialog() == true)
                {
                    Cliente.FotoPerfil = dlg.FileName;
                    OnPropertyChanged(nameof(Cliente));

                    var db = new UsuariosDbService();
                    db.ActualizarFotoPerfil(Cliente.Id, dlg.FileName);

                    // 🔥 actualizar sesión
                    if (SesionUsuario.UsuarioActual != null &&
                        SesionUsuario.UsuarioActual.Id == Cliente.Id)
                    {
                        SesionUsuario.ActualizarFoto(dlg.FileName);
                    }
                }
            });

        }
    }
}
        
