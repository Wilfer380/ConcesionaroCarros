using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using ConcesionaroCarros.Models;
using System.IO;

namespace ConcesionaroCarros.Db
{
    public class InstaladorDbService
    {
        private readonly string _connection = DatabaseInitializer.ConnectionString;
        private const string CarpetaDesarrolloGlobal = "Desarrollo global";

        private static string NormalizarCarpeta(string carpeta)
        {
            return string.IsNullOrWhiteSpace(carpeta)
                ? CarpetaDesarrolloGlobal
                : carpeta.Trim();
        }

        public void Guardar(Instalador instalador)
        {
            using (var conn = new SqliteConnection(_connection))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    INSERT INTO Instaladores 
                    (Ruta, Nombre, Descripcion, Carpeta, FechaRegistro) 
                    VALUES ($ruta, $nombre, $descripcion, $carpeta, $fecha);";

                    cmd.Parameters.AddWithValue("$ruta", instalador.Ruta);
                    cmd.Parameters.AddWithValue("$nombre", instalador.Nombre);
                    cmd.Parameters.AddWithValue("$descripcion", instalador.Descripcion ?? "");
                    cmd.Parameters.AddWithValue("$carpeta", NormalizarCarpeta(instalador.Carpeta));
                    cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Actualizar(Instalador instalador)
        {
            using (var conn = new SqliteConnection(_connection))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    UPDATE Instaladores 
                    SET Nombre = $nombre,
                        Descripcion = $descripcion,
                        Carpeta = $carpeta
                    WHERE Ruta = $ruta";

                    cmd.Parameters.AddWithValue("$nombre", instalador.Nombre);
                    cmd.Parameters.AddWithValue("$descripcion", instalador.Descripcion ?? "");
                    cmd.Parameters.AddWithValue("$carpeta", NormalizarCarpeta(instalador.Carpeta));
                    cmd.Parameters.AddWithValue("$ruta", instalador.Ruta);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Instalador> ObtenerTodos()
        {
            var lista = new List<Instalador>();

            using (var conn = new SqliteConnection(_connection))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT Id, Ruta, Nombre, Descripcion, Carpeta FROM Instaladores ORDER BY Id DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ruta = reader.GetString(1);

                            lista.Add(new Instalador
                            {
                                Id = reader.GetInt32(0),
                                Ruta = ruta,
                                Nombre = reader.IsDBNull(2)
                                    ? Path.GetFileNameWithoutExtension(ruta)
                                    : reader.GetString(2),
                                Descripcion = reader.IsDBNull(3)
                                    ? ""
                                    : reader.GetString(3),
                                Carpeta = reader.IsDBNull(4)
                                    ? CarpetaDesarrolloGlobal
                                    : NormalizarCarpeta(reader.GetString(4))
                            });
                        }
                    }
                }
            }

            return lista;
        }

        public void EliminarRuta(string ruta)
        {
            using (var conn = new SqliteConnection(_connection))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Instaladores WHERE Ruta = $ruta";
                    cmd.Parameters.AddWithValue("$ruta", ruta);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
