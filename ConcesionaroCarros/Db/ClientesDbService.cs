using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ConcesionaroCarros.Db
{
    public class ClientesDbService
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

        // =====================
        // INSERTAR
        // =====================
        public void Insertar(Cliente cliente)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    INSERT INTO Clientes 
                    (Nombres, Apellidos, Cedula, Correo, Telefono, Direccion,
                     FechaNacimiento, CiudadDepartamento, CargoActual, CodigoPostal, FechaRegistro, FotoPerfil)
                    VALUES 
                    ($nombres,$apellidos,$cedula,$correo,$telefono,$direccion,
                     $fechaNac,$ciudadDep,$cargo,$postal,$fecha,$foto);
                    ";

                    cmd.Parameters.AddWithValue("$nombres", cliente.Nombres ?? string.Empty);
                    cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos ?? string.Empty);
                    cmd.Parameters.AddWithValue("$cedula", cliente.Cedula ?? string.Empty);
                    cmd.Parameters.AddWithValue("$correo", cliente.Correo ?? string.Empty);
                    cmd.Parameters.AddWithValue("$telefono", cliente.Telefono ?? string.Empty);
                    cmd.Parameters.AddWithValue("$direccion", cliente.Direccion ?? string.Empty);

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
                }
            });
        }

        // =====================
        // ACTUALIZAR
        // =====================
        public void Actualizar(Cliente cliente)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
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
                    cmd.Parameters.AddWithValue("$nombres", cliente.Nombres ?? string.Empty);
                    cmd.Parameters.AddWithValue("$apellidos", cliente.Apellidos ?? string.Empty);
                    cmd.Parameters.AddWithValue("$cedula", cliente.Cedula ?? string.Empty);
                    cmd.Parameters.AddWithValue("$correo", cliente.Correo ?? string.Empty);
                    cmd.Parameters.AddWithValue("$telefono", cliente.Telefono ?? string.Empty);
                    cmd.Parameters.AddWithValue("$direccion", cliente.Direccion ?? string.Empty);

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
                }
            });
        }

        public List<Cliente> ObtenerTodos()
        {
            var lista = new List<Cliente>();

            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Clientes ORDER BY FechaRegistro DESC";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Cliente
                        {
                            Id = reader.GetInt32(0),
                            Nombres = reader.GetString(1),
                            Apellidos = reader.GetString(2),
                            Cedula = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Correo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Telefono = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Direccion = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            FechaNacimiento = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7)),
                            CiudadDepartamento = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            CargoActual = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            CodigoPostal = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            FechaRegistro = reader.IsDBNull(11) ? DateTime.Now : DateTime.Parse(reader.GetString(11)),
                            FotoPerfil = reader.IsDBNull(12) ? null : reader.GetString(12)
                        });
                    }
                }
            }

            return lista;
        }

        public void ActualizarFotoPerfil(int idCliente, string rutaFoto)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Clientes SET FotoPerfil = $foto WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$foto",
                        string.IsNullOrWhiteSpace(rutaFoto) ? (object)DBNull.Value : rutaFoto);
                    cmd.Parameters.AddWithValue("$id", idCliente);
                    cmd.ExecuteNonQuery();
                }
            });
        }

        // =====================
        // ELIMINAR
        // =====================
        public void EliminarPorCorreo(string correo)
        {
            var cliente = ObtenerPorCorreo(correo);
            if (cliente != null)
                Eliminar(cliente.Id);
        }

        public void Eliminar(int id)
        {
            ExecuteWithRetry(conn =>
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Clientes WHERE Id = $Id";
                    command.Parameters.AddWithValue("$Id", id);
                    command.ExecuteNonQuery();
                }
            });
        }

        public void InsertarDesdeUsuario(Usuario u)
        {
            ExecuteWithRetry(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"
                    INSERT INTO Clientes
                    (Nombres, Apellidos, Cedula, Correo, Telefono, Direccion, FechaRegistro, FotoPerfil)
                    VALUES
                    ($n,$a,'',$c,$t,'',$f,$foto);
                    ";

                    cmd.Parameters.AddWithValue("$n", u.Nombres ?? string.Empty);
                    cmd.Parameters.AddWithValue("$a", u.Apellidos ?? string.Empty);
                    cmd.Parameters.AddWithValue("$c", u.Correo ?? string.Empty);
                    cmd.Parameters.AddWithValue("$t", u.Telefono ?? string.Empty);
                    cmd.Parameters.AddWithValue("$f", DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("$foto",
                        string.IsNullOrEmpty(u.FotoPerfil)
                        ? (object)DBNull.Value
                        : u.FotoPerfil);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public Cliente ObtenerPorCorreo(string correo)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Clientes WHERE Correo=$c";
                cmd.Parameters.AddWithValue("$c", correo ?? string.Empty);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;

                    return new Cliente
                    {
                        Id = r.GetInt32(0),
                        Nombres = r.GetString(1),
                        Apellidos = r.GetString(2),
                        Cedula = r.IsDBNull(3) ? string.Empty : r.GetString(3),
                        Correo = r.IsDBNull(4) ? string.Empty : r.GetString(4),
                        Telefono = r.IsDBNull(5) ? string.Empty : r.GetString(5),
                        Direccion = r.IsDBNull(6) ? string.Empty : r.GetString(6),
                        FechaNacimiento = r.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(r.GetString(7)),
                        CiudadDepartamento = r.IsDBNull(8) ? string.Empty : r.GetString(8),
                        CargoActual = r.IsDBNull(9) ? string.Empty : r.GetString(9),
                        CodigoPostal = r.IsDBNull(10) ? string.Empty : r.GetString(10),
                        FechaRegistro = r.IsDBNull(11) ? DateTime.Now : DateTime.Parse(r.GetString(11)),
                        FotoPerfil = r.IsDBNull(12) ? null : r.GetString(12)
                    };
                }
            }
        }
    }
}
