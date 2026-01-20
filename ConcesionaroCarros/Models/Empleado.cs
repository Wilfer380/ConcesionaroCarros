using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcesionaroCarros.Models
{
    public class Empleado
    {
       public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string NombreCompleto => $"{Nombres} {Apellidos}";

        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string Cargo { get; set; }
        public bool Activo { get; set; }

        public int MetaVentas { get; set; }

    }
}
