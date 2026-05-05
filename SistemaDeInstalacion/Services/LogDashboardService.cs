using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ConcesionaroCarros.Services
{
    public class LogDashboardService
    {
        private const string AllFilterValue = "__all";

        public IReadOnlyList<string> GetAvailableMachines()
        {
            return LogService
                .GetReadableLogsDirectories()
                .Where(Directory.Exists)
                .SelectMany(Directory.GetDirectories)
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public LogDashboardSnapshot GetDashboardSnapshot(LogDashboardQuery query)
        {
            var safeQuery = query ?? new LogDashboardQuery();
            var range = ResolveTimeRange(safeQuery.TimeRangeKey);
            var baseEntries = LoadEntriesForRange(safeQuery.MachineName, range.Start);
            var filteredEntries = ApplyFilters(baseEntries, safeQuery, range.Start);
            var summary = BuildSummary(filteredEntries);
            var periodBuckets = ResolvePeriodBuckets(filteredEntries, safeQuery.TimeRangeKey);
            var sourceActivity = BuildDistribution(filteredEntries, x => x.Source, 6, false);
            var userActivity = BuildDistribution(filteredEntries, x => x.UserName, 6, false);
            var machineActivity = BuildDistribution(filteredEntries, x => x.MachineName, 6, false);
            var latencyDistribution = BuildLatencyDistribution(filteredEntries);
            var operationalHealth = BuildOperationalHealthSnapshot(baseEntries, filteredEntries);
            var criticalEvents = BuildCriticalEvents(filteredEntries);
            var instrumentationStatus = BuildInstrumentationStatus(baseEntries);
            var statusSections = BuildStatusSections(
                filteredEntries,
                summary,
                periodBuckets,
                sourceActivity,
                userActivity,
                machineActivity,
                latencyDistribution,
                baseEntries,
                instrumentationStatus,
                operationalHealth);

            return new LogDashboardSnapshot
            {
                Entries = filteredEntries,
                Summary = summary,
                AvailableMachines = GetAvailableMachines(),
                AvailableLevels = BuildAvailableLevels(baseEntries),
                AvailableSources = BuildAvailableValues(baseEntries.Select(x => x.Source)),
                AvailableUsers = BuildAvailableValues(baseEntries.Select(x => x.UserName)),
                ErrorSeries = BuildPeriodSeries(filteredEntries, "ERROR", safeQuery.TimeRangeKey),
                WarningSeries = BuildPeriodSeries(filteredEntries, "WARNING", safeQuery.TimeRangeKey),
                SourceActivity = sourceActivity,
                UserActivity = userActivity,
                MachineActivity = machineActivity,
                LatencyDistribution = latencyDistribution,
                CriticalEvents = criticalEvents,
                TimelineEvents = criticalEvents,
                InstrumentationStatus = instrumentationStatus,
                HasHealthSignals = baseEntries.Any(IsHealthSignal),
                HasSessionSignals = baseEntries.Any(IsSessionSignal),
                HasValidationSignals = baseEntries.Any(IsValidationSignal),
                ExecutiveStatus = BuildExecutiveStatus(filteredEntries, summary, range.Key, instrumentationStatus, baseEntries, operationalHealth),
                StatusSections = statusSections
            };
        }

        public AppLogEntry GetLatestEntryForUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return null;

            return LoadEntriesForRange(null, DateTime.Now.AddDays(-30))
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

            return LoadEntriesForRange(machineName, DateTime.Now.AddDays(-30))
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();
        }

        public LogDashboardSummary BuildSummary(IReadOnlyList<AppLogEntry> entries)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var latencyEntries = safeEntries
                .Where(x => x.DurationMs.HasValue)
                .Select(x => (double)(x.DurationMs ?? 0L))
                .OrderBy(x => x)
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
                AverageLatencyMs = latencyEntries.Count == 0 ? 0 : latencyEntries.Average(),
                P95LatencyMs = latencyEntries.Count == 0 ? 0 : latencyEntries[(int)Math.Ceiling(latencyEntries.Count * 0.95) - 1],
                LatestError = safeEntries
                    .Where(x => string.Equals(x.Level, "ERROR", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefault(),
                LatestCriticalEvent = safeEntries
                    .Where(IsCriticalEvent)
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefault()
            };
        }

        private static IReadOnlyList<string> BuildAvailableLevels(IEnumerable<AppLogEntry> entries)
        {
            return BuildAvailableValues(entries.Select(x => x.Level));
        }

        private static IReadOnlyList<string> BuildAvailableValues(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<AppLogEntry> ApplyFilters(
            IReadOnlyList<AppLogEntry> entries,
            LogDashboardQuery query,
            DateTime? rangeStart)
        {
            IEnumerable<AppLogEntry> filtered = entries ?? Array.Empty<AppLogEntry>();

            if (rangeStart.HasValue)
                filtered = filtered.Where(x => x.Timestamp >= rangeStart.Value);

            if (!string.IsNullOrWhiteSpace(query.Severity) && !IsAllValue(query.Severity))
            {
                filtered = filtered.Where(x => string.Equals(
                    x.Level ?? string.Empty,
                    query.Severity,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.Source) && !IsAllValue(query.Source))
            {
                filtered = filtered.Where(x => string.Equals(
                    x.Source ?? string.Empty,
                    query.Source,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.UserName) && !IsAllValue(query.UserName))
            {
                filtered = filtered.Where(x => string.Equals(
                    x.UserName ?? string.Empty,
                    query.UserName,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var search = query.SearchText.Trim();
                filtered = filtered.Where(x => ContainsText(x.Message, search) ||
                                              ContainsText(x.Details, search) ||
                                              ContainsText(x.Source, search) ||
                                              ContainsText(x.UserName, search) ||
                                              ContainsText(x.MachineName, search));
            }

            return filtered
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }

        private IReadOnlyList<AppLogEntry> LoadEntriesForRange(string machineName, DateTime? rangeStart)
        {
            var files = EnumerateLogFiles(machineName, rangeStart)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => new FileInfo(path))
                .OrderByDescending(x => x.LastWriteTimeUtc)
                .ToList();

            var entries = new List<AppLogEntry>();
            foreach (var file in files)
            {
                foreach (var entry in ParseFile(file.FullName))
                {
                    if (!rangeStart.HasValue || entry.Timestamp >= rangeStart.Value)
                        entries.Add(entry);
                }
            }

            return entries
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }

        private IEnumerable<string> EnumerateLogFiles(string machineName, DateTime? rangeStart)
        {
            foreach (var root in LogService.GetReadableLogsDirectories().Where(Directory.Exists))
            {
                var machineDirectories = ResolveMachineDirectories(root, machineName);
                foreach (var directory in machineDirectories)
                {
                    foreach (var file in EnumerateMachineLogFiles(directory, rangeStart))
                        yield return file;
                }
            }
        }

        private static IEnumerable<string> ResolveMachineDirectories(string root, string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName) || IsAllValue(machineName))
                return Directory.GetDirectories(root);

            var machineDirectory = Path.Combine(root, machineName);
            if (string.IsNullOrWhiteSpace(machineDirectory) || !Directory.Exists(machineDirectory))
                return Array.Empty<string>();

            return new[] { machineDirectory };
        }

        private static IEnumerable<string> EnumerateMachineLogFiles(string machineDirectory, DateTime? rangeStart)
        {
            if (!Directory.Exists(machineDirectory))
                yield break;

            var minimumDate = rangeStart.HasValue ? rangeStart.Value.Date : DateTime.MinValue.Date;

            foreach (var directory in Directory.GetDirectories(machineDirectory))
            {
                var folderName = Path.GetFileName(directory);
                if (LooksLikeDateFolder(folderName))
                {
                    var folderDate = DateTime.ParseExact(folderName, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (folderDate < minimumDate)
                        continue;
                }

                foreach (var file in Directory.GetFiles(directory, "*.log", SearchOption.TopDirectoryOnly))
                    yield return file;
            }

            foreach (var file in Directory.GetFiles(machineDirectory, "*.log", SearchOption.TopDirectoryOnly))
            {
                if (!rangeStart.HasValue)
                {
                    yield return file;
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(file);
                if (!LooksLikeDateFolder(name))
                {
                    yield return file;
                    continue;
                }

                var fileDate = DateTime.ParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (fileDate >= minimumDate)
                    yield return file;
            }
        }

        private static IReadOnlyList<LogMetricPoint> BuildPeriodSeries(
            IReadOnlyList<AppLogEntry> entries,
            string level,
            string timeRangeKey)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var buckets = ResolvePeriodBuckets(safeEntries, timeRangeKey);
            return BuildPeriodSeries(safeEntries, buckets, x =>
                string.Equals(x.Level, level, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<LogMetricPoint> BuildPeriodSeries(
            IReadOnlyList<AppLogEntry> entries,
            IReadOnlyList<PeriodBucket> buckets,
            Func<AppLogEntry, bool> predicate)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var points = new List<LogMetricPoint>();

            foreach (var bucket in buckets ?? Array.Empty<PeriodBucket>())
            {
                points.Add(new LogMetricPoint
                {
                    Label = bucket.Label,
                    Count = safeEntries.Count(x =>
                        x.Timestamp >= bucket.Start &&
                        x.Timestamp < bucket.End &&
                        (predicate == null || predicate(x)))
                });
            }

            return points;
        }

        private static IReadOnlyList<LogMetricDistributionItem> BuildDistribution(
            IReadOnlyList<AppLogEntry> entries,
            Func<AppLogEntry, string> selector,
            int take,
            bool includeAverageLatency)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();

            return safeEntries
                .GroupBy(x => NormalizeLabel(selector(x)), StringComparer.OrdinalIgnoreCase)
                .Select(group => new LogMetricDistributionItem
                {
                    Label = group.Key,
                    FilterValue = group
                        .Select(selector)
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                        ?.Trim(),
                    Count = group.Count(),
                    SecondaryText = includeAverageLatency
                        ? BuildLatencyText(group)
                        : group.Count() == 1 ? T("Logs_Dashboard_OneEvent", "1 evento") : F("Logs_Dashboard_NEventsFormat", "{0} eventos", group.Count().ToString("N0"))
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Take(take)
                .ToList();
        }

        private static IReadOnlyList<LogMetricDistributionItem> BuildLatencyDistribution(IReadOnlyList<AppLogEntry> entries)
        {
            var latencyEntries = (entries ?? Array.Empty<AppLogEntry>())
                .Where(x => x.DurationMs.HasValue)
                .ToList();

            var bands = new[]
            {
                new LatencyBand("< 250 ms", 0, 250),
                new LatencyBand("250-499 ms", 250, 500),
                new LatencyBand("500-999 ms", 500, 1000),
                new LatencyBand("1-2 s", 1000, 2000),
                new LatencyBand(">= 2 s", 2000, long.MaxValue)
            };

            return bands
                .Select(band => new LogMetricDistributionItem
                {
                    Label = band.Label,
                    Count = latencyEntries.Count(x =>
                    {
                        var duration = x.DurationMs ?? 0L;
                        return duration >= band.MinimumInclusive && duration < band.MaximumExclusive;
                    }),
                    SecondaryText = latencyEntries.Count == 0
                        ? T("Logs_Dashboard_NoLatencyData", "Sin latencias")
                        : BuildPercentageText(latencyEntries.Count(x =>
                        {
                            var duration = x.DurationMs ?? 0L;
                            return duration >= band.MinimumInclusive && duration < band.MaximumExclusive;
                        }), latencyEntries.Count)
                })
                .ToList();
        }

        private static IReadOnlyList<AppLogEntry> BuildCriticalEvents(IReadOnlyList<AppLogEntry> entries)
        {
            return (entries ?? Array.Empty<AppLogEntry>())
                .Where(IsCriticalEvent)
                .OrderByDescending(x => x.Timestamp)
                .Take(8)
                .ToList();
        }

        private static IReadOnlyList<PeriodBucket> ResolvePeriodBuckets(IReadOnlyList<AppLogEntry> entries, string timeRangeKey)
        {
            var now = DateTime.Now;
            var range = ResolveTimeRange(timeRangeKey);
            var start = range.Start
                ?? (entries != null && entries.Count > 0
                    ? entries.Min(x => x.Timestamp)
                    : now.AddDays(-7));
            var totalHours = Math.Max((now - start).TotalHours, 1);

            if (string.Equals(timeRangeKey, "2h", StringComparison.OrdinalIgnoreCase))
                return BuildBuckets(start, now, TimeSpan.FromMinutes(10), "HH:mm");

            if (string.Equals(timeRangeKey, "24h", StringComparison.OrdinalIgnoreCase))
                return BuildBuckets(start, now, TimeSpan.FromHours(4), "HH:mm");

            if (string.Equals(timeRangeKey, "7d", StringComparison.OrdinalIgnoreCase))
                return BuildBuckets(start.Date, now.Date.AddDays(1), TimeSpan.FromDays(1), "dd MMM");

            if (string.Equals(timeRangeKey, "30d", StringComparison.OrdinalIgnoreCase))
                return BuildBuckets(start.Date, now.Date.AddDays(1), TimeSpan.FromDays(5), "dd MMM");

            if (totalHours <= 48)
                return BuildBuckets(start, now, TimeSpan.FromHours(6), "HH:mm");

            var spanDays = Math.Max((now.Date - start.Date).TotalDays, 1);
            var daysPerBucket = Math.Max(1, (int)Math.Ceiling(spanDays / 6));
            return BuildBuckets(start.Date, now.Date.AddDays(1), TimeSpan.FromDays(daysPerBucket), "dd MMM");
        }

        private static IReadOnlyList<PeriodBucket> BuildBuckets(DateTime start, DateTime end, TimeSpan step, string format)
        {
            var buckets = new List<PeriodBucket>();
            var current = start;
            while (current < end)
            {
                var next = current.Add(step);
                if (next > end)
                    next = end;

                buckets.Add(new PeriodBucket
                {
                    Start = current,
                    End = next,
                    Label = current.ToString(format, CultureInfo.CurrentCulture)
                });

                current = next;
            }

            return buckets;
        }

        private static LogTimeRangeWindow ResolveTimeRange(string timeRangeKey)
        {
            var now = DateTime.Now;
            if (string.Equals(timeRangeKey, "2h", StringComparison.OrdinalIgnoreCase))
                return new LogTimeRangeWindow("2h", now.AddHours(-2));

            if (string.Equals(timeRangeKey, "24h", StringComparison.OrdinalIgnoreCase))
                return new LogTimeRangeWindow("24h", now.AddHours(-24));

            if (string.Equals(timeRangeKey, "30d", StringComparison.OrdinalIgnoreCase))
                return new LogTimeRangeWindow("30d", now.AddDays(-30));

            if (string.Equals(timeRangeKey, "all", StringComparison.OrdinalIgnoreCase))
                return new LogTimeRangeWindow("all", null);

            return new LogTimeRangeWindow("7d", now.AddDays(-7));
        }

        private static bool IsCriticalEvent(AppLogEntry entry)
        {
            if (entry == null)
                return false;

            if (IsOperationalDegradation(entry))
                return true;

            if (string.Equals(entry.Level, "ERROR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.Level, "WARNING", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(entry.Level, "VALIDATION", StringComparison.OrdinalIgnoreCase))
                return ContainsText(entry.Message, "rechazada");

            if (string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase))
                return IsHealthIncident(entry);

            return false;
        }

        private static bool IsHeartbeatSignal(AppLogEntry entry)
        {
            return entry != null &&
                   string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase) &&
                   (string.Equals(GetSemanticValue(entry, "signal"), "heartbeat", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetSemanticValue(entry, "event"), "heartbeat", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsDependencySignal(AppLogEntry entry)
        {
            return entry != null &&
                   string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase) &&
                   (string.Equals(GetSemanticValue(entry, "signal"), "dependency", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetSemanticValue(entry, "event"), "dependency", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsStartupSignal(AppLogEntry entry)
        {
            return entry != null &&
                   string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase) &&
                   (string.Equals(GetSemanticValue(entry, "signal"), "startup", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetSemanticValue(entry, "event"), "startup_check", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSessionSignal(AppLogEntry entry)
        {
            if (entry == null)
                return false;

            if (string.Equals(entry.Level, "SESSION", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(entry.Source, "Login", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entry.Source, "AdminLogin", StringComparison.OrdinalIgnoreCase) ||
                   (string.Equals(entry.Source, "MainWindow", StringComparison.OrdinalIgnoreCase) &&
                    ContainsText(entry.Message, "sesion"));
        }

        private static bool IsHealthSignal(AppLogEntry entry)
        {
            if (entry == null)
                return false;

            if (string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(entry.Source, "App", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entry.Source, "AppLifecycle", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entry.Source, "ThemeManager", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidationSignal(AppLogEntry entry)
        {
            if (entry == null)
                return false;

            return string.Equals(entry.Level, "VALIDATION", StringComparison.OrdinalIgnoreCase) ||
                   ContainsText(entry.Message, "validacion") ||
                   ContainsText(entry.Details, "Regla=");
        }

        private static bool IsRejectedValidation(AppLogEntry entry)
        {
            if (entry == null || !IsValidationSignal(entry))
                return false;

            // Prefer semantic token (language-agnostic).
            var accepted = GetSemanticValue(entry, "accepted");
            if (!string.IsNullOrWhiteSpace(accepted))
                return string.Equals(accepted.Trim(), "false", StringComparison.OrdinalIgnoreCase);

            // Back-compat for older localized messages.
            return ContainsText(entry.Message, "rechazada") || ContainsText(entry.Details, "rechazada");
        }

        private static string BuildInstrumentationStatus(IReadOnlyList<AppLogEntry> entries)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var notes = new List<string>();

            if (!safeEntries.Any(IsHealthSignal))
                notes.Add(T("Logs_Dashboard_InstrumentationNoteNoHealth", "Disponibilidad real aún no instrumentada; solo se muestran eventos de app/health cuando existen."));

            if (!safeEntries.Any(IsHeartbeatSignal))
                notes.Add(T("Logs_Dashboard_InstrumentationNoteNoHeartbeat", "Heartbeat de escritorio aún no es visible para todo el histórico filtrado."));

            if (!safeEntries.Any(IsDependencySignal))
                notes.Add(T("Logs_Dashboard_InstrumentationNoteNoDependencies", "Dependencias críticas aún no tienen cobertura observable completa en la ventana actual."));

            if (!safeEntries.Any(IsSessionSignal))
                notes.Add(T("Logs_Dashboard_InstrumentationNoteNoSessions", "Sesiones persistidas aún no están capturadas; por ahora se infiere actividad desde login/logout."));

            if (!safeEntries.Any(IsValidationSignal))
                notes.Add(T("Logs_Dashboard_InstrumentationNotePartialValidations", "Validaciones semánticamente explotables aún son parciales."));

            return notes.Count == 0
                ? T("Logs_Dashboard_InstrumentationCoverageActive", "Cobertura observable activa para health, heartbeat, dependencias, sesiones y validaciones. No representa uptime contractual.")
                : string.Join(" ", notes);
        }

        private static bool IsHealthIncident(AppLogEntry entry)
        {
            if (entry == null || !string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase))
                return false;

            var state = ResolveHealthState(entry);
            if (string.Equals(state, "unhealthy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "degraded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "partial", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return ContainsText(entry.Message, "error") ||
                   ContainsText(entry.Message, "fall") ||
                   ContainsText(entry.Message, "exception");
        }

        private static bool IsOperationalDegradation(AppLogEntry entry)
        {
            if (entry == null || !string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase))
                return false;

            var state = ResolveHealthState(entry);
            return string.Equals(state, "degraded", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(state, "partial", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveHealthState(AppLogEntry entry)
        {
            var semanticState = GetSemanticValue(entry, "state");
            if (!string.IsNullOrWhiteSpace(semanticState))
                return semanticState.Trim();

            if (ContainsText(entry?.Message, "degrad"))
                return "degraded";

            if (ContainsText(entry?.Message, "fall") || ContainsText(entry?.Message, "error"))
                return "unhealthy";

            return string.Empty;
        }

        private static string ResolveDependencyName(AppLogEntry entry)
        {
            var dependency = GetSemanticValue(entry, "dependency");
            return string.IsNullOrWhiteSpace(dependency) ? "dependency" : dependency.Trim();
        }

        private static string GetSemanticValue(AppLogEntry entry, string key)
        {
            return entry == null ? string.Empty : GetSemanticValue(entry.Details, key);
        }

        private static string GetSemanticValue(string details, string key)
        {
            if (string.IsNullOrWhiteSpace(details) || string.IsNullOrWhiteSpace(key))
                return string.Empty;

            // Back-compat: older logs used Spanish keys (e.g. "evento=") but the dashboard parser uses English keys.
            if (string.Equals(key, "event", StringComparison.OrdinalIgnoreCase))
            {
                var value = GetSemanticValue(details, "evento", allowFallback: false);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return GetSemanticValue(details, key, allowFallback: true);
        }

        private static string GetSemanticValue(string details, string key, bool allowFallback)
        {
            if (string.IsNullOrWhiteSpace(details) || string.IsNullOrWhiteSpace(key))
                return string.Empty;

            foreach (var part in details.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = part.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var currentKey = part.Substring(0, separatorIndex).Trim();
                if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                return part.Substring(separatorIndex + 1).Trim();
            }

            return string.Empty;
        }

        private static string BuildLatencyText(IGrouping<string, AppLogEntry> group)
        {
            var latencyValues = group
                .Where(x => x.DurationMs.HasValue)
                .Select(x => x.DurationMs ?? 0L)
                .ToList();

            if (latencyValues.Count == 0)
                return group.Count() == 1 ? T("Logs_Dashboard_OneEvent", "1 evento") : F("Logs_Dashboard_NEventsFormat", "{0} eventos", group.Count().ToString("N0"));

            return F("Logs_Dashboard_AverageMsFormat", "Promedio {0} ms", latencyValues.Average().ToString("N0"));
        }

        private static string BuildPercentageText(int part, int total)
        {
            if (total <= 0)
                return "0%";

            return ((part * 100d) / total).ToString("N0", CultureInfo.InvariantCulture) + "%";
        }

        private static string NormalizeLabel(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? T("Logs_Dashboard_NoDataValue", "Sin dato") : value.Trim();
        }

        private static bool ContainsText(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(search ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAllValue(string value)
        {
            return string.Equals(value ?? string.Empty, AllFilterValue, StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeDateFolder(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            DateTime parsed;
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
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

                DateTime timestamp;
                if (!DateTime.TryParse(parts[0], out timestamp))
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

        private sealed class PeriodBucket
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Label { get; set; }
        }

        private sealed class LatencyBand
        {
            public LatencyBand(string label, long minimumInclusive, long maximumExclusive)
            {
                Label = label;
                MinimumInclusive = minimumInclusive;
                MaximumExclusive = maximumExclusive;
            }

            public string Label { get; private set; }
            public long MinimumInclusive { get; private set; }
            public long MaximumExclusive { get; private set; }
        }

        private static OperationalHealthSnapshot BuildOperationalHealthSnapshot(
            IReadOnlyList<AppLogEntry> baseEntries,
            IReadOnlyList<AppLogEntry> filteredEntries)
        {
            var safeBaseEntries = baseEntries ?? Array.Empty<AppLogEntry>();
            var safeFilteredEntries = filteredEntries ?? Array.Empty<AppLogEntry>();
            var healthEntries = safeBaseEntries
                .Where(IsHealthSignal)
                .OrderByDescending(x => x.Timestamp)
                .ToList();
            var latestActivity = safeBaseEntries.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            var latestHealth = healthEntries.FirstOrDefault();
            var latestHeartbeat = healthEntries.FirstOrDefault(IsHeartbeatSignal);
            var latestDependencies = healthEntries
                .Where(IsDependencySignal)
                .GroupBy(ResolveDependencyName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(x => x.Timestamp).First())
                .OrderBy(x => ResolveDependencyName(x), StringComparer.OrdinalIgnoreCase)
                .ToList();
            var degradedDependencies = latestDependencies.Count(x => IsOperationalDegradation(x) || IsHealthIncident(x));
            var recentHealthIncidents = healthEntries
                .Where(IsHealthIncident)
                .Where(x => x.Timestamp >= DateTime.Now.AddDays(-7))
                .Take(8)
                .ToList();
            var lastRelevantEntry = latestHealth ?? latestActivity;
            var heartbeatIntervalMinutesText = GetSemanticValue(latestHeartbeat, "interval_minutes");
            double heartbeatIntervalMinutes;
            if (!double.TryParse(heartbeatIntervalMinutesText, NumberStyles.Any, CultureInfo.InvariantCulture, out heartbeatIntervalMinutes))
                heartbeatIntervalMinutes = 5;

            var lastHeartbeatAge = latestHeartbeat == null
                ? (TimeSpan?)null
                : DateTime.Now - latestHeartbeat.Timestamp;
            var heartbeatStatusLevel = ResolveHeartbeatStatusLevel(latestHeartbeat, lastHeartbeatAge, heartbeatIntervalMinutes);
            var heartbeatState = ResolveHealthState(latestHeartbeat);
            var dependencyStatusLevel = ResolveDependencyStatusLevel(latestDependencies, degradedDependencies);
            var dependencySummary = latestDependencies.Count == 0
                ? T("Logs_Dashboard_NoDependenciesInBaseWindow", "Sin dependencias observables en la ventana base.")
                : string.Join(" | ", latestDependencies.Select(x =>
                    NormalizeLabel(ResolveDependencyName(x)) + ": " + ResolveDependencyStateLabel(ResolveHealthState(x), GetSemanticValue(x, "sink"))));

            return new OperationalHealthSnapshot
            {
                LastActivityLabel = lastRelevantEntry == null
                    ? T("Logs_Dashboard_NoRecentSignal", "Sin senal reciente")
                    : lastRelevantEntry.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                LastActivityDetail = lastRelevantEntry == null
                    ? T("Logs_Dashboard_NoActivityInBaseRange", "No hay actividad visible en el rango base.")
                    : latestHealth == null
                        ? T("Logs_Dashboard_LastActivityFromGeneralLogs", "Ultima actividad observable desde logs generales; aun no hay evento health reciente.")
                        : NormalizeLabel(lastRelevantEntry.Message),
                HeartbeatStatusLevel = heartbeatStatusLevel,
                HeartbeatLabel = latestHeartbeat == null
                    ? T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable")
                    : BuildHeartbeatLabel(lastHeartbeatAge, heartbeatState),
                HeartbeatDetail = latestHeartbeat == null
                    ? T("Logs_Dashboard_HeartbeatReportsOnlyWhileAlive", "La app solo puede reportar heartbeat mientras esta viva; no se infiere ausencia como caida contractual.")
                    : F("Logs_Dashboard_LastHeartbeatFormat", "Ultimo heartbeat {0}. Intervalo esperado: {1} min.", FormatRelativeTime(lastHeartbeatAge), heartbeatIntervalMinutes.ToString("N0", CultureInfo.InvariantCulture)),
                DependencyStatusLevel = dependencyStatusLevel,
                DependencyLabel = latestDependencies.Count == 0
                    ? T("Logs_Dashboard_NoCoverage", "Sin cobertura")
                    : F("Logs_Dashboard_ObservableDependenciesFormat", "{0} observables / {1} degradadas", latestDependencies.Count, degradedDependencies),
                DependencyDetail = dependencySummary,
                RecentIncidentCount = recentHealthIncidents.Count,
                RecentIncidentLabel = recentHealthIncidents.Count == 0
                    ? T("Logs_Dashboard_NoRecentHealthIncidents", "Sin incidentes health recientes")
                    : recentHealthIncidents.Count == 1
                        ? T("Logs_Dashboard_OneHealthIncidentIn7Days", "1 incidente health en 7 dias")
                        : F("Logs_Dashboard_HealthIncidentsIn7DaysFormat", "{0} incidentes health en 7 dias", recentHealthIncidents.Count.ToString("N0")),
                RecentIncidentDetail = recentHealthIncidents.Count == 0
                    ? T("Logs_Dashboard_NoHealthFailuresInBaseWindow", "No hay degradaciones o fallas de health visibles en la ventana base.")
                    : string.Join(" | ", recentHealthIncidents.Select(x => x.Timestamp.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture) + " " + NormalizeLabel(x.Message))),
                HasObservableHealth = healthEntries.Count > 0,
                HasObservableHeartbeat = latestHeartbeat != null,
                HasObservableDependencies = latestDependencies.Count > 0,
                HasDegradation = degradedDependencies > 0 ||
                                 recentHealthIncidents.Count > 0 ||
                                 string.Equals(heartbeatStatusLevel, "review", StringComparison.OrdinalIgnoreCase),
                HasSevereDegradation = latestDependencies.Any(x => string.Equals(ResolveHealthState(x), "unhealthy", StringComparison.OrdinalIgnoreCase) || string.Equals(ResolveHealthState(x), "error", StringComparison.OrdinalIgnoreCase)) ||
                                     recentHealthIncidents.Any(x => string.Equals(ResolveHealthState(x), "unhealthy", StringComparison.OrdinalIgnoreCase) || string.Equals(ResolveHealthState(x), "error", StringComparison.OrdinalIgnoreCase)),
                VisibleIncidentCount = safeFilteredEntries.Count(IsHealthIncident),
                VisibleDegradationCount = safeFilteredEntries.Count(IsOperationalDegradation)
            };
        }

        private static string ResolveHeartbeatStatusLevel(AppLogEntry latestHeartbeat, TimeSpan? lastHeartbeatAge, double heartbeatIntervalMinutes)
        {
            if (latestHeartbeat == null)
                return "limited";

            var state = ResolveHealthState(latestHeartbeat);
            if (string.Equals(state, "unhealthy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
            {
                return "attention";
            }

            if (string.Equals(state, "degraded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "partial", StringComparison.OrdinalIgnoreCase))
            {
                return "review";
            }

            var thresholdMinutes = Math.Max(heartbeatIntervalMinutes * 2d, 15d);
            if ((lastHeartbeatAge ?? TimeSpan.MaxValue).TotalMinutes > thresholdMinutes)
                return "review";

            return "stable";
        }

        private static string ResolveDependencyStatusLevel(IReadOnlyList<AppLogEntry> latestDependencies, int degradedDependencies)
        {
            if ((latestDependencies ?? Array.Empty<AppLogEntry>()).Count == 0)
                return "limited";

            if (latestDependencies.Any(x => string.Equals(ResolveHealthState(x), "unhealthy", StringComparison.OrdinalIgnoreCase) || string.Equals(ResolveHealthState(x), "error", StringComparison.OrdinalIgnoreCase)))
                return "attention";

            if (degradedDependencies > 0)
                return "review";

            return "stable";
        }

        private static string BuildHeartbeatLabel(TimeSpan? age, string state)
        {
            if (age == null)
                return T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable");

            if (string.Equals(state, "stopped", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_StoppedPrefix", "Finalizado") + " " + FormatRelativeTime(age);

            if (string.Equals(state, "degraded", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "partial", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_DegradedPrefix", "Degradado") + " " + FormatRelativeTime(age);

            if (string.Equals(state, "unhealthy", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_FailedPrefix", "Con falla") + " " + FormatRelativeTime(age);

            return T("Logs_Dashboard_ActivePrefix", "Activo") + " " + FormatRelativeTime(age);
        }

        private static string ResolveDependencyStateLabel(string state, string sink)
        {
            if (string.Equals(state, "degraded", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(sink)
                    ? T("Logs_Dashboard_DegradedState", "degradada")
                    : T("Logs_Dashboard_DegradedSinkPrefix", "degradada (sink ") + sink + ")";
            }

            if (string.Equals(state, "unhealthy", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_ErrorState", "con error");

            if (string.Equals(state, "healthy", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_HealthyState", "saludable");

            return string.IsNullOrWhiteSpace(state) ? T("Logs_Dashboard_NoStateShort", "sin estado") : state;
        }

        private static string FormatRelativeTime(TimeSpan? age)
        {
            if (age == null)
                return T("Logs_Dashboard_NoReferenceShort", "Sin referencia");

            if (age.Value.TotalMinutes < 1)
                return T("Logs_Dashboard_LessThanOneMinuteAgo", "hace < 1 min");

            if (age.Value.TotalHours < 1)
                return F(
                    "Logs_Dashboard_MinutesAgoFormat",
                    "hace {0} min",
                    Math.Round(age.Value.TotalMinutes).ToString("N0", CultureInfo.CurrentCulture));

            if (age.Value.TotalDays < 1)
                return F(
                    "Logs_Dashboard_HoursAgoFormat",
                    "hace {0} h",
                    Math.Round(age.Value.TotalHours).ToString("N0", CultureInfo.CurrentCulture));

            return F(
                "Logs_Dashboard_DaysAgoFormat",
                "hace {0} d",
                Math.Round(age.Value.TotalDays).ToString("N0", CultureInfo.CurrentCulture));
        }

        private static LogExecutiveStatus BuildExecutiveStatus(
            IReadOnlyList<AppLogEntry> entries,
            LogDashboardSummary summary,
            string rangeKey,
            string instrumentationStatus,
            IReadOnlyList<AppLogEntry> baseEntries,
            OperationalHealthSnapshot operationalHealth)
        {
            var safeSummary = summary ?? new LogDashboardSummary();
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var totalCoverageSignals = 0;

            if ((baseEntries ?? Array.Empty<AppLogEntry>()).Any(IsHealthSignal))
                totalCoverageSignals++;

            if ((baseEntries ?? Array.Empty<AppLogEntry>()).Any(IsHeartbeatSignal))
                totalCoverageSignals++;

            if ((baseEntries ?? Array.Empty<AppLogEntry>()).Any(IsDependencySignal))
                totalCoverageSignals++;

            if ((baseEntries ?? Array.Empty<AppLogEntry>()).Any(IsSessionSignal))
                totalCoverageSignals++;

            if ((baseEntries ?? Array.Empty<AppLogEntry>()).Any(IsValidationSignal))
                totalCoverageSignals++;

            var healthSnapshot = operationalHealth ?? new OperationalHealthSnapshot();
            var statusLevel = ResolveExecutiveStatusLevel(safeEntries, safeSummary, totalCoverageSignals, healthSnapshot);
            var summaryText = BuildExecutiveSummaryText(safeEntries, safeSummary, totalCoverageSignals, healthSnapshot);
            var coverageText = F(
                "Logs_Dashboard_ObservableCoverageFormat",
                "Cobertura observable: {0}/5 senales base (health, heartbeat, dependencias, sesion, validacion).",
                totalCoverageSignals);

            return new LogExecutiveStatus
            {
                StatusLevel = statusLevel,
                StatusLabel = ResolveStatusLabel(statusLevel),
                Title = T("Logs_Dashboard_ExecutiveStatusTitle", "Estado ejecutivo del rango filtrado"),
                Summary = summaryText,
                Detail = string.IsNullOrWhiteSpace(instrumentationStatus)
                    ? T("Logs_Dashboard_ExecutiveSummaryRegisteredEventsOnly", "Resumen basado solo en eventos registrados; no representa uptime ni disponibilidad contractual.")
                    : instrumentationStatus,
                WindowLabel = ResolveRangeLabel(rangeKey),
                CoverageLabel = coverageText,
                LifeSignLabel = healthSnapshot.LastActivityLabel ?? T("Logs_Dashboard_NoRecentSignal", "Sin senal reciente"),
                LifeSignDetail = healthSnapshot.LastActivityDetail ?? T("Logs_Dashboard_NoHealthActivityObservable", "Sin actividad health observable."),
                HeartbeatLabel = healthSnapshot.HeartbeatLabel ?? T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable"),
                HeartbeatDetail = healthSnapshot.HeartbeatDetail ?? T("Logs_Dashboard_NoHeartbeatInBaseWindow", "Sin heartbeat en la ventana base."),
                DependencyLabel = healthSnapshot.DependencyLabel ?? T("Logs_Dashboard_NoCoverage", "Sin cobertura"),
                DependencyDetail = healthSnapshot.DependencyDetail ?? T("Logs_Dashboard_NoDependenciesObservable", "Sin dependencias observables."),
                IncidentLabel = healthSnapshot.RecentIncidentLabel ?? T("Logs_Dashboard_NoRecentHealthIncidents", "Sin incidentes health recientes"),
                IncidentDetail = healthSnapshot.RecentIncidentDetail ?? T("Logs_Dashboard_NoVisibleHealthDegradations", "Sin degradaciones health visibles.")
            };
        }

        private static IReadOnlyList<LogDashboardSection> BuildStatusSections(
            IReadOnlyList<AppLogEntry> entries,
            LogDashboardSummary summary,
            IReadOnlyList<PeriodBucket> periodBuckets,
            IReadOnlyList<LogMetricDistributionItem> sourceActivity,
            IReadOnlyList<LogMetricDistributionItem> userActivity,
            IReadOnlyList<LogMetricDistributionItem> machineActivity,
            IReadOnlyList<LogMetricDistributionItem> latencyDistribution,
            IReadOnlyList<AppLogEntry> baseEntries,
            string instrumentationStatus,
            OperationalHealthSnapshot operationalHealth)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var safeSummary = summary ?? new LogDashboardSummary();
            var healthSnapshot = operationalHealth ?? new OperationalHealthSnapshot();
            var validations = safeEntries.Where(IsValidationSignal).ToList();
            var rejectedValidations = validations.Count(IsRejectedValidation);
            var acceptedValidations = Math.Max(validations.Count - rejectedValidations, 0);
            var slowOperations = safeEntries.Count(x => (x.DurationMs ?? 0L) >= 1000);
            var incidentSeries = BuildPeriodSeries(safeEntries, periodBuckets, IsCriticalEvent);
            var validationSeries = BuildPeriodSeries(safeEntries, periodBuckets, IsValidationSignal);
            var latencySeries = BuildPeriodSeries(safeEntries, periodBuckets, x => (x.DurationMs ?? 0L) >= 1000);
            var activitySeries = BuildPeriodSeries(safeEntries, periodBuckets, x => true);
            var instrumentationSeries = BuildPeriodSeries(safeEntries, periodBuckets, x =>
                IsHealthIncident(x) || IsOperationalDegradation(x) || IsDependencySignal(x) || IsHeartbeatSignal(x));
            var incidentSegments = BuildNarrativeSegments("incidents", periodBuckets, baseEntries, x => IsCriticalEvent(x));
            var validationSegments = BuildNarrativeSegments("validations", periodBuckets, safeEntries, x => IsValidationSignal(x));
            var latencySegments = BuildNarrativeSegments("latency", periodBuckets, safeEntries, x => (x.DurationMs ?? 0L) >= 1000 || x.DurationMs.HasValue);
            var activitySegments = BuildNarrativeSegments("activity", periodBuckets, safeEntries, x => true);
            var healthSegments = BuildNarrativeSegments("health", periodBuckets, baseEntries, x => IsHealthSignal(x));

            var sections = new List<LogDashboardSection>();

            sections.Add(new LogDashboardSection
            {
                SectionKey = "incidents",
                Title = T("Logs_Dashboard_IncidentsSectionTitle", "Errores e incidentes"),
                StatusLevel = ResolveIncidentStatusLevel(safeSummary.ErrorCount, safeSummary.WarningCount, safeEntries.Count),
                StatusLabel = ResolveStatusLabel(ResolveIncidentStatusLevel(safeSummary.ErrorCount, safeSummary.WarningCount, safeEntries.Count)),
                Summary = safeSummary.ErrorCount > 0
                    ? F("Logs_Dashboard_IncidentsSummaryVisible", "Se observan {0} errores y {1} advertencias en la ventana visible.", safeSummary.ErrorCount, safeSummary.WarningCount)
                    : (safeEntries.Count == 0
                        ? T("Logs_Dashboard_NoVisibleEventsInWindow", "Sin eventos en la ventana visible.")
                        : T("Logs_Dashboard_NoVisibleErrorsWarningsLead", "No hay errores visibles; las advertencias siguen siendo la principal senal de incidente.")),
                Detail = safeSummary.LatestCriticalEvent == null
                    ? T("Logs_Dashboard_IncidentsDetailRealEventsOnly", "La lectura usa eventos reales capturados; no infiere disponibilidad ni incidentes inexistentes.")
                    : F(
                        "Logs_Dashboard_LastVisibleIncidentFormat",
                        "Ultimo incidente visible: {0:yyyy-MM-dd HH:mm} | {1} | {2}",
                        safeSummary.LatestCriticalEvent.Timestamp,
                        NormalizeLabel(safeSummary.LatestCriticalEvent.Source),
                        NormalizeLabel(safeSummary.LatestCriticalEvent.Message)),
                TimelineSegments = incidentSegments,
                TrendCells = BuildTrendCellsFromSegments(incidentSegments, incidentSeries, "incidentes visibles"),
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_ErrorsMetric", "Errores"), Value = safeSummary.ErrorCount.ToString("N0"), Hint = T("Logs_Dashboard_ErrorFactHint", "Eventos ERROR dentro del filtro actual.") },
                    new LogStatusFact { Label = T("Logs_WarningsMetric", "Advertencias"), Value = safeSummary.WarningCount.ToString("N0"), Hint = T("Logs_Dashboard_WarningFactHint", "Eventos WARNING que requieren seguimiento.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_LastEventFactLabel", "Ultimo evento"), Value = safeSummary.LatestCriticalEvent == null ? T("Logs_Dashboard_NoRecentIncident", "Sin incidente reciente") : NormalizeLabel(safeSummary.LatestCriticalEvent.Source), Hint = safeSummary.LatestCriticalEvent == null ? T("Logs_Dashboard_NoVisibleCriticalEvents", "No hay eventos criticos visibles.") : NormalizeLabel(safeSummary.LatestCriticalEvent.Message) }
                }
            });

            sections.Add(new LogDashboardSection
            {
                SectionKey = "validations",
                Title = T("Logs_Dashboard_ValidationsSectionTitle", "Validaciones y calidad"),
                StatusLevel = ResolveValidationStatusLevel(validations.Count, rejectedValidations, safeEntries.Count),
                StatusLabel = ResolveStatusLabel(ResolveValidationStatusLevel(validations.Count, rejectedValidations, safeEntries.Count)),
                Summary = validations.Count == 0
                    ? T("Logs_Dashboard_ValidationCoveragePartial", "La cobertura de validaciones sigue siendo parcial; no hay suficiente senal explotable para calidad end-to-end.")
                    : F("Logs_Dashboard_ValidationSummaryFormat", "Se registran {0} validaciones, con {1} rechazadas y {2} aceptadas.", validations.Count, rejectedValidations, acceptedValidations),
                Detail = validations.Count == 0
                    ? T("Logs_Dashboard_ValidationGapDetail", "La pagina muestra el gap de instrumentacion en lugar de fabricar un score de calidad.")
                    : T("Logs_Dashboard_ValidationEventsOnlyDetail", "Las validaciones se basan solo en eventos VALIDATION o reglas detectables en detalles."),
                TimelineSegments = validationSegments,
                TrendCells = BuildTrendCellsFromSegments(validationSegments, validationSeries, "validaciones observables"),
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_RejectedValidationsLabel", "Rechazadas"), Value = rejectedValidations.ToString("N0"), Hint = T("Logs_Dashboard_RejectedValidationsHint", "Validaciones con rechazo semantico visible en mensaje o detalle.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_AcceptedValidationsLabel", "Aceptadas"), Value = acceptedValidations.ToString("N0"), Hint = T("Logs_Dashboard_AcceptedValidationsHint", "Validaciones aceptadas dentro del filtro actual.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_CoverageTitle", "Cobertura"), Value = validations.Count == 0 ? T("Logs_Dashboard_PartialShort", "Parcial") : T("Logs_Dashboard_ActiveShort", "Activa"), Hint = T("Logs_Dashboard_ValidationCoverageHint", "No todas las reglas de negocio estan instrumentadas todavia.") }
                }
            });

            sections.Add(new LogDashboardSection
            {
                SectionKey = "latency",
                Title = T("Logs_Dashboard_LatencySectionTitle", "Latencia y rendimiento"),
                StatusLevel = ResolveLatencyStatusLevel(latencyDistribution, slowOperations),
                StatusLabel = ResolveStatusLabel(ResolveLatencyStatusLevel(latencyDistribution, slowOperations)),
                Summary = !safeEntries.Any(x => x.DurationMs.HasValue)
                    ? T("Logs_Dashboard_LatencyTelemetryIncomplete", "La telemetria de latencia sigue incompleta; solo se muestra rendimiento donde la app registra duraciones.")
                    : F("Logs_Dashboard_LatencySummaryFormat", "Latencia promedio {0} y P95 {1}; {2} operaciones superan 1 s.", FormatLatency(safeSummary.AverageLatencyMs), FormatLatency(safeSummary.P95LatencyMs), slowOperations),
                Detail = T("Logs_Dashboard_LatencyDurationOnlyDetail", "Las bandas de rendimiento reflejan solo eventos con DurationMs; no cubren dependencias sin instrumentacion."),
                TimelineSegments = latencySegments,
                TrendCells = BuildTrendCellsFromSegments(latencySegments, latencySeries, "operaciones lentas (>= 1 s)"),
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_AverageLabel", "Promedio"), Value = FormatLatency(safeSummary.AverageLatencyMs), Hint = T("Logs_Dashboard_AverageLatencyHint", "Promedio de eventos con latencia registrada.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_P95Label", "P95"), Value = FormatLatency(safeSummary.P95LatencyMs), Hint = T("Logs_Dashboard_P95LatencyHint", "Percentil 95 del subconjunto con DurationMs.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_OneSecondPlusLabel", ">= 1 s"), Value = slowOperations.ToString("N0"), Hint = T("Logs_Dashboard_SlowOperationsHint", "Volumen de operaciones lentas dentro del rango visible.") }
                }
            });

            sections.Add(new LogDashboardSection
            {
                SectionKey = "activity",
                Title = T("Logs_Dashboard_ActivitySectionTitle", "Actividad por modulo, usuario y equipo"),
                StatusLevel = safeEntries.Count == 0 ? "limited" : "stable",
                StatusLabel = ResolveStatusLabel(safeEntries.Count == 0 ? "limited" : "stable"),
                Summary = safeEntries.Count == 0
                    ? T("Logs_Dashboard_NoActivityToSummarize", "No hay actividad visible para resumir en esta ventana.")
                    : T("Logs_Dashboard_RealActivitySummary", "Se prioriza actividad real por modulo, usuario y equipo fisico; la taxonomia por team aun no existe en logs."),
                Detail = T("Logs_Dashboard_TeamNotInstrumentedDetail", "El concepto team no esta instrumentado. Se conserva drill-down real a modulo, usuario y equipo desde las distribuciones inferiores."),
                TimelineSegments = activitySegments,
                TrendCells = BuildTrendCellsFromSegments(activitySegments, activitySeries, "eventos observables"),
                Facts = new[]
                {
                    BuildTopActivityFact("Modulo lider", sourceActivity, "No hay modulo dominante visible."),
                    BuildTopActivityFact("Usuario lider", userActivity, "No hay usuario dominante visible."),
                    BuildTopActivityFact("Equipo fisico lider", machineActivity, "Se muestra equipo fisico; team de negocio sigue sin senal.")
                }
            });

            sections.Add(new LogDashboardSection
            {
                SectionKey = "health",
                Title = T("Logs_Dashboard_HealthSectionTitle", "Salud operativa y dependencias"),
                StatusLevel = ResolveOperationalHealthStatusLevel(healthSnapshot, baseEntries),
                StatusLabel = ResolveStatusLabel(ResolveOperationalHealthStatusLevel(healthSnapshot, baseEntries)),
                Summary = healthSnapshot.HasObservableHealth
                    ? F(
                        "Logs_Dashboard_HealthSummaryFormat",
                        "Ultima senal {0}. Heartbeat {1}. Dependencias: {2}.",
                        healthSnapshot.LastActivityLabel,
                        healthSnapshot.HeartbeatLabel,
                        healthSnapshot.DependencyLabel)
                    : instrumentationStatus,
                Detail = string.IsNullOrWhiteSpace(instrumentationStatus)
                    ? T("Logs_Dashboard_LocalHonestReadingDetail", "La lectura sigue siendo local y honesta: solo muestra lo que la app realmente observa desde desktop.")
                    : instrumentationStatus,
                TimelineSegments = healthSegments,
                TrendCells = BuildTrendCellsFromSegments(healthSegments, instrumentationSeries, "eventos health/dependencia visibles"),
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_LastSignalLabel", "Ultima senal"), Value = healthSnapshot.LastActivityLabel ?? T("Logs_Dashboard_NoRecentSignalShort", "Sin senal"), Hint = healthSnapshot.LastActivityDetail ?? T("Logs_Dashboard_NoHealthActivityObservable", "Sin actividad health observable.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_HeartbeatLabel", "Heartbeat"), Value = healthSnapshot.HeartbeatLabel ?? T("Logs_Dashboard_NoHeartbeatShort", "Sin heartbeat"), Hint = healthSnapshot.HeartbeatDetail ?? T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_DependenciesLabel", "Dependencias"), Value = healthSnapshot.DependencyLabel ?? T("Logs_Dashboard_NoCoverage", "Sin cobertura"), Hint = healthSnapshot.DependencyDetail ?? T("Logs_Dashboard_NoDependenciesObservable", "Sin dependencias observables.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_DegradationLabel", "Degradacion"), Value = healthSnapshot.VisibleDegradationCount <= 0 ? T("Logs_Dashboard_NoDegradation", "Sin degradacion") : healthSnapshot.VisibleDegradationCount.ToString("N0") + " " + T("Logs_Dashboard_VisibleShort", "visible"), Hint = healthSnapshot.HasDegradation ? healthSnapshot.DependencyDetail : T("Logs_Dashboard_NoOperationalDegradationVisible", "No hay degradacion operativa visible en la ventana.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_IncidentsLabel", "Incidentes"), Value = healthSnapshot.RecentIncidentLabel ?? T("Logs_Dashboard_NoIncidentsShort", "Sin incidentes"), Hint = healthSnapshot.RecentIncidentDetail ?? T("Logs_Dashboard_NoHealthIncidentsVisible", "Sin incidentes health visibles.") }
                }
            });

            return sections;
        }

        private static IReadOnlyList<LogStatusTrendCell> BuildTrendCells(
            IReadOnlyList<LogMetricPoint> series,
            Func<LogMetricPoint, string> statusSelector,
            string tooltipSuffix)
        {
            return (series ?? Array.Empty<LogMetricPoint>())
                .Select(point => new LogStatusTrendCell
                {
                    Label = point.Label,
                    Count = point.Count,
                    StatusLevel = statusSelector == null ? "limited" : statusSelector(point),
                    Tooltip = string.Format("{0}: {1} {2}", point.Label, point.Count, tooltipSuffix)
                })
                .ToList();
        }

        private static IReadOnlyList<LogStatusTrendCell> BuildTrendCellsFromSegments(
            IReadOnlyList<LogStatusTimelineSegment> segments,
            IReadOnlyList<LogMetricPoint> fallbackSeries,
            string tooltipSuffix)
        {
            var safeSegments = segments ?? Array.Empty<LogStatusTimelineSegment>();
            if (safeSegments.Count > 0)
            {
                return safeSegments
                    .Select(segment => new LogStatusTrendCell
                    {
                        Label = segment.BucketLabel,
                        Count = segment.ObservableCount,
                        StatusLevel = string.IsNullOrWhiteSpace(segment.StatusLevel) ? "limited" : segment.StatusLevel,
                        Tooltip = string.IsNullOrWhiteSpace(segment.Explanation)
                            ? string.Format("{0}: {1} {2}", segment.BucketLabel, segment.ObservableCount, tooltipSuffix)
                            : segment.Explanation
                    })
                    .ToList();
            }

            return BuildTrendCells(fallbackSeries, x => ResolveCountStatusLevel(x.Count, 1, 3), tooltipSuffix);
        }

        private static IReadOnlyList<LogStatusTimelineSegment> BuildNarrativeSegments(
            string sectionKey,
            IReadOnlyList<PeriodBucket> periodBuckets,
            IReadOnlyList<AppLogEntry> coverageEntries,
            Func<AppLogEntry, bool> sectionPredicate)
        {
            var safeCoverageEntries = coverageEntries ?? Array.Empty<AppLogEntry>();
            var segments = new List<LogStatusTimelineSegment>();

            foreach (var bucket in periodBuckets ?? Array.Empty<PeriodBucket>())
            {
                var bucketCoverage = safeCoverageEntries
                    .Where(x => x.Timestamp >= bucket.Start && x.Timestamp < bucket.End)
                    .OrderBy(x => x.Timestamp)
                    .ToList();
                var sectionEntries = bucketCoverage
                    .Where(x => sectionPredicate == null || sectionPredicate(x))
                    .OrderBy(x => x.Timestamp)
                    .ToList();
                var classification = ClassifyNarrative(sectionKey, bucketCoverage, sectionEntries, safeCoverageEntries);

                segments.Add(new LogStatusTimelineSegment
                {
                    SegmentId = BuildSegmentId(sectionKey, bucket, classification),
                    SectionKey = sectionKey,
                    BucketLabel = bucket.Label,
                    Start = bucket.Start,
                    End = bucket.End,
                    StatusLevel = classification.StatusLevel,
                    NarrativeState = classification.NarrativeState,
                    Severity = classification.Severity,
                    Summary = classification.Summary,
                    Explanation = classification.Explanation,
                    PrimarySource = classification.PrimarySource,
                    PrimaryState = classification.PrimaryState,
                    DependencyName = classification.DependencyName,
                    IncidentStart = classification.IncidentStart,
                    IncidentEnd = classification.IncidentEnd,
                    DurationLabel = classification.DurationLabel,
                    RecoveryState = classification.RecoveryState,
                    CoverageState = classification.CoverageState,
                    ObservableCount = classification.ObservableCount,
                    Detail = new LogStatusSegmentDetail
                    {
                        TooltipTitle = classification.TooltipTitle,
                        TooltipBody = classification.TooltipBody,
                        Facts = classification.Facts,
                        RelatedEventIdsOrLabels = classification.RelatedLabels
                    },
                    DrillDown = new LogSegmentDrillDownContext
                    {
                        SectionKey = sectionKey,
                        RangeStart = bucket.Start,
                        RangeEnd = bucket.End,
                        Severity = classification.Severity,
                        Source = classification.PrimarySource,
                        MachineName = classification.MachineName,
                        UserName = classification.UserName,
                        SearchText = classification.SearchText,
                        Dependency = classification.DependencyName,
                        State = classification.PrimaryState,
                        PrimaryEventType = classification.PrimaryEventType
                    }
                });
            }

            return segments;
        }

        private static NarrativeClassification ClassifyNarrative(
            string sectionKey,
            IReadOnlyList<AppLogEntry> bucketCoverage,
            IReadOnlyList<AppLogEntry> sectionEntries,
            IReadOnlyList<AppLogEntry> allCoverageEntries)
        {
            var safeCoverage = bucketCoverage ?? Array.Empty<AppLogEntry>();
            var safeSectionEntries = sectionEntries ?? Array.Empty<AppLogEntry>();
            var primaryEntry = SelectPrimaryNarrativeEntry(safeSectionEntries);
            var hasCoverage = safeCoverage.Count > 0;
            var hasSectionEntries = safeSectionEntries.Count > 0;
            var hasPartialCoverage = ResolvePartialCoverage(sectionKey, safeCoverage, safeSectionEntries);

            if (!hasCoverage)
            {
                return BuildCoverageOnlyNarrative(sectionKey, "limited", "NoData", "None", "INFO",
                    T("Logs_Dashboard_NoObservableDataSummary", "Sin datos observables"),
                    T("Logs_Dashboard_NoObservableDataExplanation", "No hay señales reales en este período, así que el dashboard no inventa salud ni recuperación."));
            }

            if (!hasSectionEntries)
            {
                if (hasPartialCoverage)
                {
                    return BuildCoverageOnlyNarrative(sectionKey, "review", "PartialCoverage", "Partial", "INFO",
                        T("Logs_Dashboard_PartialCoverageSummary", "Cobertura parcial en el período"),
                        BuildPartialCoverageExplanation(sectionKey, safeCoverage));
                }

                return BuildCoverageOnlyNarrative(sectionKey, "stable", "NoIncidentObserved", "Active", "INFO",
                    ResolveNoIncidentSummary(sectionKey),
                    ResolveNoIncidentExplanation(sectionKey, safeCoverage));
            }

            var recovery = ResolveRecovery(sectionKey, primaryEntry, allCoverageEntries);
            var severity = ResolveNarrativeSeverity(primaryEntry);
            var state = ResolveNarrativeState(sectionKey, primaryEntry, recovery);
            var statusLevel = ResolveNarrativeStatusLevel(state, hasPartialCoverage);
            var durationLabel = BuildDurationLabel(primaryEntry, recovery);
            var summary = BuildNarrativeSummary(sectionKey, primaryEntry, severity, state, safeSectionEntries.Count);
            var explanation = BuildNarrativeExplanation(sectionKey, primaryEntry, recovery, safeSectionEntries.Count, hasPartialCoverage);
            var dependency = ResolveDependencyName(primaryEntry);
            var primaryState = ResolvePrimaryState(primaryEntry);
            var primarySource = NormalizeLabel(primaryEntry?.Source);
            var tooltipTitle = bucketCoverage.FirstOrDefault() == null
                ? summary
                : string.Format("{0} · {1}", summary, primaryEntry.Timestamp.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture));
            var facts = BuildNarrativeFacts(primaryEntry, severity, state, recovery, hasPartialCoverage, safeSectionEntries.Count);

            return new NarrativeClassification
            {
                StatusLevel = statusLevel,
                NarrativeState = state,
                CoverageState = hasPartialCoverage ? "Partial" : "Active",
                RecoveryState = recovery.State,
                Severity = severity,
                Summary = summary,
                Explanation = explanation,
                TooltipTitle = tooltipTitle,
                TooltipBody = explanation,
                PrimarySource = primarySource,
                PrimaryState = primaryState,
                DependencyName = dependency,
                IncidentStart = primaryEntry.Timestamp,
                IncidentEnd = recovery.Timestamp,
                DurationLabel = durationLabel,
                ObservableCount = safeSectionEntries.Count,
                Facts = facts,
                RelatedLabels = safeSectionEntries
                    .OrderByDescending(x => x.Timestamp)
                    .Take(3)
                    .Select(x => string.Format("{0:dd MMM HH:mm} · {1}", x.Timestamp, NormalizeLabel(x.Message)))
                    .ToList(),
                MachineName = NormalizeLabel(primaryEntry.MachineName),
                UserName = NormalizeLabel(primaryEntry.UserName),
                SearchText = BuildSearchText(primaryEntry),
                PrimaryEventType = ResolvePrimaryEventType(primaryEntry)
            };
        }

        private static NarrativeClassification BuildCoverageOnlyNarrative(
            string sectionKey,
            string statusLevel,
            string narrativeState,
            string coverageState,
            string severity,
            string summary,
            string explanation)
        {
            return new NarrativeClassification
            {
                StatusLevel = statusLevel,
                NarrativeState = narrativeState,
                CoverageState = coverageState,
                RecoveryState = "NotApplicable",
                Severity = severity,
                Summary = summary,
                Explanation = explanation,
                TooltipTitle = summary,
                TooltipBody = explanation,
                PrimarySource = sectionKey,
                PrimaryState = narrativeState,
                DependencyName = string.Empty,
                IncidentStart = null,
                IncidentEnd = null,
                DurationLabel = narrativeState == "NoData"
                    ? T("Logs_Dashboard_NoCoverageObservable", "Sin cobertura observable")
                    : T("Logs_Dashboard_NoIncidentObservable", "Sin incidente observable"),
                ObservableCount = 0,
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_FactCoverage", "Cobertura"), Value = coverageState, Hint = explanation },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactReading", "Lectura"), Value = narrativeState, Hint = summary }
                },
                RelatedLabels = Array.Empty<string>(),
                MachineName = string.Empty,
                UserName = string.Empty,
                SearchText = string.Empty,
                PrimaryEventType = narrativeState
            };
        }

        private static AppLogEntry SelectPrimaryNarrativeEntry(IReadOnlyList<AppLogEntry> sectionEntries)
        {
            return (sectionEntries ?? Array.Empty<AppLogEntry>())
                .OrderByDescending(GetNarrativePriority)
                .ThenByDescending(x => x.Timestamp)
                .FirstOrDefault();
        }

        private static int GetNarrativePriority(AppLogEntry entry)
        {
            if (entry == null)
                return -1;

            if (IsHealthSignal(entry))
                return 700 + (IsHealthIncident(entry) ? 100 : 0);

            if (string.Equals(entry.Level, "ERROR", StringComparison.OrdinalIgnoreCase))
                return 600;

            if (string.Equals(entry.Level, "WARNING", StringComparison.OrdinalIgnoreCase))
                return 500;

            if (IsRejectedValidation(entry))
                return 450;

            if (IsValidationSignal(entry))
                return 425;

            if ((entry.DurationMs ?? 0L) >= 1000)
                return 400;

            if (entry.DurationMs.HasValue)
                return 300;

            return 100;
        }

        private static bool ResolvePartialCoverage(string sectionKey, IReadOnlyList<AppLogEntry> bucketCoverage, IReadOnlyList<AppLogEntry> sectionEntries)
        {
            if ((bucketCoverage ?? Array.Empty<AppLogEntry>()).Count == 0)
                return false;

            if ((sectionEntries ?? Array.Empty<AppLogEntry>()).Count > 0)
                return false;

            if (string.Equals(sectionKey, "validations", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(sectionKey, "latency", StringComparison.OrdinalIgnoreCase))
                return !(bucketCoverage ?? Array.Empty<AppLogEntry>()).Any(x => x.DurationMs.HasValue);

            if (string.Equals(sectionKey, "health", StringComparison.OrdinalIgnoreCase))
                return !(bucketCoverage ?? Array.Empty<AppLogEntry>()).Any(IsHealthSignal);

            return false;
        }

        private static string ResolveNarrativeSeverity(AppLogEntry primaryEntry)
        {
            if (primaryEntry == null)
                return "INFO";

            if (IsHealthSignal(primaryEntry))
                return "HEALTH";

            if (string.Equals(primaryEntry.Level, "VALIDATION", StringComparison.OrdinalIgnoreCase))
                return "VALIDATION";

            if ((primaryEntry.DurationMs ?? 0L) >= 1000)
                return "LATENCY";

            return NormalizeLabel(primaryEntry.Level).ToUpperInvariant();
        }

        private static string ResolveNarrativeState(string sectionKey, AppLogEntry primaryEntry, RecoveryResolution recovery)
        {
            if (primaryEntry == null)
                return "NoData";

            if (IsHealthSignal(primaryEntry))
            {
                if (IsHealthIncident(primaryEntry))
                    return recovery != null && recovery.IsObserved ? "HistoricalIncidentResolved" : "HistoricalIncidentUnresolved";

                if (IsOperationalDegradation(primaryEntry))
                    return "OperationalDegraded";

                return "NoIncidentObserved";
            }

            if (string.Equals(primaryEntry.Level, "ERROR", StringComparison.OrdinalIgnoreCase))
                return "OperationalUnhealthy";

            if (string.Equals(primaryEntry.Level, "WARNING", StringComparison.OrdinalIgnoreCase))
                return "OperationalDegraded";

            if (IsRejectedValidation(primaryEntry))
                return "ValidationRejected";

            if ((primaryEntry.DurationMs ?? 0L) >= 1000)
                return "LatencyDegraded";

            return string.Equals(sectionKey, "activity", StringComparison.OrdinalIgnoreCase)
                ? "NoIncidentObserved"
                : "OperationalDegraded";
        }

        private static string ResolveNarrativeStatusLevel(string narrativeState, bool hasPartialCoverage)
        {
            if (string.Equals(narrativeState, "NoData", StringComparison.OrdinalIgnoreCase))
                return "limited";

            if (string.Equals(narrativeState, "PartialCoverage", StringComparison.OrdinalIgnoreCase))
                return "review";

            if (string.Equals(narrativeState, "OperationalUnhealthy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(narrativeState, "ValidationRejected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(narrativeState, "HistoricalIncidentUnresolved", StringComparison.OrdinalIgnoreCase))
            {
                return "attention";
            }

            if (string.Equals(narrativeState, "OperationalDegraded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(narrativeState, "LatencyDegraded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(narrativeState, "HistoricalIncidentResolved", StringComparison.OrdinalIgnoreCase) ||
                hasPartialCoverage)
            {
                return "review";
            }

            return "stable";
        }

        private static RecoveryResolution ResolveRecovery(string sectionKey, AppLogEntry primaryEntry, IReadOnlyList<AppLogEntry> allCoverageEntries)
        {
            if (primaryEntry == null)
                return RecoveryResolution.NotApplicable();

            if (!IsHealthSignal(primaryEntry) &&
                !string.Equals(primaryEntry.Level, "ERROR", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(primaryEntry.Level, "WARNING", StringComparison.OrdinalIgnoreCase) &&
                !IsRejectedValidation(primaryEntry) &&
                (primaryEntry.DurationMs ?? 0L) < 1000)
            {
                return RecoveryResolution.NotApplicable();
            }

            var recoveryEntry = (allCoverageEntries ?? Array.Empty<AppLogEntry>())
                .Where(x => x.Timestamp >= primaryEntry.Timestamp)
                .OrderBy(x => x.Timestamp)
                .FirstOrDefault(x => IsRecoveryMatch(primaryEntry, x));

            if (recoveryEntry == null)
                return RecoveryResolution.NotObservable();

            return RecoveryResolution.Observed(recoveryEntry.Timestamp);
        }

        private static bool IsRecoveryMatch(AppLogEntry primaryEntry, AppLogEntry candidate)
        {
            if (primaryEntry == null || candidate == null)
                return false;

            if (candidate.Timestamp < primaryEntry.Timestamp)
                return false;

            if (IsHealthSignal(primaryEntry))
            {
                var primaryDependency = ResolveDependencyName(primaryEntry);
                var candidateDependency = ResolveDependencyName(candidate);
                if (!string.Equals(primaryDependency, candidateDependency, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(primaryDependency) &&
                    !string.IsNullOrWhiteSpace(candidateDependency))
                {
                    return false;
                }

                var candidateState = ResolveHealthState(candidate);
                return string.Equals(candidateState, "healthy", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(candidateState, "stopped", StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(candidate.Source, primaryEntry.Source, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(candidate.Level, primaryEntry.Level, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(candidate.Level, "ERROR", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildDurationLabel(AppLogEntry primaryEntry, RecoveryResolution recovery)
        {
            if (primaryEntry == null)
                return T("Logs_Dashboard_NoCoverageObservable", "Sin cobertura observable");

            if (recovery == null || !recovery.IsObserved || !recovery.Timestamp.HasValue)
                return T("Logs_Dashboard_RecoveryNotObservable", "Recuperación no observable");

            var duration = recovery.Timestamp.Value - primaryEntry.Timestamp;
            if (duration.TotalMinutes < 1)
                return T("Logs_Dashboard_ObservableDurationUnderMinute", "Duración observable < 1 min");

            if (duration.TotalHours < 1)
                return F(
                    "Logs_Dashboard_ObservableDurationMinutesFormat",
                    "Duración observable {0} min",
                    Math.Round(duration.TotalMinutes).ToString("N0", CultureInfo.InvariantCulture));

            return F(
                "Logs_Dashboard_ObservableDurationHoursFormat",
                "Duración observable {0} h",
                Math.Round(duration.TotalHours, 1).ToString("N1", CultureInfo.InvariantCulture));
        }

        private static string BuildNarrativeSummary(string sectionKey, AppLogEntry primaryEntry, string severity, string narrativeState, int count)
        {
            if (primaryEntry == null)
                return ResolveNoIncidentSummary(sectionKey);

            if (IsHealthSignal(primaryEntry))
            {
                var dependency = ResolveDependencyName(primaryEntry);
                var state = ResolvePrimaryState(primaryEntry);
                return F(
                    "Logs_Dashboard_HealthReportsStateFormat",
                    "{0} reporta {1}",
                    string.IsNullOrWhiteSpace(dependency) ? NormalizeLabel(primaryEntry.Source) : dependency,
                    NormalizeLabel(state));
            }

            if (string.Equals(severity, "LATENCY", StringComparison.OrdinalIgnoreCase))
                return F(
                    "Logs_Dashboard_SlowOperationDetectedFormat",
                    "Operación lenta detectada en {0}",
                    NormalizeLabel(primaryEntry.Source));

            if (string.Equals(severity, "VALIDATION", StringComparison.OrdinalIgnoreCase))
                return F(
                    "Logs_Dashboard_ValidationObservedFormat",
                    "Validación observada en {0}",
                    NormalizeLabel(primaryEntry.Source));

            return NormalizeLabel(primaryEntry.Message);
        }

        private static string BuildNarrativeExplanation(string sectionKey, AppLogEntry primaryEntry, RecoveryResolution recovery, int count, bool hasPartialCoverage)
        {
            if (primaryEntry == null)
                return ResolveNoIncidentExplanation(sectionKey, Array.Empty<AppLogEntry>());

            var explanation = new List<string>();
            explanation.Add(NormalizeLabel(primaryEntry.Message));

            if (!string.IsNullOrWhiteSpace(primaryEntry.Details))
                explanation.Add(T("Logs_Dashboard_ContextPrefix", "Contexto: ") + primaryEntry.Details.Trim());

            var state = ResolvePrimaryState(primaryEntry);
            if (!string.IsNullOrWhiteSpace(state))
                explanation.Add(T("Logs_Dashboard_AffectedStatePrefix", "Estado afectado: ") + state + ".");

            var dependency = ResolveDependencyName(primaryEntry);
            if (!string.IsNullOrWhiteSpace(dependency) && !string.Equals(dependency, "dependency", StringComparison.OrdinalIgnoreCase))
                explanation.Add(T("Logs_Dashboard_DependencyModulePrefix", "Dependencia/módulo: ") + dependency + ".");

            explanation.Add(T("Logs_Dashboard_PrimaryEventPrefix", "Evento principal: ") + primaryEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

            if (recovery != null && recovery.IsObserved && recovery.Timestamp.HasValue)
                explanation.Add(T("Logs_Dashboard_RecoveryObservedPrefix", "Recuperación observada: ") + recovery.Timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") + ".");
            else if (recovery != null && string.Equals(recovery.State, "NotObservable", StringComparison.OrdinalIgnoreCase))
                explanation.Add(T("Logs_Dashboard_RecoveryNotObservableWithTelemetry", "Recuperación no observable con la telemetría actual."));

            if (count > 1)
                explanation.Add(F("Logs_Dashboard_RelatedEventsForDrillDownFormat", "El bucket contiene {0} eventos relacionados para drill-down.", count.ToString("N0")));

            if (hasPartialCoverage)
                explanation.Add(T("Logs_Dashboard_PartialCoverageSilenceWarning", "La cobertura del período es parcial, así que no conviene sobreinterpretar silencio como salud."));

            return string.Join(" ", explanation.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static IReadOnlyList<LogStatusFact> BuildNarrativeFacts(
            AppLogEntry primaryEntry,
            string severity,
            string narrativeState,
            RecoveryResolution recovery,
            bool hasPartialCoverage,
            int observableCount)
        {
            return new[]
            {
                new LogStatusFact { Label = T("Logs_Dashboard_FactSeverity", "Severidad"), Value = NormalizeLabel(severity), Hint = T("Logs_Dashboard_FactSeverityBucketHint", "Clasificación dominante del bucket.") },
                new LogStatusFact { Label = T("Logs_Dashboard_FactState", "Estado"), Value = NormalizeLabel(narrativeState), Hint = T("Logs_Dashboard_FactNarrativeReadingHint", "Lectura narrativa derivada de señales reales.") },
                new LogStatusFact
                {
                    Label = T("Logs_Dashboard_FactRecovery", "Recuperación"),
                    Value = recovery?.State ?? "NotApplicable",
                    Hint = recovery?.Timestamp.HasValue == true
                        ? recovery.Timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : T("Logs_Dashboard_NoClosureObservable", "Sin cierre observable")
                },
                new LogStatusFact
                {
                    Label = T("Logs_Dashboard_FactCoverage", "Cobertura"),
                    Value = hasPartialCoverage ? "Partial" : "Active",
                    Hint = hasPartialCoverage
                        ? T("Logs_Dashboard_PartialCoverageWindowHint", "La ventana no cubre toda la señal esperada.")
                        : T("Logs_Dashboard_ObservableSignalsBucketHint", "Hay señales observables para este bucket.")
                },
                new LogStatusFact
                {
                    Label = T("Logs_Dashboard_FactEvents", "Eventos"),
                    Value = observableCount.ToString("N0"),
                    Hint = primaryEntry == null
                        ? T("Logs_Dashboard_NoEventsForFamily", "Sin eventos para esta familia.")
                        : NormalizeLabel(primaryEntry.Source)
                }
            };
        }

        private static string BuildSegmentId(string sectionKey, PeriodBucket bucket, NarrativeClassification classification)
        {
            return string.Join("|", new[]
            {
                NormalizeKey(sectionKey),
                bucket.Start.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                bucket.End.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                NormalizeKey(classification?.PrimaryEventType),
                NormalizeKey(classification?.PrimarySource),
                NormalizeKey(classification?.PrimaryState),
                NormalizeKey(classification?.DependencyName),
                NormalizeKey(classification?.Summary)
            });
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "na";

            var chars = value.Trim().ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .Take(48)
                .ToArray();
            return chars.Length == 0 ? "na" : new string(chars);
        }

        private static string ResolvePrimaryState(AppLogEntry entry)
        {
            if (entry == null)
                return string.Empty;

            if (IsHealthSignal(entry))
                return NormalizeLabel(ResolveHealthState(entry));

            var semanticState = GetSemanticValue(entry, "state");
            if (!string.IsNullOrWhiteSpace(semanticState))
                return NormalizeLabel(semanticState);

            return NormalizeLabel(entry.Level);
        }

        private static string ResolvePrimaryEventType(AppLogEntry entry)
        {
            if (entry == null)
                return "none";

            if (IsDependencySignal(entry))
                return "dependency";

            if (IsHeartbeatSignal(entry))
                return "heartbeat";

            if (IsStartupSignal(entry))
                return "startup";

            if (IsValidationSignal(entry))
                return "validation";

            if ((entry.DurationMs ?? 0L) >= 1000)
                return "latency";

            return NormalizeLabel(entry.Level).ToLowerInvariant();
        }

        private static string ResolveNoIncidentSummary(string sectionKey)
        {
            if (string.Equals(sectionKey, "activity", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_ActivityWithoutDominantIncident", "Actividad observable sin incidente dominante");

            return T("Logs_Dashboard_NoIncidentObservableInPeriod", "Sin incidente observable en el período");
        }

        private static string ResolveNoIncidentExplanation(string sectionKey, IReadOnlyList<AppLogEntry> coverageEntries)
        {
            if (string.Equals(sectionKey, "activity", StringComparison.OrdinalIgnoreCase))
                return F(
                    "Logs_Dashboard_ActivityEventsNoIncidentEscalationFormat",
                    "Hay {0} eventos observables en el bucket, pero ninguno requiere escalarse como incidente.",
                    (coverageEntries ?? Array.Empty<AppLogEntry>()).Count.ToString("N0"));

            return T("Logs_Dashboard_TelemetryNoAnomalyForFamily", "Hay telemetría en el período, pero no aparece una señal anómala para esta familia.");
        }

        private static string BuildPartialCoverageExplanation(string sectionKey, IReadOnlyList<AppLogEntry> coverageEntries)
        {
            if (string.Equals(sectionKey, "health", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_PartialCoverageHealthExplanation", "Hay actividad general en el bucket, pero no una señal health/dependency suficiente para afirmar salud operativa completa.");

            if (string.Equals(sectionKey, "latency", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_PartialCoverageLatencyExplanation", "Hay eventos en el bucket, pero sin duraciones suficientes para describir rendimiento completo.");

            if (string.Equals(sectionKey, "validations", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_PartialCoverageValidationsExplanation", "La app tuvo actividad en el bucket, pero no surgieron validaciones explotables para lectura de calidad.");

            return F(
                "Logs_Dashboard_PartialCoverageGenericFormat",
                "Se detectaron {0} eventos en el período, aunque la cobertura para esta familia sigue siendo parcial.",
                (coverageEntries ?? Array.Empty<AppLogEntry>()).Count.ToString("N0"));
        }

        private static string BuildSearchText(AppLogEntry primaryEntry)
        {
            if (primaryEntry == null)
                return string.Empty;

            var reason = GetSemanticValue(primaryEntry, "reason");
            if (!string.IsNullOrWhiteSpace(reason))
                return reason;

            return NormalizeLabel(primaryEntry.Message);
        }

        private static LogStatusFact BuildTopActivityFact(string label, IReadOnlyList<LogMetricDistributionItem> items, string fallbackHint)
        {
            var topItem = (items ?? Array.Empty<LogMetricDistributionItem>()).FirstOrDefault();
            if (topItem == null)
            {
                return new LogStatusFact
                {
                    Label = label,
                    Value = T("Logs_NoData", "Sin datos"),
                    Hint = fallbackHint
                };
            }

            return new LogStatusFact
            {
                Label = label,
                Value = topItem.Label,
                Hint = topItem.SecondaryText
            };
        }

        private static string ResolveExecutiveStatusLevel(
            IReadOnlyList<AppLogEntry> entries,
            LogDashboardSummary summary,
            int coverageSignals,
            OperationalHealthSnapshot operationalHealth)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var healthSnapshot = operationalHealth ?? new OperationalHealthSnapshot();

            if (safeEntries.Count == 0 && !healthSnapshot.HasObservableHealth)
                return "limited";

            if (healthSnapshot.HasSevereDegradation)
                return "attention";

            if ((summary?.ErrorCount ?? 0) > 0)
                return "attention";

            if ((summary?.WarningCount ?? 0) > 0 || healthSnapshot.HasDegradation || coverageSignals < 5)
                return coverageSignals <= 2 ? "limited" : "review";

            return "stable";
        }

        private static string BuildExecutiveSummaryText(
            IReadOnlyList<AppLogEntry> entries,
            LogDashboardSummary summary,
            int coverageSignals,
            OperationalHealthSnapshot operationalHealth)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var safeSummary = summary ?? new LogDashboardSummary();
            var healthSnapshot = operationalHealth ?? new OperationalHealthSnapshot();

            if (safeEntries.Count == 0 && !healthSnapshot.HasObservableHealth)
                return T(
                    "Logs_Dashboard_NoRecentSignalCurrentFilter",
                    "Sin señal reciente en el filtro actual; revisá rango, equipo o cobertura de instrumentación.");

            if (healthSnapshot.HasSevereDegradation)
            {
                return F(
                    "Logs_Dashboard_ExecutiveAttentionHealthFormat",
                    "Atención operativa: se detectan dependencias o salud en falla visible. Última señal {0}; cobertura observable {1}/5.",
                    healthSnapshot.LastActivityLabel ?? T("Logs_Dashboard_NoReferenceShort", "Sin referencia"),
                    coverageSignals);
            }

            if (safeSummary.ErrorCount > 0)
            {
                return F(
                    "Logs_Dashboard_ExecutiveAttentionErrorsFormat",
                    "Atención operativa: {0} errores visibles sobre {1} eventos, con cobertura observable {2}/5.",
                    safeSummary.ErrorCount,
                    safeSummary.TotalEvents,
                    coverageSignals);
            }

            if (safeSummary.WarningCount > 0)
            {
                return F(
                    "Logs_Dashboard_ExecutiveReviewWarningsFormat",
                    "Operación bajo revisión: sin errores visibles, pero con {0} advertencias y cobertura observable {1}/5.",
                    safeSummary.WarningCount,
                    coverageSignals);
            }

            if (healthSnapshot.HasDegradation)
            {
                return F(
                    "Logs_Dashboard_ExecutiveReviewHealthFormat",
                    "Operación bajo revisión: última señal {0}, heartbeat {1} y dependencias {2}.",
                    healthSnapshot.LastActivityLabel ?? T("Logs_Dashboard_NoReferenceShort", "Sin referencia"),
                    healthSnapshot.HeartbeatLabel ?? T("Logs_Dashboard_NoHeartbeatShort", "Sin heartbeat"),
                    healthSnapshot.DependencyLabel ?? T("Logs_Dashboard_NoCoverage", "Sin cobertura"));
            }

            return F(
                "Logs_Dashboard_ExecutiveOperationalFormat",
                "Operación observable dentro del rango seleccionado, con {0} eventos y cobertura base {1}/5.",
                safeSummary.TotalEvents,
                coverageSignals);
        }

        private static string ResolveIncidentStatusLevel(int errorCount, int warningCount, int totalEntries)
        {
            if (totalEntries <= 0)
                return "limited";

            if (errorCount > 0)
                return "attention";

            if (warningCount > 0)
                return "review";

            return "stable";
        }

        private static string ResolveValidationStatusLevel(int validationCount, int rejectedCount, int totalEntries)
        {
            if (totalEntries <= 0 || validationCount <= 0)
                return "limited";

            if (rejectedCount > 0)
                return "attention";

            return "stable";
        }

        private static string ResolveLatencyStatusLevel(IReadOnlyList<LogMetricDistributionItem> latencyDistribution, int slowOperations)
        {
            var latencyData = latencyDistribution ?? Array.Empty<LogMetricDistributionItem>();
            if (!latencyData.Any(x => x.Count > 0))
                return "limited";

            if (slowOperations >= 3)
                return "attention";

            if (slowOperations > 0)
                return "review";

            return "stable";
        }

        private static string ResolveInstrumentationStatusLevel(IReadOnlyList<AppLogEntry> entries)
        {
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var signals = 0;

            if (safeEntries.Any(IsHealthSignal))
                signals++;

            if (safeEntries.Any(IsHeartbeatSignal))
                signals++;

            if (safeEntries.Any(IsDependencySignal))
                signals++;

            if (safeEntries.Any(IsSessionSignal))
                signals++;

            if (safeEntries.Any(IsValidationSignal))
                signals++;

            if (signals <= 2)
                return "limited";

            if (signals <= 4)
                return "review";

            return "stable";
        }

        private static string ResolveOperationalHealthStatusLevel(OperationalHealthSnapshot operationalHealth, IReadOnlyList<AppLogEntry> entries)
        {
            var healthSnapshot = operationalHealth ?? new OperationalHealthSnapshot();

            if (!healthSnapshot.HasObservableHealth)
                return ResolveInstrumentationStatusLevel(entries);

            if (healthSnapshot.HasSevereDegradation)
                return "attention";

            if (healthSnapshot.HasDegradation ||
                string.Equals(healthSnapshot.HeartbeatStatusLevel, "review", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(healthSnapshot.DependencyStatusLevel, "review", StringComparison.OrdinalIgnoreCase))
            {
                return "review";
            }

            if (string.Equals(healthSnapshot.HeartbeatStatusLevel, "limited", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(healthSnapshot.DependencyStatusLevel, "limited", StringComparison.OrdinalIgnoreCase))
            {
                return "limited";
            }

            return "stable";
        }

        private static string ResolveValidationCellStatusLevel(int count, bool hasRejectedValidations)
        {
            if (count <= 0)
                return "limited";

            return hasRejectedValidations ? "review" : "stable";
        }

        private static string ResolveCountStatusLevel(int count, int reviewThreshold, int attentionThreshold)
        {
            if (count <= 0)
                return "stable";

            if (count >= attentionThreshold)
                return "attention";

            if (count >= reviewThreshold)
                return "review";

            return "stable";
        }

        private static string ResolveStatusLabel(string statusLevel)
        {
            if (string.Equals(statusLevel, "attention", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_StatusAttention", "Atencion");

            if (string.Equals(statusLevel, "review", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_StatusReview", "Revision");

            if (string.Equals(statusLevel, "limited", StringComparison.OrdinalIgnoreCase))
                return T("Logs_Dashboard_PartialCoverage", "Cobertura parcial");

            return T("Logs_Dashboard_StatusOperational", "Operativo");
        }

        private static string ResolveRangeLabel(string rangeKey)
        {
            if (string.Equals(rangeKey, "24h", StringComparison.OrdinalIgnoreCase))
                return T("Logs_TimeRange_Last24Hours", "Ultimas 24 horas");

            if (string.Equals(rangeKey, "30d", StringComparison.OrdinalIgnoreCase))
                return T("Logs_TimeRange_Last30Days", "Ultimos 30 dias");

            if (string.Equals(rangeKey, "all", StringComparison.OrdinalIgnoreCase))
                return T("Logs_TimeRange_AllHistory", "Todo el historico");

            return T("Logs_TimeRange_Last7Days", "Ultimos 7 dias");
        }

        private static string FormatLatency(double latencyMs)
        {
            return latencyMs <= 0 ? T("Logs_NoData", "Sin datos") : latencyMs.ToString("N0") + " ms";
        }

        private static string T(string key, string fallback)
        {
            return LocalizedText.Get(key, fallback);
        }

        private static string F(string key, string fallback, params object[] args)
        {
            return string.Format(T(key, fallback), args);
        }
    }

    public class LogDashboardQuery
    {
        public string MachineName { get; set; }
        public string TimeRangeKey { get; set; }
        public string Severity { get; set; }
        public string Source { get; set; }
        public string UserName { get; set; }
        public string SearchText { get; set; }
    }

    public class LogDashboardSnapshot
    {
        public IReadOnlyList<AppLogEntry> Entries { get; set; }
        public LogDashboardSummary Summary { get; set; }
        public IReadOnlyList<string> AvailableMachines { get; set; }
        public IReadOnlyList<string> AvailableLevels { get; set; }
        public IReadOnlyList<string> AvailableSources { get; set; }
        public IReadOnlyList<string> AvailableUsers { get; set; }
        public IReadOnlyList<LogMetricPoint> ErrorSeries { get; set; }
        public IReadOnlyList<LogMetricPoint> WarningSeries { get; set; }
        public IReadOnlyList<LogMetricDistributionItem> SourceActivity { get; set; }
        public IReadOnlyList<LogMetricDistributionItem> UserActivity { get; set; }
        public IReadOnlyList<LogMetricDistributionItem> MachineActivity { get; set; }
        public IReadOnlyList<LogMetricDistributionItem> LatencyDistribution { get; set; }
        public IReadOnlyList<AppLogEntry> CriticalEvents { get; set; }
        public IReadOnlyList<AppLogEntry> TimelineEvents { get; set; }
        public string InstrumentationStatus { get; set; }
        public bool HasHealthSignals { get; set; }
        public bool HasSessionSignals { get; set; }
        public bool HasValidationSignals { get; set; }
        public LogExecutiveStatus ExecutiveStatus { get; set; }
        public IReadOnlyList<LogDashboardSection> StatusSections { get; set; }
    }

    public class LogDashboardSummary
    {
        public int TotalEvents { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int MachinesCount { get; set; }
        public double AverageLatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public AppLogEntry LatestError { get; set; }
        public AppLogEntry LatestCriticalEvent { get; set; }
    }

    public class LogMetricPoint
    {
        public string Label { get; set; }
        public int Count { get; set; }
    }

    public class LogMetricDistributionItem
    {
        public string Label { get; set; }
        public string FilterValue { get; set; }
        public int Count { get; set; }
        public string SecondaryText { get; set; }
    }

    public class LogExecutiveStatus
    {
        public string StatusLevel { get; set; }
        public string StatusLabel { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Detail { get; set; }
        public string WindowLabel { get; set; }
        public string CoverageLabel { get; set; }
        public string LifeSignLabel { get; set; }
        public string LifeSignDetail { get; set; }
        public string HeartbeatLabel { get; set; }
        public string HeartbeatDetail { get; set; }
        public string DependencyLabel { get; set; }
        public string DependencyDetail { get; set; }
        public string IncidentLabel { get; set; }
        public string IncidentDetail { get; set; }
    }

    public class LogDashboardSection
    {
        public string SectionKey { get; set; }
        public string Title { get; set; }
        public string StatusLevel { get; set; }
        public string StatusLabel { get; set; }
        public string Summary { get; set; }
        public string Detail { get; set; }
        public IReadOnlyList<LogStatusTrendCell> TrendCells { get; set; }
        public IReadOnlyList<LogStatusTimelineSegment> TimelineSegments { get; set; }
        public IReadOnlyList<LogStatusFact> Facts { get; set; }
    }

    public class LogStatusTrendCell
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public string StatusLevel { get; set; }
        public string Tooltip { get; set; }
    }

    public class LogStatusFact
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Hint { get; set; }
    }

    public class LogStatusTimelineSegment
    {
        public string SegmentId { get; set; }
        public string SectionKey { get; set; }
        public string BucketLabel { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string StatusLevel { get; set; }
        public string NarrativeState { get; set; }
        public string Severity { get; set; }
        public string Summary { get; set; }
        public string Explanation { get; set; }
        public string PrimarySource { get; set; }
        public string PrimaryState { get; set; }
        public string DependencyName { get; set; }
        public DateTime? IncidentStart { get; set; }
        public DateTime? IncidentEnd { get; set; }
        public string DurationLabel { get; set; }
        public string RecoveryState { get; set; }
        public string CoverageState { get; set; }
        public bool IsSelected { get; set; }
        public int ObservableCount { get; set; }
        public LogStatusSegmentDetail Detail { get; set; }
        public LogSegmentDrillDownContext DrillDown { get; set; }
    }

    public class LogStatusSegmentDetail
    {
        public string TooltipTitle { get; set; }
        public string TooltipBody { get; set; }
        public IReadOnlyList<LogStatusFact> Facts { get; set; }
        public IReadOnlyList<string> RelatedEventIdsOrLabels { get; set; }
    }

    public class LogSegmentDrillDownContext
    {
        public string SectionKey { get; set; }
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public string Severity { get; set; }
        public string Source { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string SearchText { get; set; }
        public string Dependency { get; set; }
        public string State { get; set; }
        public string PrimaryEventType { get; set; }
    }

    public class LogFilterOption
    {
        public string Value { get; set; }
        public string DisplayName { get; set; }
    }

    public class LogTimeRangeOption
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
    }

    public class LogTimeRangeWindow
    {
        public LogTimeRangeWindow(string key, DateTime? start)
        {
            Key = key;
            Start = start;
        }

        public string Key { get; private set; }
        public DateTime? Start { get; private set; }
    }

    internal sealed class OperationalHealthSnapshot
    {
        public string LastActivityLabel { get; set; }
        public string LastActivityDetail { get; set; }
        public string HeartbeatStatusLevel { get; set; }
        public string HeartbeatLabel { get; set; }
        public string HeartbeatDetail { get; set; }
        public string DependencyStatusLevel { get; set; }
        public string DependencyLabel { get; set; }
        public string DependencyDetail { get; set; }
        public int RecentIncidentCount { get; set; }
        public string RecentIncidentLabel { get; set; }
        public string RecentIncidentDetail { get; set; }
        public bool HasObservableHealth { get; set; }
        public bool HasObservableHeartbeat { get; set; }
        public bool HasObservableDependencies { get; set; }
        public bool HasDegradation { get; set; }
        public bool HasSevereDegradation { get; set; }
        public int VisibleIncidentCount { get; set; }
        public int VisibleDegradationCount { get; set; }
    }

    internal sealed class NarrativeClassification
    {
        public string StatusLevel { get; set; }
        public string NarrativeState { get; set; }
        public string Severity { get; set; }
        public string Summary { get; set; }
        public string Explanation { get; set; }
        public string PrimarySource { get; set; }
        public string PrimaryState { get; set; }
        public string DependencyName { get; set; }
        public DateTime? IncidentStart { get; set; }
        public DateTime? IncidentEnd { get; set; }
        public string DurationLabel { get; set; }
        public string RecoveryState { get; set; }
        public string CoverageState { get; set; }
        public string TooltipTitle { get; set; }
        public string TooltipBody { get; set; }
        public int ObservableCount { get; set; }
        public IReadOnlyList<LogStatusFact> Facts { get; set; }
        public IReadOnlyList<string> RelatedLabels { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string SearchText { get; set; }
        public string PrimaryEventType { get; set; }
    }

    internal sealed class RecoveryResolution
    {
        public string State { get; private set; }
        public DateTime? Timestamp { get; private set; }
        public bool IsObserved => string.Equals(State, "Observed", StringComparison.OrdinalIgnoreCase);

        public static RecoveryResolution Observed(DateTime timestamp)
        {
            return new RecoveryResolution { State = "Observed", Timestamp = timestamp };
        }

        public static RecoveryResolution NotObservable()
        {
            return new RecoveryResolution { State = "NotObservable" };
        }

        public static RecoveryResolution NotApplicable()
        {
            return new RecoveryResolution { State = "NotApplicable" };
        }
    }
}
