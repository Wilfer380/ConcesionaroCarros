using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;

namespace SistemaDeInstalacion.Tests
{
    internal static class AdministradoresDbServiceTests
    {
        public static void GuardarOActualizarYLoginPorUsuarioSistema_Work()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new AdministradoresDbService();

                service.GuardarOActualizar(new Administrador
                {
                    Nombres = "Ana",
                    Apellidos = "Admin",
                    Correo = "ana.admin@weg.net",
                    UsuarioSistema = "ana.admin",
                    Rol = RolesSistema.Administrador
                }, "Admin123");

                AssertEx.True(service.ExistePorUsuarioSistema("ana.admin"),
                    "Debe existir el admin despues de guardarlo.");

                var loginOk = service.LoginPorUsuarioSistema("ana.admin", "Admin123");
                AssertEx.NotNull(loginOk, "El login admin debe funcionar con la clave correcta.");
                AssertEx.Equal("ana.admin@weg.net", loginOk.Correo,
                    "El admin autenticado debe corresponder al correo guardado.");

                var loginFail = service.LoginPorUsuarioSistema("ana.admin", "Incorrecta");
                AssertEx.Null(loginFail, "El login admin no debe funcionar con clave incorrecta.");
            }
        }

        public static void SincronizarDesdeUsuario_UpdatesExistingAdminKeepingPassword()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new AdministradoresDbService();

                service.GuardarOActualizar(new Administrador
                {
                    Nombres = "Carlos",
                    Apellidos = "Admin",
                    Correo = "carlos@weg.net",
                    UsuarioSistema = "carlos.admin",
                    Rol = RolesSistema.Administrador
                }, "Admin123");

                service.SincronizarDesdeUsuario("carlos@weg.net", new Usuario
                {
                    Id = 1,
                    Nombres = "Carlos Andres",
                    Apellidos = "Actualizado",
                    Correo = "carlos.andres@weg.net",
                    Rol = RolesSistema.Administrador
                });

                var login = service.LoginPorUsuarioSistema("carlos.admin", "Admin123");
                AssertEx.NotNull(login,
                    "La sincronizacion no debe invalidar la password admin existente.");
                AssertEx.Equal("carlos.andres@weg.net", login.Correo,
                    "La sincronizacion debe actualizar el correo del administrador.");
                AssertEx.Equal("Carlos Andres", login.Nombres,
                    "La sincronizacion debe actualizar los nombres del administrador.");
            }
        }
    }
}
