using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace ConcesionaroCarros.Db
{
    public class EmpleadosDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        public List<Empleado> ObtenerTodos()
        {
            var lista = new List<Empleado>();

            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Empleados";

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Empleado
                {
                    Id = reader.GetInt32(0),
                    Nombres = reader.GetString(1),
                    Apellidos = reader.GetString(2),
                    Correo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Telefono = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Cargo = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Activo = reader.GetInt32(6) == 1,
                    MetaVentas = reader.GetInt32(7)
                });
            }

            return lista;
        }

        public void Insertar(Empleado e)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
             @"
            INSERT INTO Empleados
            (Nombres, Apellidos, Correo, Telefono, Cargo, Activo, MetaVentas)
            VALUES
            ($n,$a,$c,$t,$cargo,$activo,$meta);

            SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("$n", e.Nombres ?? "");
            cmd.Parameters.AddWithValue("$a", e.Apellidos ?? "");
            cmd.Parameters.AddWithValue("$c", e.Correo ?? "");
            cmd.Parameters.AddWithValue("$t", e.Telefono ?? "");
            cmd.Parameters.AddWithValue("$cargo", e.Cargo ?? "");
            cmd.Parameters.AddWithValue("$activo", e.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$meta", e.MetaVentas);

            e.Id = (int)(long)cmd.ExecuteScalar();
        }

        public void Actualizar(Empleado e)
        {
             var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            UPDATE Empleados SET
                Nombres=$n,
                Apellidos=$a,
                Correo=$c,
                Telefono=$t,
                Cargo=$cargo,
                Activo=$activo,
                MetaVentas=$meta
            WHERE Id=$id;
            ";

            cmd.Parameters.AddWithValue("$id", e.Id);
            cmd.Parameters.AddWithValue("$n", e.Nombres ?? "");
            cmd.Parameters.AddWithValue("$a", e.Apellidos ?? "");
            cmd.Parameters.AddWithValue("$c", e.Correo ?? "");
            cmd.Parameters.AddWithValue("$t", e.Telefono ?? "");
            cmd.Parameters.AddWithValue("$cargo", e.Cargo ?? "");
            cmd.Parameters.AddWithValue("$activo", e.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$meta", e.MetaVentas);

            cmd.ExecuteNonQuery();
        }
    }
}
