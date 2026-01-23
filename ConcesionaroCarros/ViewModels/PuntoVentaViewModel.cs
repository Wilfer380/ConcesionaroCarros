using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class PuntoVentaViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        public ObservableCollection<Carro> Carrito => _main.Carrito;

        public decimal Total => Carrito.Sum(c => (decimal)c.PrecioVenta);

        public ICommand QuitarDelCarritoCommand { get; }
        public ICommand FinalizarVentaCommand { get; }

        public PuntoVentaViewModel(MainViewModel main)
        {
            _main = main;

            QuitarDelCarritoCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                _main.Carrito.Remove(carro);
                _main.ActualizarCarrito();
                OnPropertyChanged(nameof(Total));

                if (_main.Carrito.Count == 0)
                {
                    _main.VistaActiva = null;
                    _main.ShowDashboardCommand.Execute(null);
                }
            });

            FinalizarVentaCommand = new RelayCommand(_ =>
            {
                _main.Carrito.Clear();
                _main.ActualizarCarrito();
                _main.VistaActiva = null;
                _main.ShowDashboardCommand.Execute(null);
            });
        }
    }
}
