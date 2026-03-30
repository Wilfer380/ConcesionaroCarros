using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConcesionaroCarros.Services
{
    public class LogDashboardService
    {
        public IReadOnlyList<AppLogEntry> LoadRecentEntries(int maxEntries = 300)
        {
            return LoadEntries(null, null, maxEntries);
        }

        public IReadOnlyList<string> GetAvailableMachines()
        {
            var root = LogService.PrimaryLogsDirectory;
            if (!Directory.Exists(root))
                return Array.Empty<string>();

            return Directory
                .GetDirectories(root)
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<string> GetAvailableDates(string machineName)
        {
            var machineDirectory = ResolveMachineDirectory(machineName);
            if (string.IsNullOrWhiteSpace(machineDirectory) || !Directory.Exists(machineDirectory))
                return Array.Empty<string>();

            var dates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var directory in Directory.GetDirectories(machineDirectory))
            {
                var name = Path.GetFileName(directory);
                if (LooksLikeDateFolder(name))
                    dates.Add(name);
            }

            foreach (var file in Directory.GetFiles(machineDirectory, "*.log", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (LooksLikeDateFolder(name))
                    dates.Add(name);
            }

            return dates
                .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<AppLogEntry> LoadEntries(string machineName, string date, int maxEntries = 300)
        {
            var files = EnumerateLogFiles(machineName, date)
                .Select(path => new FileInfo(path))
                .OrderByDescending(x => x.LastWriteTimeUtc)
                .Take(60)
                .ToList();

            var entries = new List<AppLogEntry>();
            foreach (var file in files)
                entries.AddRange(ParseFile(file.FullName));

            return entries
                .OrderByDescending(x => x.Timestamp)
                .Take(maxEntries)
                .ToList();
        }

        public LogDashboardSummary BuildSummary(IReadOnlyList<AppLogEntry> entries)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var latencyEntries = safeEntries
                .Where(x => string.Equals(x.Level, "LATENCY", StringComparison.OrdinalIgnoreCase) && x.DurationMs.HasValue)
                .ToList();

            return new LogDashboardSummary
            {
                TotalEvents = safeEntries.Count,
                ErrorCount = safeEntries.Count(x => string.Equals(x.Level, "ERROR", StringComparison.OrdinalIgnoreCase)),
                WarningCount = safeEntries.Count(x => string.Equals(x.Level, "WARNING", StringComparison.OrdinalIgnoreCase)),
                MachinesCount = safeEntries
                    .Select(x => x.MachineName ?? string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                AverageLatencyMs = latencyEntries.Count == 0
                    ? 0
                    : latencyEntries.Average(x => x.DurationMs ?? 0),
                LatestError = safeEntries
                    .Where(x => string.Equals(x.Level, "ERROR", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefault()
            };
        }

        public AppLogEntry GetLatestEntryForUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return null;

            return LoadEntries(null, null, 1000)
                .Where(x => string.Equals(
                    (x.UserName ?? string.Empty).Trim(),
                    userName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();
        }

        public AppLogEntry GetLatestEntryForMachine(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
                return null;

            return LoadEntries(machineName, null, 300)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();
        }

        private IEnumerable<string> EnumerateLogFiles(string machineName, string date)
        {
            var root = LogService.PrimaryLogsDirectory;
            if (!Directory.Exists(root))
                return Enumerable.Empty<string>();

            if (string.IsNullOrWhiteSpace(machineName) ||
                string.Equals(machineName, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                return Directory.GetFiles(root, "*.log", SearchOption.AllDirectories);
            }

            var machineDirectory = ResolveMachineDirectory(machineName);
            if (string.IsNullOrWhiteSpace(machineDirectory) || !Directory.Exists(machineDirectory))
                return Enumerable.Empty<string>();

            if (string.IsNullOrWhiteSpace(date) ||
                string.Equals(date, "Todas", StringComparison.OrdinalIgnoreCase))
            {
                return Directory.GetFiles(machineDirectory, "*.log", SearchOption.AllDirectories);
            }

            var datedDirectory = Path.Combine(machineDirectory, date);
            var files = new List<string>();

            if (Directory.Exists(datedDirectory))
                files.AddRange(Directory.GetFiles(datedDirectory, "*.log", SearchOption.TopDirectoryOnly));

            var legacyFile = Path.Combine(machineDirectory, date + ".log");
            if (File.Exists(legacyFile))
                files.Add(legacyFile);

            return files;
        }

        private static string ResolveMachineDirectory(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
                return null;

            return Path.Combine(LogService.PrimaryLogsDirectory, machineName);
        }

        private static bool LooksLikeDateFolder(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            DateTime parsed;
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out parsed);
        }

        private static IEnumerable<AppLogEntry> ParseFile(string path)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch
            {
                yield break;
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(new[] { '\t' }, 8);
                if (parts.Length < 7)
                    continue;

                if (!DateTime.TryParse(parts[0], out var timestamp))
                    timestamp = File.GetLastWriteTime(path);

                long duration;
                var details = parts.ElementAtOrDefault(7) ?? string.Empty;
                var source = parts.ElementAtOrDefault(4) ?? string.Empty;
                yield return new AppLogEntry
                {
                    Timestamp = timestamp,
                    Level = parts.ElementAtOrDefault(1) ?? string.Empty,
                    MachineName = parts.ElementAtOrDefault(2) ?? string.Empty,
                    UserName = NormalizeUserName(parts.ElementAtOrDefault(3) ?? string.Empty, source, details),
                    Source = source,
                    DurationMs = long.TryParse(parts.ElementAtOrDefault(5), out duration) ? duration : (long?)null,
                    Message = parts.ElementAtOrDefault(6) ?? string.Empty,
                    Details = details,
                    LogFilePath = path
                };
            }
        }

        private static string NormalizeUserName(string storedUserName, string source, string details)
        {
            var normalizedStoredUser = (storedUserName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalizedStoredUser))
                return normalizedStoredUser;

            if (!string.Equals(source, "Login", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(source, "AdminLogin", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedStoredUser;
            }

            var candidate = (details ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(candidate))
                return normalizedStoredUser;

            if (candidate.Contains("@"))
            {
                var at = candidate.IndexOf('@');
                return at > 0 ? candidate.Substring(0, at).Trim() : normalizedStoredUser;
            }

            return candidate;
        }
    }

    public class LogDashboardSummary
    {
        public int TotalEvents { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int MachinesCount { get; set; }
        public double AverageLatencyMs { get; set; }
        public AppLogEntry LatestError { get; set; }
    }
}
