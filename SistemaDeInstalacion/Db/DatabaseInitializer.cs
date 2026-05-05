using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

namespace ConcesionaroCarros.Db
{
    public static class DatabaseInitializer
    {
        private static readonly string[] LegacyDbFileNames =
        {
            "WegInstallerSystems.db",
            "installer_systems.db",
            "carros.db"
        };

        public static string CurrentDbPath => DatabaseConnectionProvider.Instance.DatabasePath;

        public static string ConnectionString =>
            DatabaseConnectionProvider.Instance.ConnectionString;

        public static void Initialize()
        {
            EnsureDatabaseDirectoryExists();
            var legacyDbMigrated = MigrarArchivoLegacySiExiste();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                    @"
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

                    CREATE TABLE IF NOT EXISTS DeveloperAccount (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL UNIQUE,
                        Enabled INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT,
                        CreatedBy TEXT,
                        Notes TEXT
                    );
                    ";

                    cmd.ExecuteNonQuery();
                }

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
                EliminarTablasLegacy(connection);
                SeedDeveloperAccounts(connection);

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

            if (!string.IsNullOrWhiteSpace(legacyDbMigrated))
                EliminarArchivoLegacyMigrado(legacyDbMigrated);
        }

        private static string MigrarArchivoLegacySiExiste()
        {
            if (File.Exists(CurrentDbPath))
                return null;

            foreach (var legacyDbPath in ResolveLegacyDbPaths())
            {
                if (!File.Exists(legacyDbPath))
                    continue;

                File.Copy(legacyDbPath, CurrentDbPath, false);
                return legacyDbPath;
            }

            return null;
        }

        private static void EnsureDatabaseDirectoryExists()
        {
            var directory = Path.GetDirectoryName(CurrentDbPath);
            if (string.IsNullOrWhiteSpace(directory))
                return;

            Directory.CreateDirectory(directory);
        }

        private static string[] ResolveLegacyDbPaths()
        {
            var currentDirectory = Path.GetDirectoryName(CurrentDbPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            return LegacyDbFileNames
                .Select(fileName => Path.Combine(currentDirectory, fileName))
                .ToArray();
        }

        private static void EliminarArchivoLegacyMigrado(string legacyDbPath)
        {
            if (string.IsNullOrWhiteSpace(legacyDbPath))
                return;

            if (string.Equals(
                    legacyDbPath,
                    CurrentDbPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!File.Exists(CurrentDbPath) || !File.Exists(legacyDbPath))
                return;

            try
            {
                File.Delete(legacyDbPath);
            }
            catch
            {
                // Si algun proceso externo mantiene abierto el archivo legacy,
                // la aplicacion sigue operando con la base nueva y se podra
                // eliminar manualmente despues.
            }
        }

        private static void EliminarTablasLegacy(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                DROP TABLE IF EXISTS Carros;
                DROP TABLE IF EXISTS Clientes;
                DROP TABLE IF EXISTS Empleados;
                DROP TABLE IF EXISTS Administradores;
                ";
                cmd.ExecuteNonQuery();
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

        private static void SeedDeveloperAccounts(SqliteConnection connection)
        {
            if (connection == null)
                return;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
INSERT OR IGNORE INTO DeveloperAccount (Email, Enabled, CreatedAt, CreatedBy, Notes)
VALUES
  ('wandica@weg.net', 1, datetime('now'), 'system', 'seed'),
  ('maicolj@weg.net', 1, datetime('now'), 'system', 'seed');";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
