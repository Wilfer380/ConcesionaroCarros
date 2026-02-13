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
        public double Ganancia => CarritoVehiculos.Sum(c => c.PrecioVenta - c.Costo);

        
        public string ReferenciaPago { get; set; }

        public ObservableCollection<string> MetodosPago { get; } =
            new ObservableCollection<string> { "Efectivo", "Tarjeta Crédito", "Tarjeta Débito", "Transferencia", "PSE" };

        public string MetodoPagoSeleccionado { get; set; }

        public ObservableCollection<string> TiposOperacion { get; } =
            new ObservableCollection<string> { "Contado", "Crédito", "Leasing" };

        public string TipoOperacionSeleccionada { get; set; }

        public ObservableCollection<string> EstadosPago { get; } =
            new ObservableCollection<string> { "Pendiente", "Pagado", "Parcial" };

        public string EstadoPagoSeleccionado { get; set; }

        public ObservableCollection<string> CanalesRecaudo { get; } =
            new ObservableCollection<string> { "Caja Principal", "Banco", "Plataforma Digital" };

        public string CanalSeleccionado { get; set; }

        private string _asesorResponsable = "Usuario Activo";

        public string AsesorResponsable
        {
            get => _asesorResponsable;
            set
            {
                _asesorResponsable = value;
                OnPropertyChanged(nameof(AsesorResponsable));
            }
        }
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
            
            VolverADatosClienteCommand = new RelayCommand(_ =>
            {
                _puntoVenta.PasoActual = PasoVenta.DatosCliente;

                var vm = new DatosClientesViewModel(_puntoVenta);
                _puntoVenta.ContenidoCentral = new DatosClientesView
                {
                    DataContext = vm
                };
            });

            ConfirmarVentaCommand = new RelayCommand(_ =>
            {
                _puntoVenta.PasoActual = PasoVenta.FacturaGenerada;

                var cliente = _puntoVenta.ClienteActual;
                var empleado = _puntoVenta.AsesorSeleccionado;
               
                _puntoVenta.ContenidoCentral = new FacturaGenerada
                {
                    DataContext = new FacturaGeneradaViewModel(cliente, empleado, CarritoVehiculos)
                };
            });
        }
    }
}
