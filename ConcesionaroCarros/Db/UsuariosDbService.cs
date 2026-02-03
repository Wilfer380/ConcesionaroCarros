using System.Collections.Generic;
using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;

namespace ConcesionaroCarros.Db
{
    public class UsuariosDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        // 🔐 HASH PASSWORD
        private string HashPassword(string password)
        {
             var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // ✅ REGISTRAR
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

        // 🔑 LOGIN
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

        // Agregado para corregir CS1061
        public void Eliminar(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("DELETE FROM Usuarios WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}
