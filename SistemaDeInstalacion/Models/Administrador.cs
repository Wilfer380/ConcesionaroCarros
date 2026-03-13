using System;

namespace ConcesionaroCarros.Models
{
    public class Administrador
    {
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string UsuarioSistema { get; set; }
        public string Rol { get; set; }
        public string PasswordAdminHash { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
