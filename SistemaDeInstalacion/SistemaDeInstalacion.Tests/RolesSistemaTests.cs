using ConcesionaroCarros.Services;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class RolesSistemaTests
    {
        [TestMethod]
        public void AsignablesSinAdmin_ExcludesAdministrator()
        {
            AssertEx.True(RolesSistema.Todos.Contains(RolesSistema.Administrador),
                "La lista completa de roles debe incluir ADMINISTRADOR.");
            AssertEx.False(RolesSistema.AsignablesSinAdmin.Contains(RolesSistema.Administrador),
                "Los roles asignables sin admin no deben incluir ADMINISTRADOR.");
            AssertEx.True(RolesSistema.EsAdministrador("administrador"),
                "La deteccion de admin debe ser case-insensitive.");
        }

        [TestMethod]
        public void Todos_ContainsExpectedCorporateRoles()
        {
            AssertEx.True(RolesSistema.Todos.Contains(RolesSistema.RRHH),
                "La lista de roles debe incluir RRHH.");
            AssertEx.True(RolesSistema.Todos.Contains(RolesSistema.Ingenieria),
                "La lista de roles debe incluir INGENIERIA.");
            AssertEx.True(RolesSistema.Todos.Contains(RolesSistema.SistemasTI),
                "La lista de roles debe incluir SISTEMAS (TI).");
            AssertEx.Equal(16, RolesSistema.Todos.Count,
                "La lista completa de roles debe conservar la cantidad esperada.");
        }
    }
}

