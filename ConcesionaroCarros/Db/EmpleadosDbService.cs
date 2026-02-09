using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace ConcesionaroCarros.Db
{
    public class EmpleadosDbService
    {
        private readonly string _connectionString =
            DatabaseInitializer.ConnectionString;

        // =========================
        // OBTENER
        // =========================
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
                    MetaVentas = reader.GetInt32(7),
                    Cedula = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Ciudad = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Departamento = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    FotoPerfil = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }

            return lista;
        }

        // =========================
        // INSERTAR
        // =========================
        public void Insertar(Empleado e)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
             @"
            INSERT INTO Empleados
            (Nombres, Apellidos, Correo, Telefono, Cargo, Activo, MetaVentas, Cedula, Ciudad, Departamento, FotoPerfil)
            VALUES
            ($n,$a,$c,$t,$cargo,$activo,$meta,$cedula,$ciudad,$dep,$foto);
            ";

            cmd.Parameters.AddWithValue("$n", e.Nombres ?? "");
            cmd.Parameters.AddWithValue("$a", e.Apellidos ?? "");
            cmd.Parameters.AddWithValue("$c", e.Correo ?? "");
            cmd.Parameters.AddWithValue("$t", e.Telefono ?? "");
            cmd.Parameters.AddWithValue("$cargo", e.Cargo ?? "");
            cmd.Parameters.AddWithValue("$activo", e.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$meta", e.MetaVentas);
            cmd.Parameters.AddWithValue("$cedula", e.Cedula ?? "");
            cmd.Parameters.AddWithValue("$ciudad", e.Ciudad ?? "");
            cmd.Parameters.AddWithValue("$dep", e.Departamento ?? "");
            cmd.Parameters.AddWithValue("$foto", e.FotoPerfil ?? "");

            cmd.ExecuteNonQuery();
        }

        // =========================
        // ACTUALIZAR
        // =========================
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
                MetaVentas=$meta,
                Cedula=$cedula,
                Ciudad=$ciudad,
                Departamento=$dep,
                FotoPerfil=$foto
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
            cmd.Parameters.AddWithValue("$cedula", e.Cedula ?? "");
            cmd.Parameters.AddWithValue("$ciudad", e.Ciudad ?? "");
            cmd.Parameters.AddWithValue("$dep", e.Departamento ?? "");
            cmd.Parameters.AddWithValue("$foto", e.FotoPerfil ?? "");

            cmd.ExecuteNonQuery();
        }

 
        public void Eliminar(int id)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Empleados WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        public void EliminarPorCorreo(string correo)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Empleados WHERE Correo = $correo";
            cmd.Parameters.AddWithValue("$correo", correo);

            cmd.ExecuteNonQuery();
        }
        public void InsertarDesdeUsuario(Usuario u)
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
    INSERT INTO Empleados
    (Nombres, Apellidos, Correo, Telefono, Cargo, Activo, MetaVentas)
    VALUES
    ($n,$a,$c,$t,'Empleado',1,0);
    ";

            cmd.Parameters.AddWithValue("$n", u.Nombres);
            cmd.Parameters.AddWithValue("$a", u.Apellidos);
            cmd.Parameters.AddWithValue("$c", u.Correo);
            cmd.Parameters.AddWithValue("$t", u.Telefono);

            cmd.ExecuteNonQuery();
            conn.Close();
        }


    }
}
