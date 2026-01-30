using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Enums;
using ConcesionaroCarros.Views;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class DatosClientesViewModel : BaseViewModel
    {
        private readonly PuntoVentaViewModel _puntoVenta;

        public ICommand VolverAtrasCommand { get; }
        public ICommand ConfirmarDatosCommand { get; }

        public DatosClientesViewModel(PuntoVentaViewModel puntoVenta)
        {
            _puntoVenta = puntoVenta;

            VolverAtrasCommand = new RelayCommand(_ =>
            {
                _puntoVenta.PasoActual = PasoVenta.DetalleOperacion;
                _puntoVenta.ContenidoCentral = _puntoVenta;
            });

            ConfirmarDatosCommand = new RelayCommand(_ =>
            {
                _puntoVenta.IrAConfirmarVenta();
            });
        }
    }
}
