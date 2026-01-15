using System.ComponentModel;

namespace ConcesionaroCarros.Models
{
    public class Carro : INotifyPropertyChanged
    {
        public int Id { get; set; }

        private string _marca;
        private string _modelo;
        private int _año;
        private string _color;
        private double _costo;
        private double _precioVenta;
        private string _estado;
        private string _descripcion;
        private string _imagenPath;

        public string Marca
        {
            get => _marca;
            set { _marca = value; OnPropertyChanged(nameof(Marca)); }
        }

        public string Modelo
        {
            get => _modelo;
            set { _modelo = value; OnPropertyChanged(nameof(Modelo)); }
        }

        public int Año
        {
            get => _año;
            set { _año = value; OnPropertyChanged(nameof(Año)); }
        }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public double Costo
        {
            get => _costo;
            set { _costo = value; OnPropertyChanged(nameof(Costo)); }
        }

        public double PrecioVenta
        {
            get => _precioVenta;
            set { _precioVenta = value; OnPropertyChanged(nameof(PrecioVenta)); }
        }

        public string Estado
        {
            get => _estado;
            set { _estado = value; OnPropertyChanged(nameof(Estado)); }
        }

        public string Descripcion
        {
            get => _descripcion;
            set { _descripcion = value; OnPropertyChanged(nameof(Descripcion)); }
        }

        public string ImagenPath
        {
            get => _imagenPath;
            set { _imagenPath = value; OnPropertyChanged(nameof(ImagenPath)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
