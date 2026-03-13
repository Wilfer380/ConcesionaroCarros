using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;

namespace SistemaDeInstalacion.Tests
{
    internal static class UsuariosDbServiceTests
    {
        public static void RegistrarYLogin_WorksWithHashedPassword()
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

        public static void ObtenerCorreoPorUsuarioLogin_ResolvesAliasAndEmail()
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
    }
}
