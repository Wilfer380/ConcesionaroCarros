using System;

namespace ConcesionaroCarros.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string PasswordHash { get; set; }
        public string Rol { get; set; }

        public DateTime FechaRegistro { get; set; }
        public string FotoPerfil { get; set; }

    }
}
