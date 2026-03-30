using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace ConcesionaroCarros.Services
{
    public static class LogService
    {
        private const string SharedDatabasePathKey = "CC_SHARED_DATABASE_PATH";
        private static readonly object SyncRoot = new object();

        public static string PrimaryLogsDirectory => ResolvePrimaryLogsDirectory();

        public static void Info(string source, string message, string details = null)
        {
            Write("INFO", source, message, details, null, null);
        }

        public static void InfoForUser(string source, string message, string userName, string details = null)
        {
            Write("INFO", source, message, details, null, userName);
        }

        public static void Warning(string source, string message, string details = null)
        {
            Write("WARNING", source, message, details, null, null);
        }

        public static void WarningForUser(string source, string message, string userName, string details = null)
        {
            Write("WARNING", source, message, details, null, userName);
        }

        public static void Error(string source, string message, Exception ex = null, string details = null)
        {
            var fullDetails = details;
            if (ex != null)
            {
                fullDetails = string.IsNullOrWhiteSpace(fullDetails)
                    ? ex.ToString()
                    : fullDetails + " | " + ex;
            }

            Write("ERROR", source, message, fullDetails, null, null);
        }

        public static void ErrorForUser(string source, string message, string userName, Exception ex = null, string details = null)
        {
            var fullDetails = details;
            if (ex != null)
            {
                fullDetails = string.IsNullOrWhiteSpace(fullDetails)
                    ? ex.ToString()
                    : fullDetails + " | " + ex;
            }

            Write("ERROR", source, message, fullDetails, null, userName);
        }

        public static void Latency(string source, string message, long durationMs, string details = null)
        {
            Write("LATENCY", source, message, details, durationMs, null);
        }

        public static void LatencyForUser(string source, string message, string userName, long durationMs, string details = null)
        {
            Write("LATENCY", source, message, details, durationMs, userName);
        }

        public static IDisposable TrackLatency(string source, string message, string details = null)
        {
            return new LatencyScope(source, message, details);
        }

        public static string ResolveAuditUserName(string loginOrEmail)
        {
            return ResolveLogUserName(
                loginOrEmail,
                Environment.UserName ?? string.Empty,
                WindowsProfileService.ObtenerCorreoPrincipal());
        }

        public static string ResolveCurrentAuditUserName()
        {
            var correoSesion = (SesionUsuario.UsuarioActual?.Correo ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(correoSesion))
                return ResolveAuditUserName(correoSesion);

            return Environment.UserName ?? string.Empty;
        }

        public static string ResolveCurrentAuditEmail()
        {
            var deviceEmail = (WindowsProfileService.ObtenerCorreoPrincipal() ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(deviceEmail))
                return deviceEmail;

            var sessionEmail = (SesionUsuario.UsuarioActual?.Correo ?? string.Empty).Trim();
            return sessionEmail;
        }

        private static void Write(string level, string source, string message, string details, long? durationMs, string userName)
        {
            try
            {
                var timestamp = DateTime.Now;
                var line = string.Join("\t", new[]
                {
                    timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Sanitize(level),
                    Sanitize(Environment.MachineName),
                    Sanitize(!string.IsNullOrWhiteSpace(userName) ? userName : ResolveLogUserName()),
                    Sanitize(source),
                    durationMs?.ToString() ?? string.Empty,
                    Sanitize(message),
                    Sanitize(details)
                });

                var path = ResolveWritableLogFilePath(timestamp);
                var directory = Path.GetDirectoryName(path);

                lock (SyncRoot)
                {
                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch
            {
                // El log nunca debe tumbar la aplicación.
            }
        }

        private static string ResolveWritableLogFilePath(DateTime timestamp)
        {
            var primary = BuildLogFilePath(PrimaryLogsDirectory, timestamp);
            try
            {
                EnsureDirectoryWritable(Path.GetDirectoryName(primary));
                return primary;
            }
            catch
            {
                var fallbackRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SistemaDeInstalacion",
                    "LogsFallback");

                EnsureDirectoryWritable(Path.Combine(fallbackRoot, Environment.MachineName));
                return BuildLogFilePath(fallbackRoot, timestamp);
            }
        }

        private static string BuildLogFilePath(string root, DateTime timestamp)
        {
            return Path.Combine(
                root,
                Environment.MachineName,
                timestamp.ToString("yyyy-MM-dd"),
                "events.log");
        }

        private static void EnsureDirectoryWritable(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return;

            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, ".write-test.tmp");
            File.WriteAllText(probePath, "ok");
            File.Delete(probePath);
        }

        private static string ResolvePrimaryLogsDirectory()
        {
            var configuredDbPath = ConfigurationManager.AppSettings[SharedDatabasePathKey];
            if (!string.IsNullOrWhiteSpace(configuredDbPath))
            {
                configuredDbPath = Environment.ExpandEnvironmentVariables(configuredDbPath.Trim());
                if (Path.IsPathRooted(configuredDbPath))
                {
                    var dbDirectory = Path.GetDirectoryName(configuredDbPath);
                    var installerRoot = Directory.GetParent(dbDirectory ?? string.Empty)?.FullName;
                    if (!string.IsNullOrWhiteSpace(installerRoot))
                        return Path.Combine(installerRoot, "Logs");
                }
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaDeInstalacion",
                "Logs");
        }

        private static string ResolveLogUserName()
        {
            return ResolveLogUserName(
                (SesionUsuario.UsuarioActual?.Correo ?? string.Empty).Trim(),
                Environment.UserName ?? string.Empty,
                WindowsProfileService.ObtenerCorreoPrincipal());
        }

        private static string ResolveLogUserName(string loginValue, string deviceUserName, string deviceEmail)
        {
            var candidate = (loginValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(candidate))
                return deviceUserName ?? string.Empty;

            if (candidate.Contains("@"))
            {
                if (!string.IsNullOrWhiteSpace(deviceEmail) &&
                    string.Equals(candidate, deviceEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return deviceUserName ?? string.Empty;
                }

                var at = candidate.IndexOf('@');
                return at > 0 ? candidate.Substring(0, at).Trim() : candidate;
            }

            return candidate;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
        }

        private sealed class LatencyScope : IDisposable
        {
            private readonly string _source;
            private readonly string _message;
            private readonly string _details;
            private readonly Stopwatch _stopwatch;

            public LatencyScope(string source, string message, string details)
            {
                _source = source;
                _message = message;
                _details = details;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                Latency(_source, _message, _stopwatch.ElapsedMilliseconds, _details);
            }
        }
    }
}
