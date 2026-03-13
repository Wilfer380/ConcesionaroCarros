using ConcesionaroCarros.Db;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace SistemaDeInstalacion.Tests
{
    internal static class DatabaseInitializerTests
    {
        public static void Initialize_CreatesDatabaseAndTables()
        {
            using (var workspace = new TestWorkspace())
            {
                DatabaseInitializer.Initialize();

                AssertEx.True(File.Exists(workspace.CurrentDatabasePath),
                    "DatabaseInitializer debe crear WegInstaladores.db.");

                using (var conn = new SqliteConnection(DatabaseInitializer.ConnectionString))
                {
                    conn.Open();

                    AssertEx.True(TableExists(conn, "Usuarios"), "Debe existir la tabla Usuarios.");
                    AssertEx.True(TableExists(conn, "Instaladores"), "Debe existir la tabla Instaladores.");
                    AssertEx.True(TableExists(conn, "Administrador"), "Debe existir la tabla Administrador.");
                    AssertEx.True(TableExists(conn, "PasswordRecoveryLog"), "Debe existir la tabla PasswordRecoveryLog.");
                }
            }
        }

        public static void Initialize_MigratesLegacyDatabaseAndNormalizesData()
        {
            using (var workspace = new TestWorkspace())
            {
                CreateLegacyDatabase(workspace.CurrentDatabasePath);

                DatabaseInitializer.Initialize();

                AssertEx.True(File.Exists(workspace.CurrentDatabasePath),
                    "La base actual debe existir despues de inicializar una base con esquema legacy.");
                using (var conn = new SqliteConnection(DatabaseInitializer.ConnectionString))
                {
                    conn.Open();

                    AssertEx.False(TableExists(conn, "Administradores"),
                        "La tabla legacy Administradores debe eliminarse despues de migrar.");
                    AssertEx.True(TableExists(conn, "Administrador"),
                        "La tabla Administrador vigente debe existir.");

                    AssertEx.Equal("ADMINISTRADOR", Scalar(conn,
                        "SELECT Rol FROM Usuarios WHERE Correo = 'legacy@weg.net';"),
                        "El rol ADMIN debe normalizarse a ADMINISTRADOR.");

                    AssertEx.Equal("Desarrollo global", Scalar(conn,
                        "SELECT Carpeta FROM Instaladores WHERE Ruta = 'C:\\Legacy\\setup.exe';"),
                        "La carpeta vacia debe normalizarse a Desarrollo global.");

                    AssertEx.Equal("admin@weg.net", Scalar(conn,
                        "SELECT Correo FROM Administrador WHERE UsuarioSistema = 'admin.legacy';"),
                        "Los administradores legacy deben migrarse a la tabla Administrador.");
                }
            }
        }

        private static void CreateLegacyDatabase(string path)
        {
            using (var conn = new SqliteConnection($"Data Source={path}"))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
DROP TABLE IF EXISTS Usuarios;
DROP TABLE IF EXISTS Instaladores;
DROP TABLE IF EXISTS Administrador;
DROP TABLE IF EXISTS PasswordRecoveryLog;
DROP TABLE IF EXISTS Administradores;

CREATE TABLE Usuarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombres TEXT NOT NULL,
    Apellidos TEXT NOT NULL,
    Correo TEXT NOT NULL UNIQUE,
    Telefono TEXT,
    PasswordHash TEXT NOT NULL,
    Rol TEXT NOT NULL,
    FechaRegistro TEXT NOT NULL,
    FotoPerfil TEXT,
    AplicativosJson TEXT
);

CREATE TABLE Instaladores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ruta TEXT,
    Carpeta TEXT,
    FechaRegistro TEXT
);

CREATE TABLE Administradores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombres TEXT NOT NULL,
    Apellidos TEXT,
    Correo TEXT NOT NULL UNIQUE,
    UsuarioSistema TEXT NOT NULL,
    Rol TEXT NOT NULL,
    PasswordAdminHash TEXT NOT NULL,
    FechaRegistro TEXT NOT NULL
);

INSERT INTO Usuarios
(Nombres, Apellidos, Correo, Telefono, PasswordHash, Rol, FechaRegistro, FotoPerfil, AplicativosJson)
VALUES
('Legacy', 'User', 'legacy@weg.net', '', 'hash', 'ADMIN', '2026-01-01 00:00:00', null, '');

INSERT INTO Instaladores
(Ruta, Carpeta, FechaRegistro)
VALUES
('C:\Legacy\setup.exe', '', '2026-01-01 00:00:00');

INSERT INTO Administradores
(Nombres, Apellidos, Correo, UsuarioSistema, Rol, PasswordAdminHash, FechaRegistro)
VALUES
('Admin', 'Legacy', 'admin@weg.net', 'admin.legacy', 'ADMINISTRADOR', 'hashadmin', '2026-01-01 00:00:00');
";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
                cmd.Parameters.AddWithValue("$name", tableName);
                var value = cmd.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static string Scalar(SqliteConnection conn, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                var value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value);
            }
        }
    }
}
