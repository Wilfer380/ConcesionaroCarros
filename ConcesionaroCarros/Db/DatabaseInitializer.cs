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
            if (!File.Exists(_dbPath))
            {
                 var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText =
                @"
                CREATE TABLE Carros (
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
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}

