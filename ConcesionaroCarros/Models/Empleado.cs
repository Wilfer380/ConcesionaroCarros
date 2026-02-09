using System.ComponentModel;

namespace ConcesionaroCarros.Models
{
    public class Empleado : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string Cargo { get; set; }
        public string Cedula { get; set; }
        public string Ciudad { get; set; }
        public string Departamento { get; set; }
        public string FotoPerfil { get; set; }

        private bool _activo;
        public bool Activo
        {
            get => _activo;
            set
            {
                if (_activo != value)
                {
                    _activo = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Activo)));
                }
            }
        }

        public int MetaVentas { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
