using System;
using System.Collections.Generic;

namespace SistemaDeInstalacion.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            var tests = new List<(string Name, Action Run)>
            {
                ("DatabaseInitializer crea la base y las tablas", DatabaseInitializerTests.Initialize_CreatesDatabaseAndTables),
                ("DatabaseInitializer migra legacy y normaliza datos", DatabaseInitializerTests.Initialize_MigratesLegacyDatabaseAndNormalizesData),
                ("Usuario serializa aplicativos sin duplicados", UsuarioModelTests.EstablecerAplicativosAsignados_DeduplicatesAndRoundTrips),
                ("RolesSistema excluye admin de roles asignables", RolesSistemaTests.AsignablesSinAdmin_ExcludesAdministrator),
                ("UsuariosDbService registra y autentica usuarios", UsuariosDbServiceTests.RegistrarYLogin_WorksWithHashedPassword),
                ("UsuariosDbService resuelve correo por alias y correo", UsuariosDbServiceTests.ObtenerCorreoPorUsuarioLogin_ResolvesAliasAndEmail),
                ("AdministradoresDbService guarda y autentica admin", AdministradoresDbServiceTests.GuardarOActualizarYLoginPorUsuarioSistema_Work),
                ("AdministradoresDbService sincroniza cambios desde usuario", AdministradoresDbServiceTests.SincronizarDesdeUsuario_UpdatesExistingAdminKeepingPassword),
                ("InstaladorDbService guarda, actualiza y elimina", InstaladorDbServiceTests.GuardarActualizarEliminar_Works)
            };

            var passed = 0;

            foreach (var test in tests)
            {
                try
                {
                    test.Run();
                    passed++;
                    Console.WriteLine("[OK] " + test.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[FAIL] " + test.Name);
                    Console.WriteLine(ex.ToString());
                }
            }

            Console.WriteLine();
            Console.WriteLine("Resultado: {0}/{1} tests aprobados.", passed, tests.Count);
            return passed == tests.Count ? 0 : 1;
        }
    }
}
