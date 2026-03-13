using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ConcesionaroCarros.Db
{
    public class UsuariosDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        private SqliteConnection CreateOpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA busy_timeout = 5000;";
                pragma.ExecuteNonQuery();
            }

            return conn;
        }

        private static bool IsDatabaseLocked(SqliteException ex)
        {
            return ex != null && ex.SqliteErrorCode == 5;
        }

        private static void WaitBeforeRetry(int attempt)
        {
            Thread.Sleep(120 * (attempt + 1));
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public bool Registrar(Usuario usuario, string password)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using (var conn = CreateOpenConnection())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
                        @"
                        INSERT INTO Usuarios
                        (Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro, FotoPerfil, AplicativosJson)
                        VALUES
                        ($nombres, $apellidos, $correo, $telefono, $pass, $rol, $fecha, null, '[]');
                        ";

                        cmd.Parameters.AddWithValue("$nombres", usuario.Nombres ?? string.Empty);
                        cmd.Parameters.AddWithValue("$apellidos", usuario.Apellidos ?? string.Empty);
                        cmd.Parameters.AddWithValue("$correo", usuario.Correo ?? string.Empty);
                        cmd.Parameters.AddWithValue("$telefono", usuario.Telefono ?? string.Empty);
                        cmd.Parameters.AddWithValue("$pass", HashPassword(password));
                        cmd.Parameters.AddWithValue("$rol", usuario.Rol ?? string.Empty);
                        cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (SqliteException ex) when (IsDatabaseLocked(ex) && attempt < 2)
                {
                    WaitBeforeRetry(attempt);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public Usuario Login(string correo, string password)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Id, Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro, FotoPerfil, AplicativosJson
                FROM Usuarios
                WHERE Correo = $correo AND PasswordHash = $pass;
                ";

                cmd.Parameters.AddWithValue("$correo", correo ?? string.Empty);
                cmd.Parameters.AddWithValue("$pass", HashPassword(password));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return MapUsuario(reader);
                }
            }
        }

        public Usuario ObtenerPorCorreo(string correo)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Id, Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro, FotoPerfil, AplicativosJson
                FROM Usuarios
                WHERE Correo = $c
                LIMIT 1;
                ";

                cmd.Parameters.AddWithValue("$c", correo ?? string.Empty);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return MapUsuario(reader);
                }
            }
        }

        public string ObtenerCorreoPorUsuarioDispositivo(string usuarioDispositivo, string nombreVisible)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Correo
                FROM Usuarios
                WHERE UPPER(TRIM(Nombres || ' ' || IFNULL(Apellidos,''))) = UPPER(TRIM($nombreVisible))
                   OR UPPER(TRIM(Nombres)) = UPPER(TRIM($usuario))
                ORDER BY Id DESC
                LIMIT 1;
                ";

                cmd.Parameters.AddWithValue("$usuario", usuarioDispositivo ?? string.Empty);
                cmd.Parameters.AddWithValue("$nombreVisible", nombreVisible ?? string.Empty);

                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value
                    ? null
                    : Convert.ToString(result);
            }
        }

        public string ObtenerCorreoPorUsuarioLogin(
            string usuarioLogin,
            string usuarioDispositivo,
            string nombreVisible)
        {
            var usuario = (usuarioLogin ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(usuario))
                return null;

            if (usuario.Contains("@"))
            {
                using (var conn = CreateOpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                    SELECT Correo
                    FROM Usuarios
                    WHERE UPPER(TRIM(Correo)) = UPPER(TRIM($correo))
                    ORDER BY Id DESC
                    LIMIT 1;
                    ";

                    cmd.Parameters.AddWithValue("$correo", usuario);
                    var correoExacto = cmd.ExecuteScalar();
                    if (correoExacto != null && correoExacto != DBNull.Value)
                        return Convert.ToString(correoExacto);
                }
            }

            if (string.Equals(usuario, (usuarioDispositivo ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var correoDispositivo = ObtenerCorreoPorUsuarioDispositivo(usuarioDispositivo, nombreVisible);
                if (!string.IsNullOrWhiteSpace(correoDispositivo))
                    return correoDispositivo;
            }

            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Correo
                FROM Usuarios
                WHERE UPPER(TRIM(
                    CASE
                        WHEN instr(Correo, '@') > 1 THEN substr(Correo, 1, instr(Correo, '@') - 1)
                        ELSE Correo
                    END
                )) = UPPER(TRIM($usuario))
                ORDER BY Id DESC
                LIMIT 1;
                ";

                cmd.Parameters.AddWithValue("$usuario", usuario);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value
                    ? null
                    : Convert.ToString(result);
            }
        }

        public IEnumerable<Usuario> ObtenerTodos()
        {
            var usuarios = new List<Usuario>();

            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Id, Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro, FotoPerfil, AplicativosJson
                FROM Usuarios;
                ";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        usuarios.Add(MapUsuario(reader));
                }
            }

            return usuarios;
        }

        public void Eliminar(int id)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Usuarios WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void EliminarConDependencias(int id, string correo)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                    DELETE FROM PasswordRecoveryLog
                    WHERE UsuarioId = $id
                       OR UPPER(TRIM(CorreoUsuario)) = UPPER(TRIM($correo));

                    DELETE FROM Usuarios
                    WHERE Id = $id;
                    ";

                    cmd.Parameters.AddWithValue("$id", id);
                    cmd.Parameters.AddWithValue("$correo", correo ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void Actualizar(Usuario u)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                    UPDATE Usuarios SET
                        Nombres = $n,
                        Apellidos = $a,
                        Correo = $c,
                        Telefono = $t,
                        Rol = $r
                    WHERE Id = $id;
                    ";

                    cmd.Parameters.AddWithValue("$id", u.Id);
                    cmd.Parameters.AddWithValue("$n", u.Nombres ?? string.Empty);
                    cmd.Parameters.AddWithValue("$a", u.Apellidos ?? string.Empty);
                    cmd.Parameters.AddWithValue("$c", u.Correo ?? string.Empty);
                    cmd.Parameters.AddWithValue("$t", u.Telefono ?? string.Empty);
                    cmd.Parameters.AddWithValue("$r", u.Rol ?? string.Empty);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void ActualizarFotoPerfil(int idUsuario, string rutaFoto)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    UPDATE Usuarios
                    SET FotoPerfil = $foto
                    WHERE Id = $id;
                    ";

                    cmd.Parameters.AddWithValue("$foto",
                        string.IsNullOrWhiteSpace(rutaFoto) ? (object)DBNull.Value : rutaFoto);
                    cmd.Parameters.AddWithValue("$id", idUsuario);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void ActualizarAplicativosJson(int idUsuario, string aplicativosJson)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    UPDATE Usuarios
                    SET AplicativosJson = $apps
                    WHERE Id = $id;
                    ";

                    cmd.Parameters.AddWithValue("$apps",
                        string.IsNullOrWhiteSpace(aplicativosJson) ? "[]" : aplicativosJson);
                    cmd.Parameters.AddWithValue("$id", idUsuario);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void ActualizarPassword(int id, string password)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                    UPDATE Usuarios SET
                        PasswordHash = $p
                    WHERE Id = $id;
                    ";

                    cmd.Parameters.AddWithValue("$id", id);
                    cmd.Parameters.AddWithValue("$p", HashPassword(password));

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void RegistrarLogRecuperacionPassword(
            int idUsuario,
            string correoUsuario,
            string correoAdministrador,
            bool validadoMicrosoft)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                    INSERT INTO PasswordRecoveryLog
                    (UsuarioId, CorreoUsuario, CorreoAdministrador, ValidadoMicrosoft, FechaRecuperacion)
                    VALUES
                    ($usuarioId, $correoUsuario, $correoAdmin, $validadoMicrosoft, $fecha);
                    ";

                    cmd.Parameters.AddWithValue("$usuarioId", idUsuario);
                    cmd.Parameters.AddWithValue("$correoUsuario", correoUsuario ?? string.Empty);
                    cmd.Parameters.AddWithValue("$correoAdmin",
                        string.IsNullOrWhiteSpace(correoAdministrador)
                            ? (object)DBNull.Value
                            : correoAdministrador);
                    cmd.Parameters.AddWithValue("$validadoMicrosoft", validadoMicrosoft ? 1 : 0);
                    cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public int RegistrarYRetornarId(Usuario usuario, string password)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using (var conn = CreateOpenConnection())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
                        @"
                        INSERT INTO Usuarios
                        (Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro, FotoPerfil, AplicativosJson)
                        VALUES
                        ($nombres, $apellidos, $correo, $telefono, $pass, $rol, $fecha, null, '[]');
                        SELECT last_insert_rowid();
                        ";

                        cmd.Parameters.AddWithValue("$nombres", usuario.Nombres ?? string.Empty);
                        cmd.Parameters.AddWithValue("$apellidos", usuario.Apellidos ?? string.Empty);
                        cmd.Parameters.AddWithValue("$correo", usuario.Correo ?? string.Empty);
                        cmd.Parameters.AddWithValue("$telefono", usuario.Telefono ?? string.Empty);
                        cmd.Parameters.AddWithValue("$pass", HashPassword(password));
                        cmd.Parameters.AddWithValue("$rol", usuario.Rol ?? string.Empty);
                        cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch (SqliteException ex) when (IsDatabaseLocked(ex) && attempt < 2)
                {
                    WaitBeforeRetry(attempt);
                }
            }

            throw new InvalidOperationException(
                "No se pudo registrar el usuario porque la base de datos esta ocupada.");
        }

        private void ExecuteWithRetry(Action<SqliteConnection> action)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using (var conn = CreateOpenConnection())
                    {
                        action(conn);
                        return;
                    }
                }
                catch (SqliteException ex) when (IsDatabaseLocked(ex) && attempt < 2)
                {
                    WaitBeforeRetry(attempt);
                }
            }

            throw new InvalidOperationException(
                "No se pudo completar la operacion porque la base de datos esta ocupada.");
        }

        private Usuario MapUsuario(SqliteDataReader reader)
        {
            return new Usuario
            {
                Id = reader.GetInt32(0),
                Nombres = reader.GetString(1),
                Apellidos = reader.GetString(2),
                Correo = reader.GetString(3),
                Telefono = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                PasswordHash = reader.GetString(5),
                Rol = reader.GetString(6),
                FechaRegistro = DateTime.Parse(reader.GetString(7)),
                FotoPerfil = reader.IsDBNull(8) ? null : reader.GetString(8),
                AplicativosJson = reader.IsDBNull(9) ? "[]" : reader.GetString(9)
            };
        }
    }
}
