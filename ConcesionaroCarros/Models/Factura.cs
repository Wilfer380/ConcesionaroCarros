using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcesionaroCarros.Models
{
    public class Factura
    {
        public Cliente Cliente { get; set; }
        public Empleado Empleado { get; set; }
        public Carro Carro { get; set; }

        public List<DetalleFactura> Detalles { get; set; } = new List<DetalleFactura>();

        public double Subtotal => Detalles.Sum(d => d.Valor);
        public double Iva => Subtotal * 0.19;
        public double Total => Subtotal + Iva;
    }
}