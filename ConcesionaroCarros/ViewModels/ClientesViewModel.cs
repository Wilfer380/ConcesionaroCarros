using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class ClientesViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        public Cliente Cliente { get; set; } = new Cliente();

        public ICommand GuardarClienteCommand { get; }

        public ClientesViewModel(MainViewModel main)
        {
            _main = main;

            GuardarClienteCommand = new RelayCommand(_ =>
            {
                _main.ClienteActual = Cliente;
                _main.ShowEmpleadosCommand.Execute(null);
            });
        }
    }
}
