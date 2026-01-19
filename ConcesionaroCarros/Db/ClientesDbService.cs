using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ConcesionaroCarros.Db
{
    public class ClientesDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        public void Insertar(Cliente cliente)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO Clientes 
            (Nombres, Apellidos, Cedula, Correo, Telefono, Direccion, TipoCliente, FechaRegistro)
            VALUES 
            ($nombres, $apellidos, $cedula, $correo, $telefono, $direccion, $tipo, $fecha);
            ";

            cmd.Parameters.AddWithValue("$nombres", cliente.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos);
            cmd.Parameters.AddWithValue("$cedula", cliente.Cedula);
            cmd.Parameters.AddWithValue("$correo", cliente.Correo);
            cmd.Parameters.AddWithValue("$telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("$direccion", cliente.Direccion);
            cmd.Parameters.AddWithValue("$tipo", cliente.TipoCliente);
            cmd.Parameters.AddWithValue("$fecha", cliente.FechaRegistro.ToString("yyyy-MM-dd"));

            cmd.ExecuteNonQuery();
        }

        public void Actualizar(Cliente cliente)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            UPDATE Clientes SET
                Nombres = $nombres,
                Apellidos = $apellidos,
                Cedula = $cedula,
                Correo = $correo,
                Telefono = $telefono,
                Direccion = $direccion,
                TipoCliente = $tipo
            WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$id", cliente.Id);
            cmd.Parameters.AddWithValue("$nombres", cliente.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos);
            cmd.Parameters.AddWithValue("$cedula", cliente.Cedula);
            cmd.Parameters.AddWithValue("$correo", cliente.Correo);
            cmd.Parameters.AddWithValue("$telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("$direccion", cliente.Direccion);
            cmd.Parameters.AddWithValue("$tipo", cliente.TipoCliente);

            cmd.ExecuteNonQuery();
        }

        public void Eliminar(int id)
        {
             var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Clientes WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

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
                    TipoCliente = reader.GetString(7),
                    FechaRegistro = DateTime.Parse(reader.GetString(8))
                });
            }

            return lista;
        }
    }
}
