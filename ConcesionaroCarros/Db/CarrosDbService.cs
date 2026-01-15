using ConcesionaroCarros.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ConcesionaroCarros.Db
{
    public class CarrosDbService
    {
        private string _connection => DatabaseInitializer.ConnectionString;

        public List<Carro> ObtenerTodos()
        {
            var lista = new List<Carro>();

            var conn = new SqliteConnection(_connection);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Carros";

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Carro
                {
                    Id = reader.GetInt32(0),
                    Marca = reader.GetString(1),
                    Modelo = reader.GetString(2),
                    Año = reader.GetInt32(3),
                    Color = reader.GetString(4),
                    Costo = reader.GetDouble(5),
                    PrecioVenta = reader.GetDouble(6),
                    Estado = reader.IsDBNull(7) ? "Disponible" : reader.GetString(7),
                    Descripcion = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ImagenPath = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }

            return lista;
        }

        public void Insertar(Carro c)
        {
            var conn = new SqliteConnection(_connection);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT INTO Carros
                (Marca, Modelo, Año, Color, Costo, PrecioVenta, Estado, Descripcion, ImagenPath)
                VALUES (@Marca,@Modelo,@Año,@Color,@Costo,@Precio,@Estado,@Desc,@Img)";

            cmd.Parameters.AddWithValue("@Marca", c.Marca ?? "");
            cmd.Parameters.AddWithValue("@Modelo", c.Modelo ?? "");
            cmd.Parameters.AddWithValue("@Año", c.Año);
            cmd.Parameters.AddWithValue("@Color", c.Color ?? "");
            cmd.Parameters.AddWithValue("@Costo", c.Costo);
            cmd.Parameters.AddWithValue("@Precio", c.PrecioVenta);
            cmd.Parameters.AddWithValue("@Estado", c.Estado ?? "Disponible");
            cmd.Parameters.AddWithValue("@Desc", (object)c.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Img", (object)c.ImagenPath ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void Actualizar(Carro c)
        {
            var conn = new SqliteConnection(_connection);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"UPDATE Carros SET
                Marca=@Marca,
                Modelo=@Modelo,
                Año=@Año,
                Color=@Color,
                Costo=@Costo,
                PrecioVenta=@Precio,
                Estado=@Estado,
                Descripcion=@Desc,
                ImagenPath=@Img
              WHERE Id=@Id";

            cmd.Parameters.AddWithValue("@Id", c.Id);
            cmd.Parameters.AddWithValue("@Marca", c.Marca ?? "");
            cmd.Parameters.AddWithValue("@Modelo", c.Modelo ?? "");
            cmd.Parameters.AddWithValue("@Año", c.Año);
            cmd.Parameters.AddWithValue("@Color", c.Color ?? "");
            cmd.Parameters.AddWithValue("@Costo", c.Costo);
            cmd.Parameters.AddWithValue("@Precio", c.PrecioVenta);
            cmd.Parameters.AddWithValue("@Estado", c.Estado ?? "Disponible");
            cmd.Parameters.AddWithValue("@Desc", (object)c.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Img", (object)c.ImagenPath ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void Eliminar(int id)
        {
             var conn = new SqliteConnection(_connection);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Carros WHERE Id=@Id";
            cmd.Parameters.AddWithValue("@Id", id);

            cmd.ExecuteNonQuery();
        }
    }
}
