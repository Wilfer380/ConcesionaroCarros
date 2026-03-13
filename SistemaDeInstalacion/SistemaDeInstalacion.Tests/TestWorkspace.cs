using System;
using System.IO;

namespace SistemaDeInstalacion.Tests
{
    internal sealed class TestWorkspace : IDisposable
    {
        private readonly string _originalCurrentDirectory;
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
            RootPath = AppDomain.CurrentDomain.BaseDirectory;

            CleanupKnownFiles();
            Environment.CurrentDirectory = RootPath;
        }

        public string RootPath { get; }

        public string CurrentDatabasePath => Path.Combine(RootPath, "WegInstaladores.db");

        public string LegacyDatabasePath(string fileName)
        {
            return Path.Combine(RootPath, fileName);
        }

        public void Dispose()
        {
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
    }
}
