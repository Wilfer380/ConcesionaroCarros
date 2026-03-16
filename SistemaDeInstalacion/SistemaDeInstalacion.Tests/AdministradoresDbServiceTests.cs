using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class AdministradoresDbServiceTests
    {
        [TestMethod]
        public void GuardarOActualizarYLoginPorUsuarioSistema_Work()
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

        [TestMethod]
        public void SincronizarDesdeUsuario_UpdatesExistingAdminKeepingPassword()
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

        [TestMethod]
        public void EliminarPorCorreo_RemovesAdministratorRecord()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new AdministradoresDbService();

                service.GuardarOActualizar(new Administrador
                {
                    Nombres = "Luisa",
                    Apellidos = "Admin",
                    Correo = "luisa.admin@weg.net",
                    UsuarioSistema = "luisa.admin",
                    Rol = RolesSistema.Administrador
                }, "Admin123");

                AssertEx.True(service.ExistePorUsuarioSistema("luisa.admin"),
                    "El administrador debe existir antes de eliminarse.");

                service.EliminarPorCorreo("luisa.admin@weg.net");

                AssertEx.False(service.ExistePorUsuarioSistema("luisa.admin"),
                    "El administrador no debe existir despues de eliminarse.");
            }
        }

        [TestMethod]
        public void GuardarOActualizar_UpdatesAdminPasswordOnSecondSave()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new AdministradoresDbService();

                service.GuardarOActualizar(new Administrador
                {
                    Nombres = "Pedro",
                    Apellidos = "Admin",
                    Correo = "pedro.admin@weg.net",
                    UsuarioSistema = "pedro.admin",
                    Rol = RolesSistema.Administrador
                }, "Admin123");

                service.GuardarOActualizar(new Administrador
                {
                    Nombres = "Pedro",
                    Apellidos = "Admin",
                    Correo = "pedro.admin@weg.net",
                    UsuarioSistema = "pedro.admin",
                    Rol = RolesSistema.Administrador
                }, "NuevaAdmin456");

                AssertEx.Null(service.LoginPorUsuarioSistema("pedro.admin", "Admin123"),
                    "La clave anterior no debe seguir funcionando.");
                AssertEx.NotNull(service.LoginPorUsuarioSistema("pedro.admin", "NuevaAdmin456"),
                    "La nueva clave administrativa debe autenticar.");
            }
        }
    }
}

