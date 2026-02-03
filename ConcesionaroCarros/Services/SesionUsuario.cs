using ConcesionaroCarros.Models;

namespace ConcesionaroCarros.Services
{
    public static class SesionUsuario
    {
        public static Usuario UsuarioActual { get; set; }

        public static bool EsAdmin => UsuarioActual?.Rol == "ADMIN";
        public static bool EsEmpleado => UsuarioActual?.Rol == "EMPLEADO";
        public static bool EsCliente => UsuarioActual?.Rol == "CLIENTE";
    }
}
