using ConcesionaroCarros.Models;
using System;

namespace ConcesionaroCarros.Services
{
    public static class SesionUsuario
    {
        public static Usuario UsuarioActual { get; set; }

        public static bool EsAdmin => UsuarioActual?.Rol == "ADMIN";
        public static bool EsEmpleado => UsuarioActual?.Rol == "EMPLEADO";
        public static bool EsCliente => UsuarioActual?.Rol == "CLIENTE";


        public static event Action<string> FotoPerfilActualizada;

        public static void ActualizarFoto(string ruta)
        {
            if (UsuarioActual == null) return;

            UsuarioActual.FotoPerfil = ruta;

            // fuerza refresco global
            FotoPerfilActualizada?.Invoke(ruta);
        }
    }
}
