using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class UsuariosDbServiceTests
    {
        [TestMethod]
        public void RegistrarYLogin_WorksWithHashedPassword()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new UsuariosDbService();

                var usuario = new Usuario
                {
                    Nombres = "Juan",
                    Apellidos = "Perez",
                    Correo = "juan.perez@weg.net",
                    Telefono = "123456",
                    Rol = "VENTAS"
                };

                var registrado = service.Registrar(usuario, "Clave123");
                AssertEx.True(registrado, "Registrar debe devolver true para un usuario valido.");

                var loginOk = service.Login("juan.perez@weg.net", "Clave123");
                AssertEx.NotNull(loginOk, "Login debe devolver el usuario cuando la clave es correcta.");
                AssertEx.Equal("juan.perez@weg.net", loginOk.Correo,
                    "Login debe devolver el usuario correcto.");

                var loginFail = service.Login("juan.perez@weg.net", "ClaveIncorrecta");
                AssertEx.Null(loginFail, "Login no debe autenticar con una clave incorrecta.");

                using (var conn = new SqliteConnection(DatabaseInitializer.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT PasswordHash FROM Usuarios WHERE Correo = $correo;";
                        cmd.Parameters.AddWithValue("$correo", "juan.perez@weg.net");
                        var hash = Convert.ToString(cmd.ExecuteScalar());

                        AssertEx.True(!string.IsNullOrWhiteSpace(hash),
                            "El hash de password debe quedar persistido.");
                        AssertEx.False(string.Equals(hash, "Clave123", StringComparison.Ordinal),
                            "La password no debe almacenarse en texto plano.");
                    }
                }
            }
        }

        [TestMethod]
        public void ObtenerCorreoPorUsuarioLogin_ResolvesAliasAndEmail()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new UsuariosDbService();

                var registrado = service.Registrar(new Usuario
                {
                    Nombres = "Maria",
                    Apellidos = "Lopez",
                    Correo = "maria.lopez@weg.net",
                    Telefono = "",
                    Rol = "RRHH"
                }, "Clave123");

                AssertEx.True(registrado, "El usuario de prueba debe registrarse correctamente.");

                var porAlias = service.ObtenerCorreoPorUsuarioLogin("maria.lopez", "pc-local", "Maria Lopez");
                AssertEx.Equal("maria.lopez@weg.net", porAlias,
                    "Debe poder resolver el correo a partir del alias.");

                var porCorreo = service.ObtenerCorreoPorUsuarioLogin("maria.lopez@weg.net", "pc-local", "Maria Lopez");
                AssertEx.Equal("maria.lopez@weg.net", porCorreo,
                    "Debe poder resolver el correo cuando se ingresa el correo completo.");
            }
        }

        [TestMethod]
        public void Registrar_ReturnsFalseWhenCorreoYaExiste()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new UsuariosDbService();

                var usuario = new Usuario
                {
                    Nombres = "Laura",
                    Apellidos = "Rios",
                    Correo = "laura.rios@weg.net",
                    Telefono = "3000000",
                    Rol = "CALIDAD"
                };

                var primero = service.Registrar(usuario, "Clave123");
                var segundo = service.Registrar(usuario, "Clave456");

                AssertEx.True(primero, "El primer registro debe guardarse.");
                AssertEx.False(segundo, "El segundo registro con el mismo correo debe fallar.");
            }
        }

        [TestMethod]
        public void ActualizarPassword_InvalidatesOldPasswordAndAcceptsNewPassword()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new UsuariosDbService();

                var id = service.RegistrarYRetornarId(new Usuario
                {
                    Nombres = "Paula",
                    Apellidos = "Mendez",
                    Correo = "paula.mendez@weg.net",
                    Telefono = "",
                    Rol = "RRHH"
                }, "Inicial123");

                service.ActualizarPassword(id, "Nueva456");

                AssertEx.Null(service.Login("paula.mendez@weg.net", "Inicial123"),
                    "La contraseña anterior ya no debe autenticar.");
                AssertEx.NotNull(service.Login("paula.mendez@weg.net", "Nueva456"),
                    "La contraseña nueva debe autenticar.");
            }
        }

        [TestMethod]
        public void ActualizarAplicativosJson_PersistsAssignedApplications()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new UsuariosDbService();

                var id = service.RegistrarYRetornarId(new Usuario
                {
                    Nombres = "Mario",
                    Apellidos = "Castro",
                    Correo = "mario.castro@weg.net",
                    Telefono = "",
                    Rol = "SST"
                }, "Clave123");

                var usuario = service.ObtenerPorCorreo("mario.castro@weg.net");
                usuario.EstablecerAplicativosAsignados(new[]
                {
                    @"C:\Apps\ERP.exe",
                    @"C:\Apps\Reportes.exe"
                });

                service.ActualizarAplicativosJson(id, usuario.AplicativosJson);

                var recargado = service.ObtenerPorCorreo("mario.castro@weg.net");
                AssertEx.Equal(2, recargado.ObtenerAplicativosAsignados().Count,
                    "La lista de aplicativos asignados debe persistirse.");
                AssertEx.Contains("ERP", recargado.AplicativosResumen,
                    "El resumen debe reflejar los aplicativos guardados.");
            }
        }

        [TestMethod]
        public void EliminarConDependencias_RemovesUserAndPasswordRecoveryLogs()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();
                var service = new UsuariosDbService();

                var id = service.RegistrarYRetornarId(new Usuario
                {
                    Nombres = "Sara",
                    Apellidos = "Lozano",
                    Correo = "sara.lozano@weg.net",
                    Telefono = "",
                    Rol = "MARKETING"
                }, "Clave123");

                service.RegistrarLogRecuperacionPassword(
                    id,
                    "sara.lozano@weg.net",
                    "admin@weg.net",
                    false);

                service.EliminarConDependencias(id, "sara.lozano@weg.net");

                AssertEx.Null(service.ObtenerPorCorreo("sara.lozano@weg.net"),
                    "El usuario debe eliminarse.");

                using (var conn = new SqliteConnection(DatabaseInitializer.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(1) FROM PasswordRecoveryLog WHERE UsuarioId = $id;";
                        cmd.Parameters.AddWithValue("$id", id);
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        AssertEx.Equal(0, count,
                            "Los logs de recuperación del usuario también deben eliminarse.");
                    }
                }
            }
        }
    }
}

