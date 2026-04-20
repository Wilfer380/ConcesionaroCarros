using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LauncherSistema
{
    internal static class LauncherService
    {
        private const int RestoreWindowCommand = 9;
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

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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
                if (ConfirmarActualizacion(serverVersion, localVersion))
                {
                    SavePendingUpdate(serverVersion);
                    EjecutarActualizacion();
                }
                else
                {
                    TryDeletePendingUpdate();
                    AbrirAplicacionLocalConReintento();
                }

                return;
            }

            AbrirAplicacionLocalConReintento();
        }

        private static string ObtenerVersionServidor()
        {
            try
            {
                if (!File.Exists(VersionPath))
                    return null;

                var lines = ReadVersionMetadataLines();
                if (lines.Length == 0)
                    return null;

                return lines[0];
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

        private static string[] ReadVersionMetadataLines()
        {
            var content = File.ReadAllText(VersionPath);

            return content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .SkipWhile(string.IsNullOrWhiteSpace)
                .ToArray();
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
                    Arguments = "/SP- /NOCANCEL /NORESTART /UPDATEFLOW=1",
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

                private static bool ConfirmarActualizacion(string serverVersion, string localVersion)
        {
            var mensaje =
                "Hay una nueva versión disponible de SistemaDeInstalacion." +
                Environment.NewLine + Environment.NewLine +
                "Versión instalada: " + (string.IsNullOrWhiteSpace(localVersion) ? "No disponible" : localVersion) +
                Environment.NewLine +
                "Versión disponible: " + serverVersion +
                Environment.NewLine + Environment.NewLine +
                "\u00BFDesea actualizar ahora?";

            var result = MessageBox.Show(
                mensaje,
                "Nueva versión disponible",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1);

            return result == DialogResult.Yes;
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
                    var instanciaExistente = BuscarInstanciaExistente();
                    if (instanciaExistente != null)
                    {
                        var result = MessageBox.Show(
                            "La aplicacion ya esta abierta.\r\n\r\nÂ¿Desea ejecutar una nueva instancia?",
                            "Aplicacion en ejecucion",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);

                        if (result != DialogResult.Yes)
                        {
                            IntentarEnfocarInstancia(instanciaExistente);
                            return;
                        }
                    }

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

        private static Process BuscarInstanciaExistente()
        {
            var processName = Path.GetFileNameWithoutExtension(InstalledAppPath);
            var installedFullPath = Path.GetFullPath(InstalledAppPath);

            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (process.HasExited)
                        continue;

                    var mainModule = process.MainModule;
                    if (mainModule == null)
                        continue;

                    if (string.Equals(
                        Path.GetFullPath(mainModule.FileName),
                        installedFullPath,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return process;
                    }
                }
                catch
                {
                    // Si no se puede inspeccionar el proceso, se ignora y se sigue buscando.
                }
            }

            return null;
        }

        private static void IntentarEnfocarInstancia(Process process)
        {
            if (process == null)
                return;

            try
            {
                process.Refresh();
                var handle = process.MainWindowHandle;
                if (handle == IntPtr.Zero)
                    return;

                ShowWindowAsync(handle, RestoreWindowCommand);
                SetForegroundWindow(handle);
            }
            catch
            {
                // Mejor esfuerzo: si falla el foco, no se abre otra instancia.
            }
        }
    }
}
