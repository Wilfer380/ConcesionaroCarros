using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace LauncherSistema
{
    internal static class LauncherService
    {
        private const string SharedRoot =
            "\\\\comde019\\DFSMDE\\PUBLIC\\CO_MDE_DISENO_DI\\RESPALDO DISE\u00d1OS\\SAP - Respaldo dise\u00f1os\\FORMATOS SAP\\InstallerSystem";

        private static readonly string VersionPath = Path.Combine(SharedRoot, "version.txt");
        private static readonly string SetupPath = Path.Combine(SharedRoot, "SetupSistema.exe");
        private static readonly string InstalledBuildVersionPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "build.version");
        private static readonly string PendingUpdatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "update.pending");
        private static readonly string InstalledAppPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "SistemaDeInstalacion.exe");

        public static void Ejecutar(string[] args)
        {
            if (EsInicioPostActualizacion(args))
            {
                AbrirAplicacionLocalConReintento();
                return;
            }

            var serverVersion = ObtenerVersionServidor();
            var localVersion = ObtenerVersionLocal();

            if (ShouldSkipVersionPrompt(serverVersion, localVersion))
            {
                AbrirAplicacionLocalConReintento();
                return;
            }

            if (!File.Exists(InstalledAppPath))
            {
                SolicitarInstalacion();
                return;
            }

            if (!string.IsNullOrWhiteSpace(serverVersion) &&
                !string.IsNullOrWhiteSpace(localVersion) &&
                !string.Equals(serverVersion, localVersion, StringComparison.OrdinalIgnoreCase))
            {
                var result = MessageBox.Show(
                    $"Se encontro una nueva version disponible ({serverVersion}).{Environment.NewLine}{Environment.NewLine}Deseas actualizar ahora?",
                    "Actualizacion disponible",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    SavePendingUpdate(serverVersion);
                    EjecutarActualizacion();
                    return;
                }
            }

            AbrirAplicacionLocalConReintento();
        }

        private static string ObtenerVersionServidor()
        {
            try
            {
                if (!File.Exists(VersionPath))
                    return null;

                return File.ReadAllText(VersionPath).Trim();
            }
            catch
            {
                return null;
            }
        }

        private static string ObtenerVersionLocal()
        {
            try
            {
                if (File.Exists(InstalledBuildVersionPath))
                    return File.ReadAllText(InstalledBuildVersionPath).Trim();

                if (!File.Exists(InstalledAppPath))
                    return null;

                var info = FileVersionInfo.GetVersionInfo(InstalledAppPath);
                return string.IsNullOrWhiteSpace(info.ProductVersion)
                    ? info.FileVersion
                    : info.ProductVersion;
            }
            catch
            {
                return null;
            }
        }

        private static void SolicitarInstalacion()
        {
            var result = MessageBox.Show(
                "La aplicacion no esta instalada en este equipo. Se abrira el instalador.",
                "Instalacion requerida",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result == DialogResult.OK)
                EjecutarSetupNormal();
        }

        private static void EjecutarSetupNormal()
        {
            if (!File.Exists(SetupPath))
            {
                MessageBox.Show(
                    "No se encontro SetupSistema.exe en la carpeta InstallerSystem.",
                    "Instalador no disponible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = SetupPath,
                UseShellExecute = true
            });
        }

        private static void EjecutarActualizacion()
        {
            if (!File.Exists(SetupPath))
            {
                MessageBox.Show(
                    "No se encontro SetupSistema.exe en la carpeta InstallerSystem.",
                    "Instalador no disponible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = SetupPath,
                    Arguments = "/SP- /SILENT /SUPPRESSMSGBOXES /NOCANCEL /NORESTART",
                    UseShellExecute = true
                });

                if (process == null)
                {
                    MessageBox.Show(
                        "No fue posible iniciar el actualizador.",
                        "Error de actualizacion",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo iniciar la actualizacion." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "Error de actualizacion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Environment.Exit(0);
        }

        private static bool EsInicioPostActualizacion(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            foreach (var arg in args)
            {
                if (string.Equals(arg, "--post-update", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--post-install", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldSkipVersionPrompt(string serverVersion, string localVersion)
        {
            try
            {
                if (!File.Exists(PendingUpdatePath))
                    return false;

                var pendingVersion = File.ReadAllText(PendingUpdatePath).Trim();
                if (string.IsNullOrWhiteSpace(pendingVersion))
                    return false;

                if (string.Equals(localVersion, pendingVersion, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(localVersion, serverVersion, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeletePendingUpdate();
                    return true;
                }
            }
            catch
            {
                // Si falla esta validacion, se sigue con el flujo normal.
            }

            return false;
        }

        private static void SavePendingUpdate(string expectedVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expectedVersion))
                    return;

                File.WriteAllText(PendingUpdatePath, expectedVersion);
            }
            catch
            {
                // No bloquea el flujo de actualizacion.
            }
        }

        private static void TryDeletePendingUpdate()
        {
            try
            {
                if (File.Exists(PendingUpdatePath))
                    File.Delete(PendingUpdatePath);
            }
            catch
            {
                // No bloquea la apertura.
            }
        }

        private static void AbrirAplicacionLocal()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = InstalledAppPath,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir SistemaDeInstalacion.exe." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "Error al abrir la aplicacion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void AbrirAplicacionLocalConReintento()
        {
            try
            {
                if (File.Exists(InstalledAppPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = InstalledAppPath,
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        UseShellExecute = true
                    });
                    return;
                }
            }
            catch
            {
                // Si no abre al primer intento, se delega al metodo con mensaje.
            }

            AbrirAplicacionLocal();
        }
    }
}
