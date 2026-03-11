using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ConcesionaroCarros.Db
{
    public class AdministradoresDbService
    {
        private readonly string _connectionString = DatabaseInitializer.ConnectionString;

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

        private static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
                return Convert.ToBase64String(sha.ComputeHash(bytes));
            }
        }

        public void GuardarOActualizar(Administrador admin, string passwordAdmin)
        {
            using (var conn = CreateOpenConnection())
            using (var select = conn.CreateCommand())
            {
                select.CommandText = "SELECT Id FROM Administrador WHERE Correo = $c LIMIT 1;";
                select.Parameters.AddWithValue("$c", admin.Correo ?? string.Empty);
                var idObj = select.ExecuteScalar();

                if (idObj == null || idObj == DBNull.Value)
                {
                    using (var insert = conn.CreateCommand())
                    {
                        insert.CommandText =
                        @"
                        INSERT INTO Administrador
                        (Nombres, Apellidos, Correo, UsuarioSistema, Rol, PasswordAdminHash, FechaRegistro)
                        VALUES
                        ($n, $a, $c, $u, $r, $p, $f);
                        ";

                        insert.Parameters.AddWithValue("$n", admin.Nombres ?? string.Empty);
                        insert.Parameters.AddWithValue("$a", admin.Apellidos ?? string.Empty);
                        insert.Parameters.AddWithValue("$c", admin.Correo ?? string.Empty);
                        insert.Parameters.AddWithValue("$u", admin.UsuarioSistema ?? string.Empty);
                        insert.Parameters.AddWithValue("$r", admin.Rol ?? "ADMINISTRADOR");
                        insert.Parameters.AddWithValue("$p", HashPassword(passwordAdmin));
                        insert.Parameters.AddWithValue("$f", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        insert.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var update = conn.CreateCommand())
                    {
                        update.CommandText =
                        @"
                        UPDATE Administrador
                        SET Nombres = $n,
                            Apellidos = $a,
                            UsuarioSistema = $u,
                            Rol = $r,
                            PasswordAdminHash = $p
                        WHERE Correo = $c;
                        ";

                        update.Parameters.AddWithValue("$n", admin.Nombres ?? string.Empty);
                        update.Parameters.AddWithValue("$a", admin.Apellidos ?? string.Empty);
                        update.Parameters.AddWithValue("$u", admin.UsuarioSistema ?? string.Empty);
                        update.Parameters.AddWithValue("$r", admin.Rol ?? "ADMINISTRADOR");
                        update.Parameters.AddWithValue("$p", HashPassword(passwordAdmin));
                        update.Parameters.AddWithValue("$c", admin.Correo ?? string.Empty);
                        update.ExecuteNonQuery();
                    }
                }
            }
        }

        public Administrador LoginPorUsuarioSistema(string usuarioSistema, string passwordAdmin)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Id, Nombres, Apellidos, Correo, UsuarioSistema, Rol, PasswordAdminHash, FechaRegistro
                FROM Administrador
                WHERE UPPER(TRIM(UsuarioSistema)) = UPPER(TRIM($u))
                  AND PasswordAdminHash = $p
                ORDER BY Id DESC
                LIMIT 1;
                ";

                cmd.Parameters.AddWithValue("$u", usuarioSistema ?? string.Empty);
                cmd.Parameters.AddWithValue("$p", HashPassword(passwordAdmin));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new Administrador
                    {
                        Id = reader.GetInt32(0),
                        Nombres = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Apellidos = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Correo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        UsuarioSistema = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Rol = reader.IsDBNull(5) ? "ADMINISTRADOR" : reader.GetString(5),
                        PasswordAdminHash = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        FechaRegistro = reader.IsDBNull(7)
                            ? DateTime.Now
                            : DateTime.Parse(reader.GetString(7))
                    };
                }
            }
        }

        public bool ExistePorUsuarioSistema(string usuarioSistema)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT 1
                FROM Administrador
                WHERE UPPER(TRIM(UsuarioSistema)) = UPPER(TRIM($u))
                LIMIT 1;
                ";
                cmd.Parameters.AddWithValue("$u", usuarioSistema ?? string.Empty);

                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        public void EliminarPorCorreo(string correo)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Administrador WHERE UPPER(TRIM(Correo)) = UPPER(TRIM($c));";
                cmd.Parameters.AddWithValue("$c", correo ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        public void SincronizarDesdeUsuario(string correoAnterior, Usuario usuario)
        {
            if (usuario == null)
                return;

            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                @"
                SELECT Id, UsuarioSistema, PasswordAdminHash, FechaRegistro
                FROM Administrador
                WHERE UPPER(TRIM(Correo)) = UPPER(TRIM($correoAnterior))
                   OR UPPER(TRIM(Correo)) = UPPER(TRIM($correoNuevo))
                ORDER BY Id DESC
                LIMIT 1;
                ";

                cmd.Parameters.AddWithValue("$correoAnterior", correoAnterior ?? string.Empty);
                cmd.Parameters.AddWithValue("$correoNuevo", usuario.Correo ?? string.Empty);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return;

                    var id = reader.GetInt32(0);
                    var usuarioSistema = reader.IsDBNull(1)
                        ? ObtenerUsuarioDesdeCorreo(usuario.Correo)
                        : reader.GetString(1);

                    reader.Close();

                    using (var update = conn.CreateCommand())
                    {
                        update.CommandText =
                        @"
                        UPDATE Administrador
                        SET Nombres = $n,
                            Apellidos = $a,
                            Correo = $c,
                            UsuarioSistema = $u,
                            Rol = $r
                        WHERE Id = $id;
                        ";

                        update.Parameters.AddWithValue("$n", usuario.Nombres ?? string.Empty);
                        update.Parameters.AddWithValue("$a", usuario.Apellidos ?? string.Empty);
                        update.Parameters.AddWithValue("$c", usuario.Correo ?? string.Empty);
                        update.Parameters.AddWithValue("$u", usuarioSistema ?? string.Empty);
                        update.Parameters.AddWithValue("$r", usuario.Rol ?? "ADMINISTRADOR");
                        update.Parameters.AddWithValue("$id", id);
                        update.ExecuteNonQuery();
                    }
                }
            }
        }

        private static string ObtenerUsuarioDesdeCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return string.Empty;

            var trimmed = correo.Trim();
            var at = trimmed.IndexOf('@');
            return at <= 0
                ? trimmed
                : trimmed.Substring(0, at).Trim();
        }
    }
}
