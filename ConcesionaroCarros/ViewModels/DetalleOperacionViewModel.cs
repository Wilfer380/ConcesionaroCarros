using ConcesionaroCarros.Models;
using System;

namespace ConcesionaroCarros.ViewModels
{
    public class DetalleOperacionViewModel
    {
        public Carro Vehiculo { get; }

        public DetalleOperacionViewModel(Carro vehiculo)
        {
            Vehiculo = vehiculo;
        }

        public decimal PrecioBase =>
            Vehiculo == null
                ? 0
                : (decimal)(Vehiculo.Costo + Vehiculo.PrecioVenta);
        public DateTime FechaActual => DateTime.Now.Date;
    }
}
