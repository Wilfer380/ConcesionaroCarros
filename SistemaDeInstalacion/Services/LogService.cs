using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ConcesionaroCarros.Services
{
    public static class LogService
    {
        private const string SharedDatabasePathKey = "CC_SHARED_DATABASE_PATH";
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Stream en vivo para dashboards/telemetría en la misma instancia de la app.
        /// Best-effort: un handler defectuoso NO debe romper el logging.
        /// </summary>
        public static event Action<AppLogEntry> LogWritten;

        public static string PrimaryLogsDirectory => ResolvePrimaryLogsDirectory();
        public static string FallbackLogsDirectory => ResolveFallbackLogsDirectory();

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
            var finalMessage = ex == null
                ? message
                : BuildErrorMessage(message, ex);
            var fullDetails = details;
            if (ex != null)
            {
                fullDetails = string.IsNullOrWhiteSpace(fullDetails)
                    ? BuildExceptionDetails(ex)
                    : details + " | " + BuildExceptionDetails(ex);
            }

            Write("ERROR", source, finalMessage, fullDetails, null, null);
        }

        public static void ErrorForUser(string source, string message, string userName, Exception ex = null, string details = null)
        {
            var finalMessage = ex == null
                ? message
                : BuildErrorMessage(message, ex);
            var fullDetails = details;
            if (ex != null)
            {
                fullDetails = string.IsNullOrWhiteSpace(fullDetails)
                    ? BuildExceptionDetails(ex)
                    : details + " | " + BuildExceptionDetails(ex);
            }

            Write("ERROR", source, finalMessage, fullDetails, null, userName);
        }

        public static void Latency(string source, string message, long durationMs, string details = null)
        {
            Write("LATENCY", source, message, details, durationMs, null);
        }

        public static void LatencyForUser(string source, string message, string userName, long durationMs, string details = null)
        {
            Write("LATENCY", source, message, details, durationMs, userName);
        }

        public static void Session(string source, string message, string userName, string details = null)
        {
            Write("SESSION", source, message, details, null, userName);
        }

        public static void Health(string source, string message, string details = null)
        {
            Write("HEALTH", source, message, details, null, null);
        }

        public static void Validation(string source, string rule, bool isAccepted, string details = null, string userName = null)
        {
            // IMPORTANT: keep semantic tokens stable across UI languages so dashboards can parse results reliably.
            var message = isAccepted
                ? LocalizedText.Get("Logs_ValidationAcceptedMessage", "Validación aceptada")
                : LocalizedText.Get("Logs_ValidationRejectedMessage", "Validación rechazada");

            var semanticPrefix = string.IsNullOrWhiteSpace(rule)
                ? "event=validation|accepted=" + (isAccepted ? "true" : "false")
                : "event=validation|rule=" + rule.Trim() + "|accepted=" + (isAccepted ? "true" : "false");

            var semanticDetails = string.IsNullOrWhiteSpace(details)
                ? semanticPrefix
                : semanticPrefix + " | " + details;

            Write("VALIDATION", source, message, semanticDetails, null, userName);
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

                TryRaiseLogWritten(new AppLogEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    MachineName = Environment.MachineName,
                    UserName = !string.IsNullOrWhiteSpace(userName) ? userName : ResolveLogUserName(),
                    Source = source,
                    DurationMs = durationMs,
                    Message = message,
                    Details = details,
                    LogFilePath = path
                });
            }
            catch
            {
                // El log nunca debe tumbar la aplicación.
            }
        }

        private static void TryRaiseLogWritten(AppLogEntry entry)
        {
            try
            {
                LogWritten?.Invoke(entry);
            }
            catch
            {
                // Best-effort.
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
                var fallbackRoot = FallbackLogsDirectory;

                EnsureDirectoryWritable(Path.Combine(fallbackRoot, Environment.MachineName));
                return BuildLogFilePath(fallbackRoot, timestamp);
            }
        }

        public static string[] GetReadableLogsDirectories()
        {
            return new[] { PrimaryLogsDirectory, FallbackLogsDirectory }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
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

        private static string ResolveFallbackLogsDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaDeInstalacion",
                "LogsFallback");
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

        private static string BuildErrorMessage(string message, Exception ex)
        {
            var summary = BuildExceptionSummary(ex);
            if (string.IsNullOrWhiteSpace(summary))
                return message;

            return string.IsNullOrWhiteSpace(message)
                ? summary
                : message + ": " + summary;
        }

        private static string BuildExceptionSummary(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            var root = ex;
            while (root.InnerException != null)
                root = root.InnerException;

            var typeName = root.GetType().Name;
            var exceptionMessage = (root.Message ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(exceptionMessage))
                return typeName;

            return typeName + " - " + exceptionMessage;
        }

        private static string BuildExceptionDetails(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            var root = ex;
            while (root.InnerException != null)
                root = root.InnerException;

            var details = "Tipo=" + root.GetType().FullName;

            if (!string.IsNullOrWhiteSpace(root.Message))
                details += " | Mensaje=" + root.Message.Trim();

            if (!string.IsNullOrWhiteSpace(root.Source))
                details += " | Fuente=" + root.Source.Trim();

            if (!string.IsNullOrWhiteSpace(root.StackTrace))
                details += " | Stack=" + root.StackTrace.Trim();

            return details;
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
