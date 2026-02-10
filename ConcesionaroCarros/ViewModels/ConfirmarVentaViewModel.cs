using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Enums;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class ConfirmarVentaViewModel : BaseViewModel
    {
        private readonly PuntoVentaViewModel _puntoVenta;

        public ICommand VolverADatosClienteCommand { get; }
        public ICommand ConfirmarVentaCommand { get; }
        public ObservableCollection<Carro> CarritoVehiculos { get; set; }

        public double Subtotal => CarritoVehiculos.Sum(c => c.PrecioVenta);

        public double Iva => Subtotal * 0.19;

        public double TotalFinal => Subtotal + Iva;

        // opcional (ganancia)
        public double Ganancia =>
            CarritoVehiculos.Sum(c => c.PrecioVenta - c.Costo);

        // NUEVO
        public ConfirmarVentaViewModel(PuntoVentaViewModel puntoVenta)
        {
            _puntoVenta = puntoVenta;

            CarritoVehiculos = _puntoVenta.Carrito;

            CarritoVehiculos.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Iva));
                OnPropertyChanged(nameof(TotalFinal));
                OnPropertyChanged(nameof(Ganancia));
            };

            // NUEVO → volver a datos del cliente
            VolverADatosClienteCommand = new RelayCommand(_ =>
            {
                _puntoVenta.PasoActual = PasoVenta.DatosCliente;

                var vm = new DatosClientesViewModel(_puntoVenta);
                _puntoVenta.ContenidoCentral = new DatosClientesView
                {
                    DataContext = vm
                };
            });

            // NUEVO → ir a factura generada
            ConfirmarVentaCommand = new RelayCommand(_ =>
            {
                _puntoVenta.PasoActual = PasoVenta.FacturaGenerada;

                _puntoVenta.ContenidoCentral = new FacturaGenerada
                {
                    DataContext = new FacturaGeneradaViewModel()
                };
            });
        }
    }
}
