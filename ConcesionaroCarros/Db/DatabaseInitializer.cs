using Microsoft.Data.Sqlite;
using System.IO;

namespace ConcesionaroCarros.Db
{
    public static class DatabaseInitializer
    {
        private static string _dbPath = "carros.db";

        public static string ConnectionString =>
            $"Data Source={_dbPath}";

        public static void Initialize()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Carros (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Marca TEXT,
                Modelo TEXT,
                Año INTEGER,
                Color TEXT,
                Costo REAL,
                PrecioVenta REAL,
                Estado TEXT,
                Descripcion TEXT,
                ImagenPath TEXT
            );

            CREATE TABLE IF NOT EXISTS Clientes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombres TEXT NOT NULL,
                Apellidos TEXT NOT NULL,
                Cedula TEXT NOT NULL UNIQUE,
                Correo TEXT,
                Telefono TEXT,
                Direccion TEXT,
                TipoCliente TEXT,
                FechaRegistro TEXT
            );
            ";
            cmd.ExecuteNonQuery();
        }
    }
}
