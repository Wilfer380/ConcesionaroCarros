using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class InstaladorDbServiceTests
    {
        [TestMethod]
        public void GuardarActualizarEliminar_Works()
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

        [TestMethod]
        public void Guardar_NormalizesEmptyFolderToDesarrolloGlobal()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new InstaladorDbService();
                var ruta = $@"C:\Apps\Global-{Guid.NewGuid():N}.exe";

                service.Guardar(new Instalador
                {
                    Ruta = ruta,
                    Nombre = "GlobalApp",
                    Descripcion = "",
                    Carpeta = ""
                });

                var guardado = service.ObtenerTodos()
                    .FirstOrDefault(x => string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));

                AssertEx.NotNull(guardado, "El instalador debe existir despues de guardarse.");
                AssertEx.Equal("Desarrollo global", guardado.Carpeta,
                    "La carpeta vacia debe normalizarse a Desarrollo global.");
            }
        }

        [TestMethod]
        public void ObtenerTodos_UsesFileNameWhenNombreIsNull()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var ruta = $@"C:\Apps\Viewer-{Guid.NewGuid():N}.exe";

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseInitializer.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        INSERT INTO Instaladores (Ruta, Nombre, Descripcion, Carpeta, FechaRegistro)
                        VALUES ($ruta, NULL, '', 'Desarrollo global', '2026-01-01 00:00:00');";
                        cmd.Parameters.AddWithValue("$ruta", ruta);
                        cmd.ExecuteNonQuery();
                    }
                }

                var service = new InstaladorDbService();
                var guardado = service.ObtenerTodos()
                    .FirstOrDefault(x => string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));

                AssertEx.NotNull(guardado, "El instalador insertado debe recuperarse.");
                AssertEx.Equal(Path.GetFileNameWithoutExtension(ruta), guardado.Nombre,
                    "Cuando el nombre es NULL debe usarse el nombre del archivo.");
            }
        }
    }
}


