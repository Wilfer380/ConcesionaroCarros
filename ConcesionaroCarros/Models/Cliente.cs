using System;

namespace ConcesionaroCarros.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        public string Nombres { get; set; } = "";
        public string Apellidos { get; set; } = "";
        public string Cedula { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Direccion { get; set; } = "";

        public DateTime? FechaNacimiento { get; set; }

        public string CiudadDepartamento { get; set; } = "";

        // Solo empleados usan esto
        public string CargoActual { get; set; }

        public string CodigoPostal { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public string FotoPerfil { get; set; }
    }
}
