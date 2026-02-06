using System.Collections.Generic;
using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ConcesionaroCarros.Db
{
    public class UsuariosDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        private string HashPassword(string password)
        {
             var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool Registrar(Usuario usuario, string password)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO Usuarios
            (Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro)
            VALUES
            ($nombres, $apellidos, $correo, $telefono, $pass, $rol, $fecha);
            ";

            cmd.Parameters.AddWithValue("$nombres", usuario.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", usuario.Apellidos);
            cmd.Parameters.AddWithValue("$correo", usuario.Correo);
            cmd.Parameters.AddWithValue("$telefono", usuario.Telefono);
            cmd.Parameters.AddWithValue("$pass", HashPassword(password));
            cmd.Parameters.AddWithValue("$rol", usuario.Rol);
            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Usuario Login(string correo, string password)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT * FROM Usuarios
            WHERE Correo = $correo AND PasswordHash = $pass;
            ";

            cmd.Parameters.AddWithValue("$correo", correo);
            cmd.Parameters.AddWithValue("$pass", HashPassword(password));

            var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new Usuario
            {
                Id = reader.GetInt32(0),
                Nombres = reader.GetString(1),
                Apellidos = reader.GetString(2),
                Correo = reader.GetString(3),
                Telefono = reader.GetString(4),
                PasswordHash = reader.GetString(5),
                Rol = reader.GetString(6),
                FechaRegistro = DateTime.Parse(reader.GetString(7))
            };
        }

        public IEnumerable<Usuario> ObtenerTodos()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT * FROM Usuarios;
            ";

            var reader = cmd.ExecuteReader();
            var usuarios = new List<Usuario>();
            while (reader.Read())
            {
                usuarios.Add(new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombres = reader.GetString(1),
                    Apellidos = reader.GetString(2),
                    Correo = reader.GetString(3),
                    Telefono = reader.GetString(4),
                    PasswordHash = reader.GetString(5),
                    Rol = reader.GetString(6),
                    FechaRegistro = DateTime.Parse(reader.GetString(7))
                });
            }

            return usuarios;
        }

        public void Eliminar(int id)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Usuarios WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);

                cmd.ExecuteNonQuery();
            }
        }
        public void Actualizar(Usuario u)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                var cmd = conn.CreateCommand();
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
                cmd.Parameters.AddWithValue("$n", u.Nombres);
                cmd.Parameters.AddWithValue("$a", u.Apellidos);
                cmd.Parameters.AddWithValue("$c", u.Correo);
                cmd.Parameters.AddWithValue("$t", u.Telefono);
                cmd.Parameters.AddWithValue("$r", u.Rol);

                cmd.ExecuteNonQuery();
            }
        }
        public void ActualizarPassword(int id, string password)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                var cmd = conn.CreateCommand();
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
        }
        public int RegistrarYRetornarId(Usuario usuario, string password)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO Usuarios
            (Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro)
            VALUES
            ($nombres, $apellidos, $correo, $telefono, $pass, $rol, $fecha);
            SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("$nombres", usuario.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", usuario.Apellidos);
            cmd.Parameters.AddWithValue("$correo", usuario.Correo);
            cmd.Parameters.AddWithValue("$telefono", usuario.Telefono);
            cmd.Parameters.AddWithValue("$pass", HashPassword(password));
            cmd.Parameters.AddWithValue("$rol", usuario.Rol);
            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            int id = Convert.ToInt32(cmd.ExecuteScalar());
            conn.Close();

            return id;
        }


    }
}
