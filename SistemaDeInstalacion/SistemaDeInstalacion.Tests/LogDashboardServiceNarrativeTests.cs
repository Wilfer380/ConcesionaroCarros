using ConcesionaroCarros.Services;
using ConcesionaroCarros.ViewModels;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SistemaDeInstalacion.Tests
{
    [TestClass]
    public class LogDashboardServiceNarrativeTests
    {
        private static readonly IReadOnlyList<LogMetricDistributionItem> EmptyDistributions =
            Array.Empty<LogMetricDistributionItem>();

        [TestMethod]
        public void BuildStatusSections_PrioritizesHealthSignalsOverFallbackSignals()
        {
            var incidentTime = DateTime.Now.AddMinutes(-40);
            var entries = new[]
            {
                CreateEntry(incidentTime, "HEALTH", "DependencyMonitor", "SQL degradada", "signal=dependency | state=degraded | dependency=SQL"),
                CreateEntry(incidentTime.AddMinutes(1), "WARNING", "Orders", "Hay warnings secundarios", "reason=warning-fallback"),
                CreateEntry(incidentTime.AddMinutes(2), "LATENCY", "Orders", "Operacion lenta", "reason=slow-order", 1800)
            };

            var sections = BuildSections(entries, entries);
            var healthSegment = GetDominantSegment(sections, "health");

            AssertEx.NotNull(healthSegment, "Debe existir un segmento health observable.");
            AssertEx.Equal("HEALTH", healthSegment.Severity,
                "La severidad dominante debe quedar anclada a la senal health y no degradarse a WARNING/LATENCY por ruido secundario.");
            AssertEx.Equal("OperationalDegraded", healthSegment.NarrativeState,
                "La narrativa health degradada debe reflejar la degradacion operativa observable.");
            AssertEx.Contains("SQL", healthSegment.Explanation,
                "La explicacion debe conservar la dependencia afectada para drill-down coherente.");
        }

        [TestMethod]
        public void BuildStatusSections_ClassifiesFallbackIncidentWithoutHealthSignals()
        {
            var incidentTime = DateTime.Now.AddMinutes(-35);
            var entries = new[]
            {
                CreateEntry(incidentTime, "ERROR", "Billing", "Error cobrando factura", "reason=payment-failure")
            };

            var sections = BuildSections(entries, entries);
            var incidentSegment = GetDominantSegment(sections, "incidents");

            AssertEx.NotNull(incidentSegment, "Debe existir un segmento de incidentes observable.");
            AssertEx.Equal("ERROR", incidentSegment.Severity,
                "Sin health signals, la clasificacion debe caer al fallback mas fuerte visible.");
            AssertEx.Equal("OperationalUnhealthy", incidentSegment.NarrativeState,
                "Un ERROR aislado debe leerse como unhealthy y no como una simple revision.");
        }

        [TestMethod]
        public void BuildStatusSections_DistinguishesNoDataFromNoIncidentObserved()
        {
            var emptySections = BuildSections(Array.Empty<AppLogEntry>(), Array.Empty<AppLogEntry>());
            var emptyHealthSegment = GetFirstSegment(emptySections, "health");

            AssertEx.NotNull(emptyHealthSegment, "Incluso sin datos debe existir un bucket narrativo de cobertura.");
            AssertEx.Equal("NoData", emptyHealthSegment.NarrativeState,
                "Sin cobertura visible el dashboard no debe inventar salud operativa.");

            var healthyTime = DateTime.Now.AddMinutes(-25);
            var healthyEntries = new[]
            {
                CreateEntry(healthyTime, "HEALTH", "DependencyMonitor", "SQL saludable", "signal=dependency | state=healthy | dependency=SQL")
            };

            var healthySections = BuildSections(healthyEntries, healthyEntries);
            var healthySegment = GetDominantSegment(healthySections, "health");

            AssertEx.NotNull(healthySegment, "Con health real debe existir un segmento health dominante.");
            AssertEx.Equal("NoIncidentObserved", healthySegment.NarrativeState,
                "Una senal saludable debe quedar separada semanticamente del caso sin datos.");
            AssertEx.Equal("HEALTH", healthySegment.Severity,
                "La severidad debe seguir marcando que la lectura viene de health real.");
        }

        [TestMethod]
        public void BuildStatusSections_ReportsObservedAndUnobservableRecoveryHonestly()
        {
            var baseTime = DateTime.Now.AddMinutes(-50);
            var resolvedEntries = new[]
            {
                CreateEntry(baseTime, "HEALTH", "DependencyMonitor", "SQL degradada", "signal=dependency | state=degraded | dependency=SQL"),
                CreateEntry(baseTime.AddMinutes(8), "HEALTH", "DependencyMonitor", "SQL saludable", "signal=dependency | state=healthy | dependency=SQL")
            };
            var resolvedSections = BuildSections(resolvedEntries, resolvedEntries);
            var resolvedSegment = GetDominantSegment(resolvedSections, "health");

            AssertEx.NotNull(resolvedSegment, "Debe existir un segmento health resoluble.");
            AssertEx.Equal("Observed", resolvedSegment.RecoveryState,
                "Si hay una senal compatible de cierre, la recuperacion debe marcarse como observada.");
            AssertEx.Contains("Duracion observable", resolvedSegment.DurationLabel,
                "Con recuperacion observada debe exponerse una duracion honesta.");

            var unresolvedEntries = new[]
            {
                CreateEntry(baseTime, "HEALTH", "DependencyMonitor", "Broker degradado", "signal=dependency | state=degraded | dependency=Broker")
            };
            var unresolvedSections = BuildSections(unresolvedEntries, unresolvedEntries);
            var unresolvedSegment = GetDominantSegment(unresolvedSections, "health");

            AssertEx.NotNull(unresolvedSegment, "Debe existir un segmento health aun sin cierre observable.");
            AssertEx.Equal("NotObservable", unresolvedSegment.RecoveryState,
                "Sin senal real de recuperacion, el dashboard no debe fabricar cierre.");
            AssertEx.Equal("Recuperacion no observable", unresolvedSegment.DurationLabel,
                "La duracion debe ser honesta cuando no hay recovery verificable.");
        }

        [TestMethod]
        public void BuildStatusSections_KeepsStableSegmentIdentityAcrossEquivalentSnapshots()
        {
            var incidentTime = DateTime.Now.AddMinutes(-30);
            var entries = new[]
            {
                CreateEntry(incidentTime, "ERROR", "Orders", "Fallo al registrar pedido", "reason=order-failure"),
                CreateEntry(incidentTime.AddMinutes(2), "WARNING", "Orders", "Retry en curso", "reason=order-retry")
            };
            var periodBuckets = InvokeStatic(typeof(LogDashboardService), "ResolvePeriodBuckets", entries, "24h");

            var firstSections = BuildSections(entries, entries, periodBuckets);
            var secondSections = BuildSections(entries, entries, periodBuckets);
            var firstSegment = GetDominantSegment(firstSections, "incidents");
            var secondSegment = GetDominantSegment(secondSections, "incidents");

            AssertEx.NotNull(firstSegment, "El snapshot inicial debe emitir un segmento narrativo dominante.");
            AssertEx.NotNull(secondSegment, "El snapshot equivalente debe emitir el mismo segmento narrativo dominante.");
            AssertEx.Equal(firstSegment.SegmentId, secondSegment.SegmentId,
                "Dos snapshots equivalentes deben reconciliarse por el mismo SegmentId estable.");
        }

        [TestMethod]
        public void ApplySnapshot_PreservesNarrativeSelectionWhenSegmentIdStillExists()
        {
            var originalSegment = CreateSegment("segment-001", "Primer incidente", DateTime.Now.AddMinutes(-20));
            var refreshedSegment = CreateSegment("segment-001", "Primer incidente refrescado", DateTime.Now.AddMinutes(-20));

            using (var viewModel = new LogsViewModel())
            {
                InvokeInstance(viewModel, "ApplySnapshot", CreateSnapshot(originalSegment));
                viewModel.SelectNarrativeSegmentCommand.Execute(viewModel.StatusSections[0].TimelineSegments[0]);

                InvokeInstance(viewModel, "ApplySnapshot", CreateSnapshot(refreshedSegment));

                AssertEx.Equal("segment-001", viewModel.SelectedNarrativeSegmentId,
                    "La seleccion debe reconciliarse por SegmentId tras el refresh.");
                AssertEx.Equal("Primer incidente refrescado", viewModel.SelectedNarrativeSegment.Summary,
                    "La seleccion debe apuntar al objeto refrescado, no a una referencia vieja.");
                AssertEx.True(viewModel.StatusSections[0].TimelineSegments[0].IsSelected,
                    "El segmento reconciliado debe seguir marcado visualmente como seleccionado.");
            }
        }

        [TestMethod]
        public void ApplySnapshot_ClearsNarrativeSelectionHonestlyWhenSegmentLeavesWindow()
        {
            var originalSegment = CreateSegment("segment-002", "Incidente efimero", DateTime.Now.AddMinutes(-15));
            var replacementSegment = CreateSegment("segment-999", "Otro incidente", DateTime.Now.AddMinutes(-10));

            using (var viewModel = new LogsViewModel())
            {
                InvokeInstance(viewModel, "ApplySnapshot", CreateSnapshot(originalSegment));
                viewModel.SelectNarrativeSegmentCommand.Execute(viewModel.StatusSections[0].TimelineSegments[0]);

                InvokeInstance(viewModel, "ApplySnapshot", CreateSnapshot(replacementSegment));

                AssertEx.Null(viewModel.SelectedNarrativeSegment,
                    "Si el segmento desaparece de la ventana activa, la seleccion debe limpiarse.");
                AssertEx.True(string.IsNullOrWhiteSpace(viewModel.SelectedNarrativeSegmentId),
                    "No se debe conservar un SegmentId colgante cuando el contexto ya no existe.");
                AssertEx.Contains("ya no existe en la ventana actual", viewModel.NarrativeSelectionStatus,
                    "El mensaje al usuario debe explicar honestamente que el contexto desaparecio, sin remapearlo a otro incidente.");
            }
        }

        private static IReadOnlyList<LogDashboardSection> BuildSections(
            IReadOnlyList<AppLogEntry> entries,
            IReadOnlyList<AppLogEntry> baseEntries,
            object periodBuckets = null)
        {
            var service = new LogDashboardService();
            var safeEntries = entries ?? Array.Empty<AppLogEntry>();
            var safeBaseEntries = baseEntries ?? safeEntries;
            var summary = service.BuildSummary(safeEntries);
            var resolvedBuckets = periodBuckets ?? InvokeStatic(typeof(LogDashboardService), "ResolvePeriodBuckets", safeEntries, "24h");

            return (IReadOnlyList<LogDashboardSection>)InvokeStatic(
                typeof(LogDashboardService),
                "BuildStatusSections",
                safeEntries,
                summary,
                resolvedBuckets,
                EmptyDistributions,
                EmptyDistributions,
                EmptyDistributions,
                EmptyDistributions,
                safeBaseEntries,
                "Cobertura de prueba",
                null);
        }

        private static LogDashboardSection GetSection(IEnumerable<LogDashboardSection> sections, string sectionKey)
        {
            return (sections ?? Array.Empty<LogDashboardSection>())
                .FirstOrDefault(section => string.Equals(section.SectionKey, sectionKey, StringComparison.OrdinalIgnoreCase));
        }

        private static LogStatusTimelineSegment GetFirstSegment(IEnumerable<LogDashboardSection> sections, string sectionKey)
        {
            return GetSection(sections, sectionKey)?.TimelineSegments?.FirstOrDefault();
        }

        private static LogStatusTimelineSegment GetDominantSegment(IEnumerable<LogDashboardSection> sections, string sectionKey)
        {
            return GetSection(sections, sectionKey)?.TimelineSegments?
                .OrderByDescending(segment => segment.ObservableCount)
                .ThenByDescending(segment => segment.End)
                .FirstOrDefault(segment => segment.ObservableCount > 0)
                ?? GetFirstSegment(sections, sectionKey);
        }

        private static AppLogEntry CreateEntry(
            DateTime timestamp,
            string level,
            string source,
            string message,
            string details,
            long? durationMs = null)
        {
            return new AppLogEntry
            {
                Timestamp = timestamp,
                Level = level,
                MachineName = "MACHINE-01",
                UserName = "qa.user",
                Source = source,
                Message = message,
                Details = details,
                DurationMs = durationMs,
                LogFilePath = "tests.log"
            };
        }

        private static LogDashboardSnapshot CreateSnapshot(LogStatusTimelineSegment segment)
        {
            return new LogDashboardSnapshot
            {
                Entries = Array.Empty<AppLogEntry>(),
                Summary = new LogDashboardSummary(),
                ErrorSeries = Array.Empty<LogMetricPoint>(),
                WarningSeries = Array.Empty<LogMetricPoint>(),
                SourceActivity = EmptyDistributions,
                UserActivity = EmptyDistributions,
                MachineActivity = EmptyDistributions,
                LatencyDistribution = EmptyDistributions,
                CriticalEvents = Array.Empty<AppLogEntry>(),
                TimelineEvents = Array.Empty<AppLogEntry>(),
                StatusSections = new[]
                {
                    new LogDashboardSection
                    {
                        SectionKey = "incidents",
                        Title = "Errores e incidentes",
                        StatusLevel = "review",
                        StatusLabel = "Revision",
                        Summary = segment?.Summary ?? "Sin resumen",
                        Detail = segment?.Explanation ?? "Sin detalle",
                        TimelineSegments = segment == null ? Array.Empty<LogStatusTimelineSegment>() : new[] { segment },
                        Facts = Array.Empty<LogStatusFact>()
                    }
                },
                ExecutiveStatus = new LogExecutiveStatus
                {
                    StatusLevel = "review",
                    StatusLabel = "Revision",
                    Title = "Estado ejecutivo del rango filtrado",
                    Summary = "Snapshot sintetico para pruebas.",
                    Detail = "Sin build/runtime.",
                    WindowLabel = "Ultimas 24 horas",
                    CoverageLabel = "Cobertura observable: 1/5 senales base.",
                    LifeSignLabel = "Senal sintetica",
                    LifeSignDetail = "Prueba de reconciliacion",
                    HeartbeatLabel = "Heartbeat sintetico",
                    HeartbeatDetail = "Prueba de reconciliacion",
                    DependencyLabel = "Dependencia sintetica",
                    DependencyDetail = "Prueba de reconciliacion",
                    IncidentLabel = "Incidente sintetico",
                    IncidentDetail = "Prueba de reconciliacion"
                }
            };
        }

        private static LogStatusTimelineSegment CreateSegment(string segmentId, string summary, DateTime start)
        {
            return new LogStatusTimelineSegment
            {
                SegmentId = segmentId,
                SectionKey = "incidents",
                BucketLabel = start.ToString("HH:mm"),
                Start = start,
                End = start.AddMinutes(10),
                StatusLevel = "attention",
                NarrativeState = "OperationalUnhealthy",
                Severity = "ERROR",
                Summary = summary,
                Explanation = "Segmento sintetico para reconciliacion de seleccion.",
                PrimarySource = "Tests",
                PrimaryState = "ERROR",
                DependencyName = string.Empty,
                IncidentStart = start,
                IncidentEnd = null,
                DurationLabel = "Recuperacion no observable",
                RecoveryState = "NotObservable",
                CoverageState = "Active",
                ObservableCount = 1,
                Detail = new LogStatusSegmentDetail
                {
                    TooltipTitle = summary,
                    TooltipBody = "Detalle sintetico",
                    Facts = Array.Empty<LogStatusFact>(),
                    RelatedEventIdsOrLabels = Array.Empty<string>()
                },
                DrillDown = new LogSegmentDrillDownContext
                {
                    SectionKey = "incidents",
                    RangeStart = start,
                    RangeEnd = start.AddMinutes(10),
                    Severity = "ERROR",
                    Source = "Tests",
                    MachineName = "MACHINE-01",
                    UserName = "qa.user",
                    SearchText = summary,
                    Dependency = string.Empty,
                    State = "ERROR",
                    PrimaryEventType = "error"
                }
            };
        }

        private static object InvokeStatic(Type targetType, string methodName, params object[] arguments)
        {
            return InvokeMethod(targetType, null, methodName, arguments);
        }

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            return InvokeMethod(target.GetType(), target, methodName, arguments);
        }

        private static object InvokeMethod(Type targetType, object target, string methodName, params object[] arguments)
        {
            var methods = targetType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
                .ToList();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != (arguments?.Length ?? 0))
                    continue;

                var compatible = true;
                for (var index = 0; index < parameters.Length; index++)
                {
                    var argument = arguments[index];
                    if (argument == null)
                        continue;

                    if (!parameters[index].ParameterType.IsInstanceOfType(argument))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (!compatible)
                    continue;

                return method.Invoke(target, arguments);
            }

            throw new MissingMethodException(targetType.FullName, methodName);
        }
    }
}
