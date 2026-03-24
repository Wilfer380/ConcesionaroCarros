using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace LauncherSistema
{
    internal static class LauncherService
    {
        private const string SharedRoot =
            "\\\\comde019\\DFSMDE\\PUBLIC\\CO_MDE_DISENO_DI\\RESPALDO DISE\u00d1OS\\SAP - Respaldo dise\u00f1os\\FORMATOS SAP\\InstallerSystem";

        private static readonly string VersionPath = Path.Combine(SharedRoot, "version.txt");
        private static readonly string SetupPath = Path.Combine(SharedRoot, "SetupSistema.exe");
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

            if (!File.Exists(InstalledAppPath))
            {
                SolicitarInstalacion();
                return;
            }

            if (serverVersion != null && localVersion != null && serverVersion > localVersion)
            {
                var result = MessageBox.Show(
                    $"Se encontro una nueva version disponible ({serverVersion}).{Environment.NewLine}{Environment.NewLine}Deseas actualizar ahora?",
                    "Actualizacion disponible",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    EjecutarActualizacion();
                    return;
                }
            }

            AbrirAplicacionLocalConReintento();
        }

        private static Version ObtenerVersionServidor()
        {
            try
            {
                if (!File.Exists(VersionPath))
                    return null;

                var content = File.ReadAllText(VersionPath).Trim();
                return Version.TryParse(content, out var version) ? version : null;
            }
            catch
            {
                return null;
            }
        }

        private static Version ObtenerVersionLocal()
        {
            try
            {
                if (!File.Exists(InstalledAppPath))
                    return null;

                var info = FileVersionInfo.GetVersionInfo(InstalledAppPath);
                var versionText = string.IsNullOrWhiteSpace(info.ProductVersion)
                    ? info.FileVersion
                    : info.ProductVersion;

                return Version.TryParse(versionText, out var version) ? version : null;
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = SetupPath,
                    Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NOCANCEL /NORESTART",
                    UseShellExecute = true
                });
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

            MessageBox.Show(
                "La actualizacion se iniciara ahora. La aplicacion se volvera a abrir automaticamente cuando termine.",
                "Actualizacion iniciada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Environment.Exit(0);
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
            for (var attempt = 0; attempt < 10; attempt++)
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
                    // Espera corta mientras Windows termina de actualizar archivos.
                }

                Thread.Sleep(1000);
            }

            AbrirAplicacionLocal();
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
    }
}
