using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Enums;
using ConcesionaroCarros.Views;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class ConfirmarVentaViewModel : BaseViewModel
    {
        private readonly PuntoVentaViewModel _puntoVenta;

        public ICommand VolverADatosClienteCommand { get; }
        public ICommand ConfirmarVentaCommand { get; }

        // NUEVO
        public ConfirmarVentaViewModel(PuntoVentaViewModel puntoVenta)
        {
            _puntoVenta = puntoVenta;

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
