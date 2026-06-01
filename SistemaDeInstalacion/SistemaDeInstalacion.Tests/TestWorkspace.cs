using ConcesionaroCarros.Db;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace SistemaDeInstalacion.Tests
{
    internal sealed class TestWorkspace : IDisposable
    {
        private readonly string _originalCurrentDirectory;
        private readonly string _originalBranchName;
        private readonly string _originalTestDatabasePath;
        private static readonly string[] ManagedDatabaseFiles =
        {
            "WegInstaladores.db",
            "WegInstallerSystems.db",
            "installer_systems.db",
            "carros.db"
        };

        public TestWorkspace()
        {
            _originalCurrentDirectory = Environment.CurrentDirectory;
            _originalBranchName = Environment.GetEnvironmentVariable(DatabaseConnectionProvider.BranchNameKey);
            _originalTestDatabasePath = Environment.GetEnvironmentVariable(DatabaseConnectionProvider.TestDatabasePathKey);
            RootPath = AppDomain.CurrentDomain.BaseDirectory;

            Environment.SetEnvironmentVariable(DatabaseConnectionProvider.BranchNameKey, "feature/test-workspace");
            Environment.SetEnvironmentVariable(DatabaseConnectionProvider.TestDatabasePathKey, CurrentDatabasePath);

            CleanupKnownFiles();
            Environment.CurrentDirectory = RootPath;

            DatabaseInitializer.Initialize();
            ResetDatabaseContents();
        }

        public string RootPath { get; }

        public string CurrentDatabasePath => Path.Combine(RootPath, "WegInstaladores.db");

        public string LegacyDatabasePath(string fileName)
        {
            return Path.Combine(RootPath, fileName);
        }

        public void Dispose()
        {
            ResetDatabaseContents();
            Environment.SetEnvironmentVariable(DatabaseConnectionProvider.BranchNameKey, _originalBranchName);
            Environment.SetEnvironmentVariable(DatabaseConnectionProvider.TestDatabasePathKey, _originalTestDatabasePath);
            Environment.CurrentDirectory = _originalCurrentDirectory;
            CleanupKnownFiles();
        }

        private void CleanupKnownFiles()
        {
            foreach (var fileName in ManagedDatabaseFiles)
            {
                try
                {
                    var fullPath = Path.Combine(RootPath, fileName);
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
                catch
                {
                    // Si algun proceso deja un archivo abierto, no bloqueamos el resultado del test.
                }
            }
        }

        private void ResetDatabaseContents()
        {
            try
            {
                using (var conn = new SqliteConnection(DatabaseInitializer.ConnectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        DELETE FROM PasswordRecoveryLog;
                        DELETE FROM Usuarios;
                        DELETE FROM Instaladores;
                        DELETE FROM Administrador;
                        DELETE FROM sqlite_sequence WHERE name IN ('PasswordRecoveryLog', 'Usuarios', 'Instaladores', 'Administrador');
                        ";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // If the DB doesn't exist yet or is locked, the next init will repair it.
            }
        }
    }
}
