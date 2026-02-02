using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EditarClienteViewModel : BaseViewModel
    {
        private readonly ClientesViewModel _parent;

        public Cliente Cliente { get; set; }

        public ICommand GuardarCommand { get; }
        public ICommand CerrarCommand { get; }

        public EditarClienteViewModel(ClientesViewModel parent, Cliente cliente)
        {
            _parent = parent;

            Cliente = cliente;

            // 🔹 SOLO inicializa si es nuevo
            if (Cliente.Id == 0)
            {
                Cliente.FechaRegistro = DateTime.Now;
            }

            GuardarCommand = new RelayCommand(_ =>
            {
                _parent.GuardarCliente(Cliente);
            });

            CerrarCommand = new RelayCommand(_ =>
            {
                _parent.CerrarModal();
            });
        }
    }
}
