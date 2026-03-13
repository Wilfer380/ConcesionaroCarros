using ConcesionaroCarros.Models;

namespace SistemaDeInstalacion.Tests
{
    internal static class UsuarioModelTests
    {
        public static void EstablecerAplicativosAsignados_DeduplicatesAndRoundTrips()
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
    }
}
