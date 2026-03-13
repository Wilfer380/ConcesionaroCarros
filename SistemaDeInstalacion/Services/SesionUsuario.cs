using ConcesionaroCarros.Models;
using System;

namespace ConcesionaroCarros.Services
{
    public static class SesionUsuario
    {
        public static Usuario UsuarioActual { get; set; }
        public static bool ModoAdministrador { get; set; }

        public static bool EsAdmin =>
            ModoAdministrador && RolesSistema.EsAdministrador(UsuarioActual?.Rol);
        public static bool EsEmpleado => UsuarioActual != null && !EsAdmin;
        public static bool EsCliente => UsuarioActual != null && !EsAdmin;


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
