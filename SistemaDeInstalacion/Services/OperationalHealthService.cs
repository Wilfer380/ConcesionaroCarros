using ConcesionaroCarros.Db;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;

namespace ConcesionaroCarros.Services
{
    public sealed class OperationalHealthService : IDisposable
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(5);

        private readonly object _syncRoot = new object();
        private Timer _timer;
        private bool _disposed;
        private int _isPublishing;

        public void Start()
        {
            lock (_syncRoot)
            {
                if (_disposed || _timer != null)
                    return;

                _timer = new Timer(_ => PublishSnapshot(false), null, HeartbeatInterval, HeartbeatInterval);
            }

            PublishSnapshot(true);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _timer?.Dispose();
                _timer = null;
            }
        }

        private static bool IsSharedPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase);
        }

        private void PublishSnapshot(bool isStartup)
        {
            if (Interlocked.Exchange(ref _isPublishing, 1) == 1)
                return;

            try
            {
                var databaseState = ProbeDatabase(out var databaseDetails);
                var logSinkState = ProbeLogSink(out var logSinkDetails);
                var isHealthy = databaseState == "healthy" && logSinkState == "healthy";
                var state = isHealthy ? "healthy" : "degraded";

                if (isStartup)
                {
                    LogService.Health(
                        "AppHealth",
                        isHealthy ? "Chequeo de arranque completado" : "Chequeo de arranque con degradacion observable",
                        "signal=startup|event=startup_check|state=" + state + "|coverage=partial|scope=desktop_local_only|database=" + databaseState + "|log_sink=" + logSinkState);
                }

                LogService.Health(
                    "AppHealth",
                    isHealthy ? "Heartbeat operativo" : "Heartbeat con degradacion observable",
                    "signal=heartbeat|event=heartbeat|state=" + state + "|coverage=partial|scope=desktop_local_only|interval_minutes=5|database=" + databaseState + "|log_sink=" + logSinkState);

                LogService.Health("AppHealth", "Estado de base de datos", databaseDetails);
                LogService.Health("AppHealth", "Estado de escritura de logs", logSinkDetails);
            }
            catch (Exception ex)
            {
                LogService.Health(
                    "AppHealth",
                    "Heartbeat no pudo completarse",
                    "signal=heartbeat|event=heartbeat|state=error|coverage=partial|scope=desktop_local_only|reason=health_probe_exception|error=" + SanitizeValue(ex.Message));
            }
            finally
            {
                Interlocked.Exchange(ref _isPublishing, 0);
            }
        }

        private static string ProbeDatabase(out string details)
        {
            var dbPath = DatabaseInitializer.CurrentDbPath;
            var location = IsSharedPath(dbPath) ? "shared" : "local";

            if (string.IsNullOrWhiteSpace(dbPath))
            {
                details = "signal=dependency|event=dependency|dependency=database|state=unhealthy|coverage=active|location=" + location + "|reason=path_missing";
                return "unhealthy";
            }

            if (!File.Exists(dbPath))
            {
                details = "signal=dependency|event=dependency|dependency=database|state=unhealthy|coverage=active|location=" + location + "|reason=file_missing|path=" + SanitizeValue(dbPath);
                return "unhealthy";
            }

            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadWrite"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1;";
                        command.ExecuteScalar();
                    }
                }

                details = "signal=dependency|event=dependency|dependency=database|state=healthy|coverage=active|location=" + location + "|path=" + SanitizeValue(dbPath);
                return "healthy";
            }
            catch (Exception ex)
            {
                details = "signal=dependency|event=dependency|dependency=database|state=unhealthy|coverage=active|location=" + location + "|reason=probe_failed|path=" + SanitizeValue(dbPath) + "|error=" + SanitizeValue(ex.Message);
                return "unhealthy";
            }
        }

        private static string ProbeLogSink(out string details)
        {
            var primaryRoot = LogService.PrimaryLogsDirectory;
            var fallbackRoot = LogService.FallbackLogsDirectory;
            var primaryOk = TryProbeDirectory(Path.Combine(primaryRoot, Environment.MachineName), out var primaryError);

            if (primaryOk)
            {
                details = "signal=dependency|event=dependency|dependency=log_sink|state=healthy|coverage=active|sink=primary|primary_state=healthy|fallback_path=" + SanitizeValue(fallbackRoot);
                return "healthy";
            }

            var fallbackOk = TryProbeDirectory(Path.Combine(fallbackRoot, Environment.MachineName), out var fallbackError);
            var state = fallbackOk ? "degraded" : "unhealthy";
            var sink = fallbackOk ? "fallback" : "none";

            details = "signal=dependency|event=dependency|dependency=log_sink|state=" + state + "|coverage=active|sink=" + sink + "|primary_state=error|fallback_state=" + (fallbackOk ? "healthy" : "error") + "|path=" + SanitizeValue(primaryRoot) + "|fallback_path=" + SanitizeValue(fallbackRoot) + "|error=" + SanitizeValue(primaryError) + "|fallback_error=" + SanitizeValue(fallbackError);
            return state;
        }

        private static bool TryProbeDirectory(string directory, out string error)
        {
            error = null;

            try
            {
                Directory.CreateDirectory(directory);
                var probePath = Path.Combine(directory, ".health-probe.tmp");
                File.WriteAllText(probePath, "ok");
                File.Delete(probePath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string SanitizeValue(string value)
        {
            return (value ?? string.Empty)
                .Replace("|", "/")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }
    }
}
