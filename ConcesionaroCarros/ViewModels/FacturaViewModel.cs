using ConcesionaroCarros.Models;

namespace ConcesionaroCarros.ViewModels
{
    public class FacturaViewModel : BaseViewModel
    {
        public Factura Factura { get; }

        public FacturaViewModel(MainViewModel main)
        {
            Factura = main.FacturaActual;
        }
    }
}

