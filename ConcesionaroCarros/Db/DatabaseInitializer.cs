using Microsoft.Data.Sqlite;
using System;

namespace ConcesionaroCarros.Db
{
    public static class DatabaseInitializer
    {
        private static string _dbPath = "carros.db";

        public static string ConnectionString =>
            $"Data Source={_dbPath}";

        public static void Initialize()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
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

                    CREATE TABLE IF NOT EXISTS Empleados (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombres TEXT NOT NULL,
                        Apellidos TEXT NOT NULL,
                        Correo TEXT,
                        Telefono TEXT,
                        Cargo TEXT,
                        Activo INTEGER,
                        MetaVentas INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS Usuarios (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombres TEXT NOT NULL,
                        Apellidos TEXT NOT NULL,
                        Correo TEXT NOT NULL UNIQUE,
                        Telefono TEXT,
                        PasswordHash TEXT NOT NULL,
                        Rol TEXT NOT NULL,
                        FechaRegistro TEXT NOT NULL,
                        FotoPerfil TEXT,
                        AplicativosJson TEXT DEFAULT '[]'
                    );

                    CREATE TABLE IF NOT EXISTS Instaladores (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Ruta TEXT,
                        Carpeta TEXT,
                        FechaRegistro TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Administrador (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombres TEXT NOT NULL,
                        Apellidos TEXT,
                        Correo TEXT NOT NULL UNIQUE,
                        UsuarioSistema TEXT NOT NULL,
                        Rol TEXT NOT NULL,
                        PasswordAdminHash TEXT NOT NULL,
                        FechaRegistro TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS PasswordRecoveryLog (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UsuarioId INTEGER NOT NULL,
                        CorreoUsuario TEXT NOT NULL,
                        CorreoAdministrador TEXT,
                        ValidadoMicrosoft INTEGER NOT NULL DEFAULT 0,
                        FechaRecuperacion TEXT NOT NULL
                    );

                    ";

                    cmd.ExecuteNonQuery();
                }

                // Migraciones para bases existentes con columnas faltantes.
                EnsureColumnExists(connection, "Carros", "Placa", "TEXT");
                EnsureColumnExists(connection, "Carros", "EstadoAntiguedad", "TEXT");
                EnsureColumnExists(connection, "Carros", "UnidadesDisponibles", "INTEGER DEFAULT 0");
                EnsureColumnExists(connection, "Carros", "EstadoGarantia", "TEXT");

                EnsureColumnExists(connection, "Clientes", "FechaNacimiento", "TEXT");
                EnsureColumnExists(connection, "Clientes", "CiudadDepartamento", "TEXT");
                EnsureColumnExists(connection, "Clientes", "CargoActual", "TEXT");
                EnsureColumnExists(connection, "Clientes", "CodigoPostal", "TEXT");
                EnsureColumnExists(connection, "Clientes", "FotoPerfil", "TEXT");

                EnsureColumnExists(connection, "Empleados", "Cedula", "TEXT");
                EnsureColumnExists(connection, "Empleados", "Ciudad", "TEXT");
                EnsureColumnExists(connection, "Empleados", "Departamento", "TEXT");
                EnsureColumnExists(connection, "Empleados", "FotoPerfil", "TEXT");

                EnsureColumnExists(connection, "Usuarios", "FotoPerfil", "TEXT");
                EnsureColumnExists(connection, "Usuarios", "AplicativosJson", "TEXT DEFAULT '[]'");

                EnsureColumnExists(connection, "Instaladores", "Nombre", "TEXT");
                EnsureColumnExists(connection, "Instaladores", "Descripcion", "TEXT");
                EnsureColumnExists(connection, "Instaladores", "Carpeta", "TEXT");

                EnsureColumnExists(connection, "Administrador", "Nombres", "TEXT");
                EnsureColumnExists(connection, "Administrador", "Apellidos", "TEXT");
                EnsureColumnExists(connection, "Administrador", "Correo", "TEXT");
                EnsureColumnExists(connection, "Administrador", "UsuarioSistema", "TEXT");
                EnsureColumnExists(connection, "Administrador", "Rol", "TEXT");
                EnsureColumnExists(connection, "Administrador", "PasswordAdminHash", "TEXT");
                EnsureColumnExists(connection, "Administrador", "FechaRegistro", "TEXT");

                EnsureColumnExists(connection, "PasswordRecoveryLog", "UsuarioId", "INTEGER");
                EnsureColumnExists(connection, "PasswordRecoveryLog", "CorreoUsuario", "TEXT");
                EnsureColumnExists(connection, "PasswordRecoveryLog", "CorreoAdministrador", "TEXT");
                EnsureColumnExists(connection, "PasswordRecoveryLog", "ValidadoMicrosoft", "INTEGER DEFAULT 0");
                EnsureColumnExists(connection, "PasswordRecoveryLog", "FechaRecuperacion", "TEXT");

                MigrarTablaAdministradoresLegacy(connection);

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                    UPDATE Usuarios
                    SET AplicativosJson = '[]'
                    WHERE AplicativosJson IS NULL OR TRIM(AplicativosJson) = '';

                    UPDATE Usuarios
                    SET Rol = 'VENTAS'
                    WHERE UPPER(TRIM(Rol)) = 'CLIENTE';

                    UPDATE Usuarios
                    SET Rol = 'INGENIERIA'
                    WHERE UPPER(TRIM(Rol)) = 'INGENIERO';

                    UPDATE Usuarios
                    SET Rol = 'ADMINISTRADOR'
                    WHERE UPPER(TRIM(Rol)) = 'ADMIN';

                    UPDATE Instaladores
                    SET Carpeta = 'Desarrollo global'
                    WHERE Carpeta IS NULL OR TRIM(Carpeta) = '';
                    ";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureColumnExists(
            SqliteConnection connection,
            string tableName,
            string columnName,
            string columnDefinition)
        {
            if (ColumnExists(connection, tableName, columnName))
                return;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition};";
                cmd.ExecuteNonQuery();
            }
        }

        private static bool ColumnExists(
            SqliteConnection connection,
            string tableName,
            string columnName)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var existingColumn = reader.GetString(1);
                        if (string.Equals(existingColumn, columnName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        private static void MigrarTablaAdministradoresLegacy(SqliteConnection connection)
        {
            if (!TableExists(connection, "Administradores"))
                return;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                @"
                INSERT OR IGNORE INTO Administrador
                (Nombres, Apellidos, Correo, UsuarioSistema, Rol, PasswordAdminHash, FechaRegistro)
                SELECT Nombres, Apellidos, Correo, UsuarioSistema, Rol, PasswordAdminHash, FechaRegistro
                FROM Administradores;
                ";
                cmd.ExecuteNonQuery();
            }
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
                cmd.Parameters.AddWithValue("$name", tableName);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }
    }
}
