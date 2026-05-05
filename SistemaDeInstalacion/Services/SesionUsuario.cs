using ConcesionaroCarros.Models;
using System;

namespace ConcesionaroCarros.Services
{
    public static class SesionUsuario
    {
        public static Usuario UsuarioActual { get; set; }
        public static bool ModoAdministrador { get; set; }
        public static PrivilegedProfile PerfilPrivilegiado { get; set; } = PrivilegedProfile.None;

        public static bool EsAdmin =>
            PerfilPrivilegiado == PrivilegedProfile.Admin &&
            ModoAdministrador &&
            RolesSistema.EsAdministrador(UsuarioActual?.Rol);

        public static bool EsDeveloper =>
            UsuarioActual != null &&
            ModoAdministrador &&
            PerfilPrivilegiado == PrivilegedProfile.Developer;

        public static bool EsPrivilegiado => EsAdmin || EsDeveloper;

        public static bool EsSuperAdmin =>
            EsAdmin && SuperAdminPolicy.IsSuperAdminEmail(UsuarioActual?.Correo);

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
