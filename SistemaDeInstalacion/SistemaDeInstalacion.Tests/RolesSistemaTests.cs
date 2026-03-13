using ConcesionaroCarros.Services;
using System.Linq;

namespace SistemaDeInstalacion.Tests
{
    internal static class RolesSistemaTests
    {
        public static void AsignablesSinAdmin_ExcludesAdministrator()
        {
            AssertEx.True(RolesSistema.Todos.Contains(RolesSistema.Administrador),
                "La lista completa de roles debe incluir ADMINISTRADOR.");
            AssertEx.False(RolesSistema.AsignablesSinAdmin.Contains(RolesSistema.Administrador),
                "Los roles asignables sin admin no deben incluir ADMINISTRADOR.");
            AssertEx.True(RolesSistema.EsAdministrador("administrador"),
                "La deteccion de admin debe ser case-insensitive.");
        }
    }
}
