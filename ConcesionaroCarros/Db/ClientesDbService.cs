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
            (Nombres, Apellidos, Cedula, Correo, Telefono, Direccion,
             FechaNacimiento, CiudadDepartamento, CargoActual, CodigoPostal, FechaRegistro)
            VALUES 
            ($nombres, $apellidos, $cedula, $correo, $telefono, $direccion,
             $fechaNac, $ciudadDep, $cargo, $postal, $fecha);
            ";

            cmd.Parameters.AddWithValue("$nombres", cliente.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos);
            cmd.Parameters.AddWithValue("$cedula", cliente.Cedula);
            cmd.Parameters.AddWithValue("$correo", cliente.Correo);
            cmd.Parameters.AddWithValue("$telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("$direccion", cliente.Direccion);
            cmd.Parameters.AddWithValue("$fechaNac",
                cliente.FechaNacimiento.HasValue
                    ? cliente.FechaNacimiento.Value.ToString("yyyy-MM-dd")
                    : null);
            cmd.Parameters.AddWithValue("$ciudadDep", cliente.CiudadDepartamento);
            cmd.Parameters.AddWithValue("$cargo", cliente.CargoActual);
            cmd.Parameters.AddWithValue("$postal", cliente.CodigoPostal);
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
                FechaNacimiento = $fechaNac,
                CiudadDepartamento = $ciudadDep,
                CargoActual = $cargo,
                CodigoPostal = $postal
            WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$id", cliente.Id);
            cmd.Parameters.AddWithValue("$nombres", cliente.Nombres);
            cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos);
            cmd.Parameters.AddWithValue("$cedula", cliente.Cedula);
            cmd.Parameters.AddWithValue("$correo", cliente.Correo);
            cmd.Parameters.AddWithValue("$telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("$direccion", cliente.Direccion);
            cmd.Parameters.AddWithValue("$fechaNac",
                cliente.FechaNacimiento.HasValue
                    ? cliente.FechaNacimiento.Value.ToString("yyyy-MM-dd")
                    : null);
            cmd.Parameters.AddWithValue("$ciudadDep", cliente.CiudadDepartamento);
            cmd.Parameters.AddWithValue("$cargo", cliente.CargoActual);
            cmd.Parameters.AddWithValue("$postal", cliente.CodigoPostal);

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
                DateTime? fechaNacimiento = null;
                if (!reader.IsDBNull(7))
                {
                    fechaNacimiento = DateTime.Parse(reader.GetString(7));
                }

                DateTime fechaRegistro;
                if (!reader.IsDBNull(11))
                {
                    fechaRegistro = DateTime.Parse(reader.GetString(11));
                }
                else
                {
                    fechaRegistro = DateTime.Now;
                }

                lista.Add(new Cliente
                {
                    Id = reader.GetInt32(0),
                    Nombres = reader.GetString(1),
                    Apellidos = reader.GetString(2),
                    Cedula = reader.GetString(3),
                    Correo = reader.GetString(4),
                    Telefono = reader.GetString(5),
                    Direccion = reader.GetString(6),
                    FechaNacimiento = fechaNacimiento,
                    CiudadDepartamento = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    CargoActual = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    CodigoPostal = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    FechaRegistro = fechaRegistro
                });
            }

            return lista;
        }
    }
}
