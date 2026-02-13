using ConcesionaroCarros.Models;
using System;
using System.Collections.ObjectModel;

namespace ConcesionaroCarros.ViewModels
{
    public class FacturaGeneradaViewModel : BaseViewModel
    {
        public string IdFactura { get; } = $"FV-{DateTime.Now:yyyyMMddHHmmss}";
        public DateTime FechaFactura { get; } = DateTime.Now;

        public Cliente Cliente { get; }
        public Empleado Empleado { get; }

        // 🔥 LISTA DE CARROS
        public ObservableCollection<Carro> CarrosSeleccionados { get; }

        // 🔥 carro que se está renderizando
        private Carro _carroActual;
        public Carro CarroActual
        {
            get => _carroActual;
            set
            {
                _carroActual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Iva));
                OnPropertyChanged(nameof(Total));
            }
        }

        public double Subtotal => CarroActual?.PrecioVenta ?? 0;
        public double Iva => Subtotal * 0.19;
        public double Total => Subtotal + Iva;

        public FacturaGeneradaViewModel(
            Cliente cliente,
            Empleado empleado,
            ObservableCollection<Carro> carros)
        {
            Cliente = cliente;
            Empleado = empleado;
            CarrosSeleccionados = carros;

            if (carros.Count > 0)
                CarroActual = carros[0];
        }
    }
}
