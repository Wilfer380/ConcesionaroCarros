using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcesionaroCarros.Db
{
    public class ClientesDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        // =====================
        // INSERTAR
        // =====================
        public void Insertar(Cliente cliente)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText = @"
            INSERT INTO Clientes 
            (Nombres, Apellidos, Cedula, Correo, Telefono, Direccion,
             FechaNacimiento, CiudadDepartamento, CargoActual, CodigoPostal, FechaRegistro, FotoPerfil)
            VALUES 
            ($nombres,$apellidos,$cedula,$correo,$telefono,$direccion,
             $fechaNac,$ciudadDep,$cargo,$postal,$fecha,$foto);
            ";

            cmd.Parameters.AddWithValue("$nombres", cliente.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos);
            cmd.Parameters.AddWithValue("$cedula", cliente.Cedula);
            cmd.Parameters.AddWithValue("$correo", cliente.Correo);
            cmd.Parameters.AddWithValue("$telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("$direccion", cliente.Direccion);

            if (cliente.FechaNacimiento.HasValue)
                cmd.Parameters.AddWithValue("$fechaNac", cliente.FechaNacimiento.Value.ToString("yyyy-MM-dd"));
            else
                cmd.Parameters.AddWithValue("$fechaNac", DBNull.Value);

            cmd.Parameters.AddWithValue("$ciudadDep",
                string.IsNullOrWhiteSpace(cliente.CiudadDepartamento)
                ? (object)DBNull.Value
                : cliente.CiudadDepartamento);

            cmd.Parameters.AddWithValue("$cargo",
                string.IsNullOrWhiteSpace(cliente.CargoActual)
                ? (object)DBNull.Value
                : cliente.CargoActual);

            cmd.Parameters.AddWithValue("$postal",
                string.IsNullOrWhiteSpace(cliente.CodigoPostal)
                ? (object)DBNull.Value
                : cliente.CodigoPostal);

            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd"));

            cmd.Parameters.AddWithValue("$foto",
                string.IsNullOrEmpty(cliente.FotoPerfil)
                ? (object)DBNull.Value
                : cliente.FotoPerfil);

            cmd.ExecuteNonQuery();

            conn.Close();
        }

        // =====================
        // ACTUALIZAR
        // =====================
        public void Actualizar(Cliente cliente)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText = @"
            UPDATE Clientes SET
                Nombres=$nombres,
                Apellidos=$apellidos,
                Cedula=$cedula,
                Correo=$correo,
                Telefono=$telefono,
                Direccion=$direccion,
                FechaNacimiento=$fechaNac,
                CiudadDepartamento=$ciudadDep,
                CargoActual=$cargo,
                CodigoPostal=$postal,
                FotoPerfil=$foto
            WHERE Id=$id;
            ";

            cmd.Parameters.AddWithValue("$id", cliente.Id);
            cmd.Parameters.AddWithValue("$nombres", cliente.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos);
            cmd.Parameters.AddWithValue("$cedula", cliente.Cedula);
            cmd.Parameters.AddWithValue("$correo", cliente.Correo);
            cmd.Parameters.AddWithValue("$telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("$direccion", cliente.Direccion);

            if (cliente.FechaNacimiento.HasValue)
                cmd.Parameters.AddWithValue("$fechaNac", cliente.FechaNacimiento.Value.ToString("yyyy-MM-dd"));
            else
                cmd.Parameters.AddWithValue("$fechaNac", DBNull.Value);

            cmd.Parameters.AddWithValue("$ciudadDep",
                string.IsNullOrWhiteSpace(cliente.CiudadDepartamento)
                ? (object)DBNull.Value
                : cliente.CiudadDepartamento);

            cmd.Parameters.AddWithValue("$cargo",
                string.IsNullOrWhiteSpace(cliente.CargoActual)
                ? (object)DBNull.Value
                : cliente.CargoActual);

            cmd.Parameters.AddWithValue("$postal",
                string.IsNullOrWhiteSpace(cliente.CodigoPostal)
                ? (object)DBNull.Value
                : cliente.CodigoPostal);

            cmd.Parameters.AddWithValue("$foto",
                string.IsNullOrEmpty(cliente.FotoPerfil)
                ? (object)DBNull.Value
                : cliente.FotoPerfil);

            cmd.ExecuteNonQuery();

            conn.Close();
        }

        // =====================
        // OBTENER
        // =====================
        public List<Cliente> ObtenerTodos()
        {
            var lista = new List<Cliente>();

            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Clientes ORDER BY FechaRegistro DESC";

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Cliente
                {
                    Id = reader.GetInt32(0),
                    Nombres = reader.GetString(1),
                    Apellidos = reader.GetString(2),
                    Cedula = reader.GetString(3),
                    Correo = reader.GetString(4),
                    Telefono = reader.GetString(5),
                    Direccion = reader.GetString(6),
                    FechaNacimiento = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7)),
                    CiudadDepartamento = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    CargoActual = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    CodigoPostal = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    FechaRegistro = DateTime.Parse(reader.GetString(11)),
                    FotoPerfil = reader.IsDBNull(12) ? null : reader.GetString(12)
                });
            }

            conn.Close();

            return lista;
        }

        public void ActualizarFotoPerfil(int idCliente, string rutaFoto)
        {
             var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Clientes SET FotoPerfil = $foto WHERE Id = $id";

            cmd.Parameters.AddWithValue("$foto", rutaFoto);
            cmd.Parameters.AddWithValue("$id", idCliente);

            cmd.ExecuteNonQuery();
        }

        // =====================
        // ELIMINAR
        // =====================
        public void EliminarPorCorreo(string correo)
        {
            var clientes = ObtenerTodos();
            var cliente = clientes.FirstOrDefault(c => c.Correo == correo);
            if (cliente != null)
            {
                Eliminar(cliente.Id);
            }
        }

        public void Eliminar(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = new SqliteCommand("DELETE FROM Clientes WHERE Id = $Id", connection);
                command.Parameters.AddWithValue("$Id", id);
                command.ExecuteNonQuery();
            }
        }
        public void InsertarDesdeUsuario(Usuario u)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
    INSERT INTO Clientes
    (Nombres, Apellidos, Cedula, Correo, Telefono, Direccion, FechaRegistro, FotoPerfil)
    VALUES
    ($n,$a,'',$c,$t,'',$f,$foto);
    ";

            cmd.Parameters.AddWithValue("$n", u.Nombres);
            cmd.Parameters.AddWithValue("$a", u.Apellidos);
            cmd.Parameters.AddWithValue("$c", u.Correo);
            cmd.Parameters.AddWithValue("$t", u.Telefono);
            cmd.Parameters.AddWithValue("$f", DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$foto",
                string.IsNullOrEmpty(u.FotoPerfil)
                ? (object)DBNull.Value
                : u.FotoPerfil);

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public Cliente ObtenerPorCorreo(string correo)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Clientes WHERE Correo=$c";
            cmd.Parameters.AddWithValue("$c", correo);

            var r = cmd.ExecuteReader();

            if (!r.Read()) return null;

            return new Cliente
            {
                Id = r.GetInt32(0),
                Nombres = r.GetString(1),
                Apellidos = r.GetString(2),
                Cedula = r.GetString(3),
                Correo = r.GetString(4),
                Telefono = r.GetString(5),
                Direccion = r.IsDBNull(6) ? "" : r.GetString(6),
                FechaNacimiento = r.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(r.GetString(7)),
                CiudadDepartamento = r.IsDBNull(8) ? "" : r.GetString(8),
                CargoActual = r.IsDBNull(9) ? "" : r.GetString(9),
                CodigoPostal = r.IsDBNull(10) ? "" : r.GetString(10),
                FechaRegistro = r.IsDBNull(11)
                ? DateTime.Now
                : DateTime.Parse(r.GetString(11)),

                FotoPerfil = r.IsDBNull(12) ? null : r.GetString(12)
            };
        }

    }
}
