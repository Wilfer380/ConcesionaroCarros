using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class VentaViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        public ObservableCollection<DetalleFactura> Extras { get; set; } = new ObservableCollection<DetalleFactura>();

        public ICommand ProcesarVentaCommand { get; }

        public VentaViewModel(MainViewModel main)
        {
            _main = main;

            ProcesarVentaCommand = new RelayCommand(_ =>
            {
                var factura = new Factura
                {
                    Cliente = _main.ClienteActual,
                    Empleado = _main.EmpleadoActual,
                    Carro = _main.CarroSeleccionado
                };

                factura.Detalles.Add(new DetalleFactura
                {
                    Concepto = "Precio base vehículo",
                    Valor = _main.CarroSeleccionado.PrecioBase
                });

                foreach (var e in Extras)
                    factura.Detalles.Add(e);

                _main.FacturaActual = factura;
                _main.ShowFacturaCommand.Execute(null);
            });
        }
    }
}
