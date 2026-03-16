using ConcesionaroCarros.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class UsuarioModelTests
    {
        [TestMethod]
        public void EstablecerAplicativosAsignados_DeduplicatesAndRoundTrips()
        {
            var usuario = new Usuario();

            usuario.EstablecerAplicativosAsignados(new[]
            {
                @"C:\Apps\ERP.exe",
                @"C:\Apps\ERP.exe",
                @"C:\Apps\Reportes.exe",
                "",
                null
            });

            var asignados = usuario.ObtenerAplicativosAsignados();

            AssertEx.Equal(2, asignados.Count,
                "AplicativosJson debe eliminar duplicados y valores vacios.");
            AssertEx.True(asignados.Contains(@"C:\Apps\ERP.exe"),
                "La lista serializada debe conservar ERP.exe.");
            AssertEx.True(asignados.Contains(@"C:\Apps\Reportes.exe"),
                "La lista serializada debe conservar Reportes.exe.");
            AssertEx.Contains("ERP", usuario.AplicativosResumen,
                "El resumen debe incluir el nombre del ejecutable.");
        }

        [TestMethod]
        public void AplicativosResumen_ReturnsHyphenWhenListIsEmptyOrInvalid()
        {
            var usuario = new Usuario();

            AssertEx.Equal("-", usuario.AplicativosResumen,
                "Cuando no hay aplicativos asignados el resumen debe ser '-'.");

            usuario.AplicativosJson = "json-invalido";

            AssertEx.Equal("-", usuario.AplicativosResumen,
                "Cuando el JSON es invalido el resumen debe seguir siendo '-'.");
            AssertEx.Equal(0, usuario.ObtenerAplicativosAsignados().Count,
                "Cuando el JSON es invalido debe devolverse una lista vacia.");
        }
    }
}

