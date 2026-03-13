using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using System;
using System.Linq;

namespace SistemaDeInstalacion.Tests
{
    internal static class InstaladorDbServiceTests
    {
        public static void GuardarActualizarEliminar_Works()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new InstaladorDbService();
                var ruta = $@"C:\Apps\Herramienta-{Guid.NewGuid():N}.exe";

                service.Guardar(new Instalador
                {
                    Ruta = ruta,
                    Nombre = "Herramienta",
                    Descripcion = "Version inicial",
                    Carpeta = "Desarrollo global"
                });

                var lista = service.ObtenerTodos();
                var guardado = lista.FirstOrDefault(x => string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));
                AssertEx.NotNull(guardado,
                    "Debe existir el instalador recien guardado.");
                AssertEx.Equal("Herramienta", guardado.Nombre,
                    "El nombre guardado debe persistirse.");

                service.Actualizar(new Instalador
                {
                    Ruta = ruta,
                    Nombre = "Herramienta",
                    Descripcion = "Version actualizada",
                    Carpeta = "Punto local de desarrollo planta"
                });

                var actualizado = service.ObtenerTodos()
                    .FirstOrDefault(x => string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));
                AssertEx.NotNull(actualizado,
                    "El instalador actualizado debe seguir existiendo.");
                AssertEx.Equal("Version actualizada", actualizado.Descripcion,
                    "La descripcion debe actualizarse.");
                AssertEx.Equal("Punto local de desarrollo planta", actualizado.Carpeta,
                    "La carpeta debe actualizarse.");

                service.EliminarRuta(ruta);
                var eliminado = service.ObtenerTodos()
                    .FirstOrDefault(x => string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));
                AssertEx.Null(eliminado, "El instalador debe eliminarse por su ruta.");
            }
        }
    }
}
