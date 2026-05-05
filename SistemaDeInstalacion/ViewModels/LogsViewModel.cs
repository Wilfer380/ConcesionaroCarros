using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcesionaroCarros.ViewModels
{
    public class LogsViewModel : BaseViewModel, IDisposable, ILocalizableViewModel
    {
        private const string AllFilterValue = "__all";
        private const string DashboardBlockCategory = "dashboard-block";
        private const string SourceActivityCategory = "source-activity";
        private const string UserActivityCategory = "user-activity";
        private const string MachineActivityCategory = "machine-activity";
        private const string LatencyDistributionCategory = "latency-distribution";

        private static readonly HashSet<string> DeveloperUsers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "wandica",
                "maicolj"
            };

        private static bool IsNoDataValue(string value)
        {
            return string.Equals(value, T("Logs_Dashboard_NoDataValue", "Sin dato"), StringComparison.OrdinalIgnoreCase);
        }

        private readonly LogDashboardService _dashboardService = new LogDashboardService();
        private readonly DispatcherTimer _autoRefreshTimer;
        private readonly DispatcherTimer _eventRefreshTimer;
        private readonly Dispatcher _uiDispatcher;
        private readonly string _currentDeveloperUser;
        private bool _isRefreshingFilters;
        private bool _hasAppliedDefaultContext;
        private string _statusMessage;
        private string _selectedMachineFilter;
        private LogFilterOption _selectedMachineOption;
        private string _selectedTimeRangeKey;
        private LogTimeRangeOption _selectedTimeRangeOption;
        private string _selectedSeverityFilter;
        private LogFilterOption _selectedSeverityOption;
        private string _selectedSourceFilter;
        private LogFilterOption _selectedSourceOption;
        private string _selectedUserFilter;
        private LogFilterOption _selectedUserOption;
        private string _searchText;
        private int _totalEventos;
        private int _totalErrores;
        private int _totalAdvertencias;
        private int _totalEquipos;
        private string _latenciaPromedio;
        private string _latenciaPercentil95;
        private string _ultimoError;
        private string _instrumentationStatus;
        private string _executiveStatusTitle;
        private string _executiveStatusLevel;
        private string _executiveStatusLabel;
        private string _executiveStatusSummary;
        private string _executiveStatusDetail;
        private string _executiveWindowLabel;
        private string _executiveCoverageLabel;
        private string _executiveLifeSignLabel;
        private string _executiveLifeSignDetail;
        private string _executiveHeartbeatLabel;
        private string _executiveHeartbeatDetail;
        private string _executiveDependencyLabel;
        private string _executiveDependencyDetail;
        private string _executiveIncidentLabel;
        private string _executiveIncidentDetail;
        private int _errorSeriesMax;
        private int _warningSeriesMax;
        private int _sourceActivityMax;
        private int _userActivityMax;
        private int _machineActivityMax;
        private int _latencyDistributionMax;
        private LogDashboardSnapshot _latestSnapshot;
        private LogSegmentDrillDownContext _activeNarrativeDrillDown;
        private bool _hasActiveIncidentTimelineDrillDown;
        private string _incidentDrillDownMachineFilter;
        private string _incidentDrillDownSeverityFilter;
        private string _incidentDrillDownSourceFilter;
        private string _incidentDrillDownUserFilter;
        private string _selectedNarrativeSegmentId;
        private LogStatusTimelineSegment _selectedNarrativeSegment;
        private LogStatusTimelineSegment _hoveredNarrativeSegment;
        private string _narrativeSelectionStatus;
        private DashboardHoverCard _dashboardHoverCard;
        private bool _isNarrativeHoverSourceActive;
        private bool _isNarrativeHoverContextActive;
        private bool _isDashboardHoverSourceActive;
        private bool _isDashboardHoverCardActive;
        private bool _refreshDeferredWhileHover;
        private bool _eventRefreshPending;
        private string _dashboardViewModeKey;
        private string _lastHistoryRangeKey;

        public ObservableCollection<AppLogEntry> FilteredEntries { get; } =
            new ObservableCollection<AppLogEntry>();

        public ObservableCollection<LogFilterOption> AvailableMachines { get; } =
            new ObservableCollection<LogFilterOption>();

        public ObservableCollection<LogTimeRangeOption> AvailableTimeRanges { get; } =
            new ObservableCollection<LogTimeRangeOption>();

        public ObservableCollection<LogFilterOption> AvailableSeverities { get; } =
            new ObservableCollection<LogFilterOption>();

        public ObservableCollection<LogFilterOption> AvailableSources { get; } =
            new ObservableCollection<LogFilterOption>();

        public ObservableCollection<LogFilterOption> AvailableUsers { get; } =
            new ObservableCollection<LogFilterOption>();

        public ObservableCollection<LogMetricPoint> ErrorSeries { get; } =
            new ObservableCollection<LogMetricPoint>();

        public ObservableCollection<LogMetricPoint> WarningSeries { get; } =
            new ObservableCollection<LogMetricPoint>();

        public ObservableCollection<LogMetricDistributionItem> SourceActivity { get; } =
            new ObservableCollection<LogMetricDistributionItem>();

        public ObservableCollection<LogMetricDistributionItem> UserActivity { get; } =
            new ObservableCollection<LogMetricDistributionItem>();

        public ObservableCollection<LogMetricDistributionItem> MachineActivity { get; } =
            new ObservableCollection<LogMetricDistributionItem>();

        public ObservableCollection<LogMetricDistributionItem> LatencyDistribution { get; } =
            new ObservableCollection<LogMetricDistributionItem>();

        public ObservableCollection<AppLogEntry> RecentCriticalEvents { get; } =
            new ObservableCollection<AppLogEntry>();

        public ObservableCollection<IncidentDayGroup> IncidentDays { get; } =
            new ObservableCollection<IncidentDayGroup>();

        public ObservableCollection<LogDashboardSection> StatusSections { get; } =
            new ObservableCollection<LogDashboardSection>();

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ApplySourceFilterCommand { get; }
        public ICommand ApplyUserFilterCommand { get; }
        public ICommand ApplyMachineFilterCommand { get; }
        public ICommand ApplyIncidentDrillDownCommand { get; }
        public ICommand SelectNarrativeSegmentCommand { get; }
        public ICommand ApplyNarrativeSegmentDrillDownCommand { get; }
        public ICommand ClearNarrativeSelectionCommand { get; }
        public ICommand ClearTimelineDrillDownCommand { get; }
        public ICommand SetDashboardViewModeCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string SelectedMachineFilter
        {
            get => _selectedMachineFilter;
            set
            {
                _selectedMachineFilter = value;
                _selectedMachineOption = ResolveOption(AvailableMachines, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMachineOption));
                RefreshIfNeeded();
            }
        }

        public LogFilterOption SelectedMachineOption
        {
            get => _selectedMachineOption;
            set
            {
                var resolvedOption = ResolveSelectedOption(AvailableMachines, value, _selectedMachineFilter);
                if (resolvedOption == null && _isRefreshingFilters)
                    return;

                _selectedMachineOption = resolvedOption;
                _selectedMachineFilter = resolvedOption?.Value ?? AllFilterValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMachineFilter));
                RefreshIfNeeded();
            }
        }

        public string SelectedTimeRangeKey
        {
            get => _selectedTimeRangeKey;
            set
            {
                _selectedTimeRangeKey = value;
                _selectedTimeRangeOption = ResolveTimeRangeOption(AvailableTimeRanges, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTimeRangeOption));
                RefreshIfNeeded();
            }
        }

        public LogTimeRangeOption SelectedTimeRangeOption
        {
            get => _selectedTimeRangeOption;
            set
            {
                var resolvedOption = ResolveSelectedTimeRangeOption(AvailableTimeRanges, value, _selectedTimeRangeKey);
                if (resolvedOption == null && _isRefreshingFilters)
                    return;

                _selectedTimeRangeOption = resolvedOption;
                _selectedTimeRangeKey = resolvedOption?.Key ?? "7d";
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTimeRangeKey));
                RefreshIfNeeded();
            }
        }

        public string DashboardViewModeKey
        {
            get => _dashboardViewModeKey;
            set => SetDashboardViewMode(value);
        }

        public bool IsRealTimeView => string.Equals(_dashboardViewModeKey, "realtime", StringComparison.OrdinalIgnoreCase);
        public bool IsHistoryView => !IsRealTimeView;

        public string SelectedSeverityFilter
        {
            get => _selectedSeverityFilter;
            set
            {
                _selectedSeverityFilter = value;
                _selectedSeverityOption = ResolveOption(AvailableSeverities, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSeverityOption));
                RefreshIfNeeded();
            }
        }

        public LogFilterOption SelectedSeverityOption
        {
            get => _selectedSeverityOption;
            set
            {
                var resolvedOption = ResolveSelectedOption(AvailableSeverities, value, _selectedSeverityFilter);
                if (resolvedOption == null && _isRefreshingFilters)
                    return;

                _selectedSeverityOption = resolvedOption;
                _selectedSeverityFilter = resolvedOption?.Value ?? AllFilterValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSeverityFilter));
                RefreshIfNeeded();
            }
        }

        public string SelectedSourceFilter
        {
            get => _selectedSourceFilter;
            set
            {
                _selectedSourceFilter = value;
                _selectedSourceOption = ResolveOption(AvailableSources, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSourceOption));
                RefreshIfNeeded();
            }
        }

        public LogFilterOption SelectedSourceOption
        {
            get => _selectedSourceOption;
            set
            {
                var resolvedOption = ResolveSelectedOption(AvailableSources, value, _selectedSourceFilter);
                if (resolvedOption == null && _isRefreshingFilters)
                    return;

                _selectedSourceOption = resolvedOption;
                _selectedSourceFilter = resolvedOption?.Value ?? AllFilterValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSourceFilter));
                RefreshIfNeeded();
            }
        }

        public string SelectedUserFilter
        {
            get => _selectedUserFilter;
            set
            {
                _selectedUserFilter = value;
                _selectedUserOption = ResolveOption(AvailableUsers, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedUserOption));
                RefreshIfNeeded();
            }
        }

        public LogFilterOption SelectedUserOption
        {
            get => _selectedUserOption;
            set
            {
                var resolvedOption = ResolveSelectedOption(AvailableUsers, value, _selectedUserFilter);
                if (resolvedOption == null && _isRefreshingFilters)
                    return;

                _selectedUserOption = resolvedOption;
                _selectedUserFilter = resolvedOption?.Value ?? AllFilterValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedUserFilter));
                RefreshIfNeeded();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                RefreshIfNeeded();
            }
        }

        public int TotalEventos
        {
            get => _totalEventos;
            set
            {
                _totalEventos = value;
                OnPropertyChanged();
            }
        }

        public int TotalErrores
        {
            get => _totalErrores;
            set
            {
                _totalErrores = value;
                OnPropertyChanged();
            }
        }

        public int TotalAdvertencias
        {
            get => _totalAdvertencias;
            set
            {
                _totalAdvertencias = value;
                OnPropertyChanged();
            }
        }

        public int TotalEquipos
        {
            get => _totalEquipos;
            set
            {
                _totalEquipos = value;
                OnPropertyChanged();
            }
        }

        public string LatenciaPromedio
        {
            get => _latenciaPromedio;
            set
            {
                _latenciaPromedio = value;
                OnPropertyChanged();
            }
        }

        public string LatenciaPercentil95
        {
            get => _latenciaPercentil95;
            set
            {
                _latenciaPercentil95 = value;
                OnPropertyChanged();
            }
        }

        public string UltimoError
        {
            get => _ultimoError;
            set
            {
                _ultimoError = value;
                OnPropertyChanged();
            }
        }

        public string InstrumentationStatus
        {
            get => _instrumentationStatus;
            set
            {
                _instrumentationStatus = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveStatusTitle
        {
            get => _executiveStatusTitle;
            set
            {
                _executiveStatusTitle = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveStatusLabel
        {
            get => _executiveStatusLabel;
            set
            {
                _executiveStatusLabel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveStatusLevel
        {
            get => _executiveStatusLevel;
            set
            {
                _executiveStatusLevel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveStatusSummary
        {
            get => _executiveStatusSummary;
            set
            {
                _executiveStatusSummary = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveStatusDetail
        {
            get => _executiveStatusDetail;
            set
            {
                _executiveStatusDetail = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveWindowLabel
        {
            get => _executiveWindowLabel;
            set
            {
                _executiveWindowLabel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveLifeSignLabel
        {
            get => _executiveLifeSignLabel;
            set
            {
                _executiveLifeSignLabel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveLifeSignDetail
        {
            get => _executiveLifeSignDetail;
            set
            {
                _executiveLifeSignDetail = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveHeartbeatLabel
        {
            get => _executiveHeartbeatLabel;
            set
            {
                _executiveHeartbeatLabel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveHeartbeatDetail
        {
            get => _executiveHeartbeatDetail;
            set
            {
                _executiveHeartbeatDetail = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveDependencyLabel
        {
            get => _executiveDependencyLabel;
            set
            {
                _executiveDependencyLabel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveDependencyDetail
        {
            get => _executiveDependencyDetail;
            set
            {
                _executiveDependencyDetail = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveIncidentLabel
        {
            get => _executiveIncidentLabel;
            set
            {
                _executiveIncidentLabel = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveIncidentDetail
        {
            get => _executiveIncidentDetail;
            set
            {
                _executiveIncidentDetail = value;
                OnPropertyChanged();
            }
        }

        public string ExecutiveCoverageLabel
        {
            get => _executiveCoverageLabel;
            set
            {
                _executiveCoverageLabel = value;
                OnPropertyChanged();
            }
        }

        public int ErrorSeriesMax
        {
            get => _errorSeriesMax;
            set
            {
                _errorSeriesMax = value;
                OnPropertyChanged();
            }
        }

        public int WarningSeriesMax
        {
            get => _warningSeriesMax;
            set
            {
                _warningSeriesMax = value;
                OnPropertyChanged();
            }
        }

        public int SourceActivityMax
        {
            get => _sourceActivityMax;
            set
            {
                _sourceActivityMax = value;
                OnPropertyChanged();
            }
        }

        public int UserActivityMax
        {
            get => _userActivityMax;
            set
            {
                _userActivityMax = value;
                OnPropertyChanged();
            }
        }

        public int MachineActivityMax
        {
            get => _machineActivityMax;
            set
            {
                _machineActivityMax = value;
                OnPropertyChanged();
            }
        }

        public int LatencyDistributionMax
        {
            get => _latencyDistributionMax;
            set
            {
                _latencyDistributionMax = value;
                OnPropertyChanged();
            }
        }

        public string SelectedNarrativeSegmentId
        {
            get => _selectedNarrativeSegmentId;
            set
            {
                _selectedNarrativeSegmentId = value;
                OnPropertyChanged();
            }
        }

        public LogStatusTimelineSegment SelectedNarrativeSegment
        {
            get => _selectedNarrativeSegment;
            set
            {
                _selectedNarrativeSegment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedNarrativeSegment));
                OnPropertyChanged(nameof(ContextNarrativeSegment));
            }
        }

        public bool HasSelectedNarrativeSegment => SelectedNarrativeSegment != null;

        public bool HasActiveNarrativeDrillDown => _activeNarrativeDrillDown != null;

        public bool HasTimelineDrillDownContext => HasActiveNarrativeDrillDown || _hasActiveIncidentTimelineDrillDown;

        public string NarrativeSelectionStatus
        {
            get => _narrativeSelectionStatus;
            set
            {
                _narrativeSelectionStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NarrativeContextStatus));
            }
        }

        public string NarrativeContextTitle => _hoveredNarrativeSegment != null
            ? T("Logs_Dashboard_NarrativeHoverTitle", "Lectura narrativa en hover")
            : T("Logs_Dashboard_NarrativePersistentTitle", "Contexto narrativo persistente");

        public LogStatusTimelineSegment ContextNarrativeSegment => _hoveredNarrativeSegment ?? _selectedNarrativeSegment;

        public string NarrativeContextEyebrow => ContextNarrativeSegment == null
            ? T("Logs_Dashboard_NoNarrativeBlockFocused", "Sin bloque narrativo en foco")
            : string.Format(
                "{0:yyyy-MM-dd HH:mm} · {1}",
                ContextNarrativeSegment.Start,
                string.IsNullOrWhiteSpace(ContextNarrativeSegment.BucketLabel) ? T("Logs_Dashboard_HistoricalBlock", "Bloque historico") : ContextNarrativeSegment.BucketLabel);

        public string NarrativeContextSeverityBadge => string.IsNullOrWhiteSpace(ContextNarrativeSegment?.Severity)
            ? T("Logs_Dashboard_NoSeverity", "Sin severidad")
            : ContextNarrativeSegment.Severity;

        public string NarrativeContextStateBadge => string.IsNullOrWhiteSpace(ContextNarrativeSegment?.NarrativeState)
            ? T("Logs_Dashboard_NoState", "Sin estado")
            : ContextNarrativeSegment.NarrativeState;

        public string NarrativeContextStatusIcon => ResolveStatusIcon(ContextNarrativeSegment?.StatusLevel, ContextNarrativeSegment?.Severity);

        public string NarrativeContextPeriodLabel => ContextNarrativeSegment == null
            ? T("Logs_Dashboard_NoVisiblePeriod", "Sin periodo visible")
            : string.Format(
                "{0:yyyy-MM-dd HH:mm} → {1:yyyy-MM-dd HH:mm}",
                ContextNarrativeSegment.Start,
                ContextNarrativeSegment.End);

        public string NarrativeContextDurationLabel => !string.IsNullOrWhiteSpace(ContextNarrativeSegment?.DurationLabel)
            ? ContextNarrativeSegment.DurationLabel
            : T("Logs_Dashboard_NoObservableDuration", "Sin duracion observable");

        public IReadOnlyList<string> NarrativeContextRelatedItems => (ContextNarrativeSegment?.Detail?.RelatedEventIdsOrLabels ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(8)
            .ToArray();

        public bool HasNarrativeContextRelatedItems => NarrativeContextRelatedItems.Count > 0;

        public string NarrativeContextStatus => _hoveredNarrativeSegment == null || ContextNarrativeSegment == null
            ? NarrativeSelectionStatus
            : string.Format(
                T("Logs_Dashboard_NarrativeHoverActiveFormat", "Hover activo en {0}: el detalle queda visible mientras mantengas el puntero sobre este bloque. Click solo si querés fijar contexto o bajar a drill-down."),
                ContextNarrativeSegment.BucketLabel);

        public DashboardHoverCard DashboardHoverCard
        {
            get => _dashboardHoverCard;
            private set
            {
                _dashboardHoverCard = value;
                OnPropertyChanged();
            }
        }

        public LogsViewModel()
        {
            _currentDeveloperUser = ResolveCurrentDeveloperUser();
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            _dashboardViewModeKey = "realtime";
            _selectedTimeRangeKey = "2h";
            _lastHistoryRangeKey = "7d";
            _selectedMachineFilter = AllFilterValue;
            _selectedSeverityFilter = AllFilterValue;
            _selectedSourceFilter = AllFilterValue;
            _selectedUserFilter = AllFilterValue;
            _latenciaPromedio = LocalizedText.Get("Logs_NoData", "Sin datos");
            _latenciaPercentil95 = LocalizedText.Get("Logs_NoData", "Sin datos");
            _ultimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
            _instrumentationStatus = LocalizedText.Get("Logs_NoData", "Sin datos");
            _executiveStatusTitle = T("Logs_Dashboard_ExecutiveStatusTitle", "Estado ejecutivo del rango filtrado");
            _executiveStatusLevel = "limited";
            _executiveStatusLabel = T("Logs_Dashboard_PartialCoverage", "Cobertura parcial");
            _executiveStatusSummary = T("Logs_Dashboard_PreparingRealSignals", "Preparando lectura de senales reales...");
            _executiveStatusDetail = T("Logs_Dashboard_ExecutiveStatusDetailFallback", "El resumen se basa en logs observables; no representa uptime.");
            _executiveWindowLabel = T("Logs_TimeRange_Last7Days", "Ultimos 7 dias");
            _executiveCoverageLabel = T("Logs_Dashboard_ObservableCoverageFallback", "Cobertura observable: 0/5 senales base.");
            _executiveLifeSignLabel = T("Logs_Dashboard_NoRecentSignal", "Sin senal reciente");
            _executiveLifeSignDetail = T("Logs_Dashboard_WaitingHealthActivity", "Esperando actividad health observable.");
            _executiveHeartbeatLabel = T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable");
            _executiveHeartbeatDetail = T("Logs_Dashboard_HeartbeatAliveOnly", "La app solo reporta heartbeat mientras esta viva.");
            _executiveDependencyLabel = T("Logs_Dashboard_NoCoverage", "Sin cobertura");
            _executiveDependencyDetail = T("Logs_Dashboard_NoDependenciesObservableYet", "Todavia no hay dependencias observables.");
            _executiveIncidentLabel = T("Logs_Dashboard_NoRecentHealthIncidents", "Sin incidentes health recientes");
            _executiveIncidentDetail = T("Logs_Dashboard_NoVisibleHealthDegradations", "No hay degradaciones health visibles.");
            _narrativeSelectionStatus = T("Logs_Dashboard_SelectHistoricalSegmentStatus", "Selecciona un segmento historico para fijar contexto narrativo y bajar a drill-down coherente.");
            _dashboardHoverCard = BuildDefaultHoverCard();

            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

            _eventRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _eventRefreshTimer.Tick += EventRefreshTimer_Tick;

            RefreshCommand = new RelayCommand(_ => Refresh());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            ApplySourceFilterCommand = new RelayCommand(value => ApplySourceFilter(value as string));
            ApplyUserFilterCommand = new RelayCommand(value => ApplyUserFilter(value as string));
            ApplyMachineFilterCommand = new RelayCommand(value => ApplyMachineFilter(value as string));
            ApplyIncidentDrillDownCommand = new RelayCommand(value => ApplyIncidentDrillDown(value as AppLogEntry));
            SelectNarrativeSegmentCommand = new RelayCommand(value => SelectNarrativeSegment(value as LogStatusTimelineSegment));
            ApplyNarrativeSegmentDrillDownCommand = new RelayCommand(value => ApplyNarrativeSegmentDrillDown((value as LogStatusTimelineSegment) ?? SelectedNarrativeSegment));
            ClearNarrativeSelectionCommand = new RelayCommand(_ => ClearNarrativeSelection());
            ClearTimelineDrillDownCommand = new RelayCommand(_ => ClearTimelineDrillDown());
            SetDashboardViewModeCommand = new RelayCommand(value => SetDashboardViewMode(value as string));

            BuildTimeRanges();
            Refresh();

            // Tiempo real por evento: la UI se refresca en cuanto entra un log en esta instancia.
            LogService.LogWritten += LogService_LogWritten;
        }

        public void StartAutoRefresh()
        {
            if (!_autoRefreshTimer.IsEnabled)
                _autoRefreshTimer.Start();
        }

        public void SetNarrativeHoverSourceActive(bool isActive)
        {
            UpdateInteractiveHoverState(() => _isNarrativeHoverSourceActive = isActive);
        }

        public void SetNarrativeContextHoverActive(bool isActive)
        {
            UpdateInteractiveHoverState(() => _isNarrativeHoverContextActive = isActive);
        }

        public void SetDashboardHoverSourceActive(bool isActive)
        {
            UpdateInteractiveHoverState(() => _isDashboardHoverSourceActive = isActive);
        }

        public void SetDashboardHoverCardActive(bool isActive)
        {
            UpdateInteractiveHoverState(() => _isDashboardHoverCardActive = isActive);
        }

        public void PreviewNarrativeSegment(LogStatusTimelineSegment segment)
        {
            if (segment == null)
                return;

            _hoveredNarrativeSegment = segment;
            PreviewNarrativeHoverCard(segment);
            RaiseNarrativeContextProperties();
        }

        public void ClearNarrativeHover(LogStatusTimelineSegment segment = null)
        {
            if (_hoveredNarrativeSegment == null)
                return;

            if (segment != null &&
                !string.Equals(_hoveredNarrativeSegment.SegmentId, segment.SegmentId, StringComparison.Ordinal))
                return;

            _hoveredNarrativeSegment = null;
            ClearDashboardHoverCard();
            RaiseNarrativeContextProperties();
        }

        private void PreviewNarrativeHoverCard(LogStatusTimelineSegment segment)
        {
            if (segment == null)
                return;

            var titleDate = segment.Start == default
                ? segment.BucketLabel ?? T("Logs_Dashboard_HistoricalBlock", "Bloque historico")
                : segment.Start.ToString("yyyy-MM-dd HH:mm");

            var duration = !string.IsNullOrWhiteSpace(segment.DurationLabel)
                ? segment.DurationLabel
                : segment.IncidentStart.HasValue
                    ? T("Logs_Dashboard_InProgress", "En curso")
                    : string.Empty;

            var facts = new List<LogStatusFact>();
            facts.Add(new LogStatusFact { Label = T("Logs_Dashboard_FactBlock", "Bloque"), Value = segment.BucketLabel ?? "-", Hint = T("Logs_Dashboard_FactBlockHint", "Ventana temporal inspeccionada.") });
            facts.Add(new LogStatusFact { Label = T("Logs_Dashboard_FactSeverity", "Severidad"), Value = segment.Severity ?? "-", Hint = T("Logs_Dashboard_FactSeverityHint", "Señal agregada para este bloque.") });
            if (!string.IsNullOrWhiteSpace(segment.PrimarySource))
                facts.Add(new LogStatusFact { Label = T("Logs_Dashboard_FactSource", "Fuente"), Value = segment.PrimarySource, Hint = T("Logs_Dashboard_FactSourceHint", "Origen principal de la señal narrativa.") });
            if (segment.ObservableCount > 0)
                facts.Add(new LogStatusFact { Label = T("Logs_Dashboard_FactEvents", "Eventos"), Value = segment.ObservableCount.ToString("N0"), Hint = T("Logs_Dashboard_FactEventsHint", "Cantidad observable en el rango y filtros actuales.") });
            if (!string.IsNullOrWhiteSpace(duration))
                facts.Add(new LogStatusFact { Label = T("Logs_Dashboard_DurationTitle", "Duracion"), Value = duration, Hint = T("Logs_Dashboard_FactDurationHint", "Duración del incidente/degradación cuando aplica.") });

            if (segment.Detail?.Facts != null)
                facts.AddRange(segment.Detail.Facts.Where(x => x != null));

            var related = segment.Detail?.RelatedEventIdsOrLabels != null
                ? string.Join(", ", segment.Detail.RelatedEventIdsOrLabels.Where(x => !string.IsNullOrWhiteSpace(x)).Take(6))
                : null;

            DashboardHoverCard = new DashboardHoverCard
            {
                StatusLevel = string.IsNullOrWhiteSpace(segment.StatusLevel) ? "limited" : segment.StatusLevel,
                Eyebrow = string.Format("{0} · {1}", titleDate, segment.BucketLabel ?? T("Logs_Dashboard_HistoricalBlock", "Bloque historico")),
                Title = string.Format("{0} — {1}", titleDate, segment.NarrativeState ?? segment.Severity ?? T("Logs_Dashboard_NarrativeReadingTitle", "Lectura narrativa")),
                StatusIcon = ResolveStatusIcon(segment.StatusLevel, segment.Severity),
                BadgeLabel = string.IsNullOrWhiteSpace(segment.Severity) ? T("Logs_Dashboard_NoSeverity", "Sin severidad") : segment.Severity,
                SecondaryBadge = string.IsNullOrWhiteSpace(segment.NarrativeState) ? (segment.StatusLevel ?? "limited") : segment.NarrativeState,
                Summary = string.IsNullOrWhiteSpace(segment.Summary) ? T("Logs_Dashboard_NoNarrativeSummaryForBlock", "Sin resumen narrativo visible para este bloque.") : segment.Summary,
                Detail = !string.IsNullOrWhiteSpace(segment.Explanation)
                    ? segment.Explanation
                    : !string.IsNullOrWhiteSpace(segment.Detail?.TooltipBody)
                        ? segment.Detail.TooltipBody
                        : T("Logs_Dashboard_HoverNarrativeDetailFallback", "Pasá por los bloques para ver qué pasó en lenguaje humano, con impacto y señales relacionadas."),
                DurationLabel = duration,
                ActionHint = string.IsNullOrWhiteSpace(related)
                    ? T("Logs_Dashboard_HoverReadHint", "Hover para leer (sin desaparecer). Click si querés fijar contexto o bajar a drill-down.")
                    : T("Logs_Dashboard_RelatedPrefix", "Relacionado: ") + related,
                Facts = facts.Take(10).ToArray(),
                RelatedItems = (segment.Detail?.RelatedEventIdsOrLabels ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(6)
                    .ToArray()
            };
        }

        public void PreviewMetricHover(string category, LogMetricDistributionItem item)
        {
            if (item == null)
                return;

            var categoryLabel = ResolveMetricCategoryTitle(category);
            var contextEntries = ResolveMetricHoverEntries(category, item);
            var semantic = ResolveMetricHoverSemantic(category, item, contextEntries);
            var relatedItems = BuildMetricRelatedItems(category, contextEntries);
            var temporalContext = BuildTemporalContextLabel(contextEntries);
            var durationOrVigency = BuildDurationOrVigencyLabel(contextEntries);

            DashboardHoverCard = new DashboardHoverCard
            {
                StatusLevel = semantic.StatusLevel,
                Eyebrow = BuildHoverEyebrow(category, contextEntries),
                Title = string.Format("{0}: {1}", categoryLabel, string.IsNullOrWhiteSpace(item.Label) ? T("Logs_Dashboard_NoDataValue", "Sin dato") : item.Label),
                StatusIcon = ResolveStatusIcon(semantic.StatusLevel, semantic.Severity),
                BadgeLabel = semantic.BadgeLabel,
                SecondaryBadge = semantic.SecondaryBadge,
                Summary = BuildMetricHoverSummary(category, item, contextEntries),
                Detail = BuildMetricHoverDetail(category, item, contextEntries),
                ActionHint = IsMetricCategory(category, LatencyDistributionCategory)
                    ? T("Logs_Dashboard_InformationalHoverHint", "Lectura solo informativa: hover consistente, sin click obligatorio.")
                    : T("Logs_Dashboard_ContextualFilterHoverHint", "Hover para leer; click si querés aplicar filtro contextual a la grilla inferior."),
                DurationLabel = durationOrVigency,
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_FactCategory", "Categoria"), Value = categoryLabel, Hint = T("Logs_Dashboard_FactCategoryHint", "Bloque del dashboard que originó la lectura.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactValue", "Valor"), Value = string.IsNullOrWhiteSpace(item.Label) ? T("Logs_Dashboard_NoDataValue", "Sin dato") : item.Label, Hint = T("Logs_Dashboard_FactValueHint", "Etiqueta principal del bloque en hover.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactEvents", "Eventos"), Value = item.Count.ToString("N0"), Hint = T("Logs_Dashboard_FactEventsHint", "Cantidad visible en el rango y filtros actuales.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactTemporalContext", "Contexto temporal"), Value = temporalContext, Hint = T("Logs_Dashboard_FactTemporalContextHint", "Si el bloque es agregado, se muestra la ultima señal o el rango real sin inventar timestamps.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactReading", "Lectura"), Value = semantic.SecondaryBadge, Hint = semantic.BadgeLabel }
                },
                RelatedItems = relatedItems
            };
        }

        public void PreviewFactHover(string sectionTitle, LogStatusFact fact)
        {
            if (fact == null)
                return;

            var section = StatusSections.FirstOrDefault(x => string.Equals(x.Title, sectionTitle, StringComparison.OrdinalIgnoreCase));
            var primarySegment = ResolvePrimarySectionSegment(section);
            var relatedItems = BuildFactRelatedItems(section, primarySegment, fact);
            var temporalContext = BuildSectionTemporalContext(section, primarySegment);

            DashboardHoverCard = new DashboardHoverCard
            {
                StatusLevel = section?.StatusLevel ?? "review",
                Eyebrow = BuildSectionHoverEyebrow(sectionTitle, primarySegment),
                Title = string.IsNullOrWhiteSpace(sectionTitle)
                    ? fact.Label ?? T("Logs_Dashboard_FactCardFallback", "Fact del dashboard")
                    : string.Format("{0}: {1}", sectionTitle, fact.Label),
                StatusIcon = ResolveStatusIcon(section?.StatusLevel ?? "review", primarySegment?.Severity),
                BadgeLabel = string.IsNullOrWhiteSpace(section?.StatusLabel) ? (fact.Label ?? T("Logs_Dashboard_FactCardShortFallback", "Fact")) : section.StatusLabel,
                SecondaryBadge = !string.IsNullOrWhiteSpace(primarySegment?.NarrativeState) ? primarySegment.NarrativeState : (fact.Label ?? T("Logs_Dashboard_HoverShortLabel", "Hover")),
                Summary = string.IsNullOrWhiteSpace(fact.Value) ? T("Logs_Dashboard_NoVisibleValue", "Sin valor visible.") : fact.Value,
                Detail = BuildFactHoverDetail(section, primarySegment, fact),
                DurationLabel = !string.IsNullOrWhiteSpace(primarySegment?.DurationLabel)
                    ? primarySegment.DurationLabel
                    : BuildSectionDurationOrVigency(section, primarySegment),
                ActionHint = T("Logs_Dashboard_ConsistentHoverHint", "Hover consistente; no necesitás click para leer el contexto de este bloque."),
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_FactBlock", "Bloque"), Value = fact.Label ?? T("Logs_Dashboard_NoLabel", "Sin etiqueta"), Hint = T("Logs_Dashboard_FactInspectedNameHint", "Nombre del fact inspeccionado.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactValue", "Valor"), Value = fact.Value ?? T("Logs_Dashboard_NoDataValue", "Sin dato"), Hint = T("Logs_Dashboard_FactVisibleValueHint", "Valor visible del bloque.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactTemporalContext", "Contexto temporal"), Value = temporalContext, Hint = T("Logs_Dashboard_FactPrioritizedNarrativeHint", "Se prioriza la ultima narrativa observable del bloque; si no existe, se muestra el rango filtrado.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_FactState", "Estado"), Value = !string.IsNullOrWhiteSpace(primarySegment?.NarrativeState) ? primarySegment.NarrativeState : (section?.StatusLabel ?? T("Logs_Dashboard_NoState", "Sin estado")), Hint = section?.Summary ?? fact.Hint }
                },
                RelatedItems = relatedItems
            };
        }

        public void ClearDashboardHoverCard()
        {
            DashboardHoverCard = BuildDefaultHoverCard();
        }

        public void StopAutoRefresh()
        {
            if (_autoRefreshTimer.IsEnabled)
                _autoRefreshTimer.Stop();
        }

        public void Refresh()
        {
            try
            {
                _isRefreshingFilters = true;
                EnsureDefaultContext();

                var snapshot = _dashboardService.GetDashboardSnapshot(BuildQuery());
                RebuildFilterOptions(snapshot);
                ApplySnapshot(snapshot);
                _isRefreshingFilters = false;
            }
            catch (Exception ex)
            {
                _isRefreshingFilters = false;
                ResetMetrics();
                StatusMessage = LocalizedText.Get("Logs_LoadErrorStatus", "No fue posible cargar los logs.");
                UltimoError = ex.Message;
            }
        }

        public override void RefreshLocalization()
        {
            BuildTimeRanges();
            Refresh();
        }

        public void Dispose()
        {
            StopAutoRefresh();
            _autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
            _eventRefreshTimer.Tick -= EventRefreshTimer_Tick;
            LogService.LogWritten -= LogService_LogWritten;
        }

        private void BuildTimeRanges()
        {
            AvailableTimeRanges.Clear();
            AvailableTimeRanges.Add(new LogTimeRangeOption { Key = "2h", DisplayName = T("Logs_TimeRange_RealTime2Hours", "Tiempo real (ultimas 2 horas)") });
            AvailableTimeRanges.Add(new LogTimeRangeOption { Key = "24h", DisplayName = T("Logs_TimeRange_Last24Hours", "Ultimas 24 horas") });
            AvailableTimeRanges.Add(new LogTimeRangeOption { Key = "7d", DisplayName = T("Logs_TimeRange_Last7Days", "Ultimos 7 dias") });
            AvailableTimeRanges.Add(new LogTimeRangeOption { Key = "30d", DisplayName = T("Logs_TimeRange_Last30Days", "Ultimos 30 dias") });
            AvailableTimeRanges.Add(new LogTimeRangeOption { Key = "all", DisplayName = T("Logs_TimeRange_AllHistory", "Todo el historico") });
            _selectedTimeRangeOption = ResolveTimeRangeOption(AvailableTimeRanges, _selectedTimeRangeKey);
            RaisePropertyChanges(nameof(SelectedTimeRangeKey), nameof(SelectedTimeRangeOption));
        }

        private void RebuildFilterOptions(LogDashboardSnapshot snapshot)
        {
            ReplaceOptions(AvailableMachines, BuildOptionItems(snapshot.AvailableMachines, T("Logs_Filter_AllMachines", "Todos los equipos"), _selectedMachineFilter));
            ReplaceOptions(AvailableSeverities, BuildOptionItems(snapshot.AvailableLevels, T("Logs_Filter_AllSeverities", "Todas las severidades"), _selectedSeverityFilter));
            ReplaceOptions(AvailableSources, BuildOptionItems(snapshot.AvailableSources, T("Logs_Filter_AllSources", "Todos los modulos"), _selectedSourceFilter));
            ReplaceOptions(AvailableUsers, BuildOptionItems(snapshot.AvailableUsers, T("Logs_Filter_AllUsers", "Todos los usuarios"), _selectedUserFilter));

            _selectedMachineFilter = EnsureSelection(AvailableMachines, _selectedMachineFilter);
            _selectedSeverityFilter = EnsureSelection(AvailableSeverities, _selectedSeverityFilter);
            _selectedSourceFilter = EnsureSelection(AvailableSources, _selectedSourceFilter);
            _selectedUserFilter = EnsureSelection(AvailableUsers, _selectedUserFilter);
            _selectedMachineOption = ResolveOption(AvailableMachines, _selectedMachineFilter);
            _selectedSeverityOption = ResolveOption(AvailableSeverities, _selectedSeverityFilter);
            _selectedSourceOption = ResolveOption(AvailableSources, _selectedSourceFilter);
            _selectedUserOption = ResolveOption(AvailableUsers, _selectedUserFilter);

            RaisePropertyChanges(
                nameof(SelectedMachineFilter),
                nameof(SelectedMachineOption),
                nameof(SelectedSeverityFilter),
                nameof(SelectedSeverityOption),
                nameof(SelectedSourceFilter),
                nameof(SelectedSourceOption),
                nameof(SelectedUserFilter),
                nameof(SelectedUserOption));
        }

        private void ApplySnapshot(LogDashboardSnapshot snapshot)
        {
            var summary = snapshot.Summary ?? new LogDashboardSummary();
            ReplaceItems(ErrorSeries, snapshot.ErrorSeries ?? Array.Empty<LogMetricPoint>());
            ReplaceItems(WarningSeries, snapshot.WarningSeries ?? Array.Empty<LogMetricPoint>());
            ReplaceItems(SourceActivity, snapshot.SourceActivity ?? Array.Empty<LogMetricDistributionItem>());
            ReplaceItems(UserActivity, snapshot.UserActivity ?? Array.Empty<LogMetricDistributionItem>());
            ReplaceItems(MachineActivity, snapshot.MachineActivity ?? Array.Empty<LogMetricDistributionItem>());
            ReplaceItems(LatencyDistribution, snapshot.LatencyDistribution ?? Array.Empty<LogMetricDistributionItem>());
            ReplaceItems(StatusSections, snapshot.StatusSections ?? Array.Empty<LogDashboardSection>());
            _latestSnapshot = snapshot;
            ReconcileNarrativeSelection(snapshot.StatusSections ?? Array.Empty<LogDashboardSection>());
            RefreshStatusSectionsVisualState();
            ApplyNarrativeDrillDownToViews(snapshot);

            TotalEventos = summary.TotalEvents;
            TotalErrores = summary.ErrorCount;
            TotalAdvertencias = summary.WarningCount;
            TotalEquipos = summary.MachinesCount;
            LatenciaPromedio = summary.AverageLatencyMs <= 0
                ? LocalizedText.Get("Logs_NoData", "Sin datos")
                : summary.AverageLatencyMs.ToString("N0") + " ms";
            LatenciaPercentil95 = summary.P95LatencyMs <= 0
                ? LocalizedText.Get("Logs_NoData", "Sin datos")
                : summary.P95LatencyMs.ToString("N0") + " ms";
            UltimoError = summary.LatestError == null
                ? LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes")
                : summary.LatestError.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " - " + summary.LatestError.Message;
            InstrumentationStatus = snapshot.InstrumentationStatus ?? LocalizedText.Get("Logs_NoData", "Sin datos");

            var executiveStatus = snapshot.ExecutiveStatus ?? new LogExecutiveStatus();
            ExecutiveStatusTitle = executiveStatus.Title ?? T("Logs_Dashboard_ExecutiveStatusTitle", "Estado ejecutivo del rango filtrado");
            ExecutiveStatusLevel = executiveStatus.StatusLevel ?? "limited";
            ExecutiveStatusLabel = executiveStatus.StatusLabel ?? T("Logs_Dashboard_PartialCoverage", "Cobertura parcial");
            ExecutiveStatusSummary = executiveStatus.Summary ?? T("Logs_Dashboard_NoSummaryAvailable", "Sin resumen disponible.");
            ExecutiveStatusDetail = executiveStatus.Detail ?? T("Logs_Dashboard_NoDetailAvailable", "Sin detalle disponible.");
            ExecutiveWindowLabel = executiveStatus.WindowLabel ?? T("Logs_TimeRange_Last7Days", "Ultimos 7 dias");
            ExecutiveCoverageLabel = executiveStatus.CoverageLabel ?? T("Logs_Dashboard_ObservableCoverageFallback", "Cobertura observable: 0/5 senales base.");
            ExecutiveLifeSignLabel = executiveStatus.LifeSignLabel ?? T("Logs_Dashboard_NoRecentSignal", "Sin senal reciente");
            ExecutiveLifeSignDetail = executiveStatus.LifeSignDetail ?? T("Logs_Dashboard_NoHealthActivityObservable", "Sin actividad health observable.");
            ExecutiveHeartbeatLabel = executiveStatus.HeartbeatLabel ?? T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable");
            ExecutiveHeartbeatDetail = executiveStatus.HeartbeatDetail ?? T("Logs_Dashboard_NoHeartbeatInBaseWindow", "Sin heartbeat en la ventana base.");
            ExecutiveDependencyLabel = executiveStatus.DependencyLabel ?? T("Logs_Dashboard_NoCoverage", "Sin cobertura");
            ExecutiveDependencyDetail = executiveStatus.DependencyDetail ?? T("Logs_Dashboard_NoDependenciesObservable", "Sin dependencias observables.");
            ExecutiveIncidentLabel = executiveStatus.IncidentLabel ?? T("Logs_Dashboard_NoRecentHealthIncidents", "Sin incidentes health recientes");
            ExecutiveIncidentDetail = executiveStatus.IncidentDetail ?? T("Logs_Dashboard_NoVisibleHealthDegradations", "Sin degradaciones health visibles.");

            ErrorSeriesMax = CalculateMax(ErrorSeries.Select(x => x.Count));
            WarningSeriesMax = CalculateMax(WarningSeries.Select(x => x.Count));
            SourceActivityMax = CalculateMax(SourceActivity.Select(x => x.Count));
            UserActivityMax = CalculateMax(UserActivity.Select(x => x.Count));
            MachineActivityMax = CalculateMax(MachineActivity.Select(x => x.Count));
            LatencyDistributionMax = CalculateMax(LatencyDistribution.Select(x => x.Count));

            StatusMessage = BuildStatusMessage(snapshot);
        }

        private void ClearFilters()
        {
            _isRefreshingFilters = true;
            SelectedMachineFilter = AllFilterValue;
            SelectedTimeRangeKey = "7d";
            SelectedSeverityFilter = AllFilterValue;
            SelectedSourceFilter = AllFilterValue;
            SelectedUserFilter = AllFilterValue;
            SearchText = string.Empty;
            _isRefreshingFilters = false;
            ClearIncidentTimelineDrillDownContext();
            Refresh();
        }

        private void ApplySourceFilter(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return;

            SelectedSourceFilter = source;
        }

        private void ApplyUserFilter(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                return;

            SelectedUserFilter = user;
        }

        private void ApplyMachineFilter(string machine)
        {
            if (string.IsNullOrWhiteSpace(machine))
                return;

            SelectedMachineFilter = machine;
        }

        private void ApplyIncidentDrillDown(AppLogEntry entry)
        {
            if (entry == null)
                return;

            CaptureIncidentTimelineDrillDownContext();
            _isRefreshingFilters = true;

            if (!string.IsNullOrWhiteSpace(entry.Level))
                SelectedSeverityFilter = entry.Level;

            if (!string.IsNullOrWhiteSpace(entry.Source))
                SelectedSourceFilter = entry.Source;

            if (!string.IsNullOrWhiteSpace(entry.UserName))
                SelectedUserFilter = entry.UserName;

            if (!string.IsNullOrWhiteSpace(entry.MachineName))
                SelectedMachineFilter = entry.MachineName;

            _isRefreshingFilters = false;
            _hasActiveIncidentTimelineDrillDown = true;
            OnPropertyChanged(nameof(HasTimelineDrillDownContext));
            Refresh();
        }

        private void SelectNarrativeSegment(LogStatusTimelineSegment segment)
        {
            if (segment == null)
                return;

            SelectedNarrativeSegmentId = segment.SegmentId;
            ReconcileNarrativeSelection(StatusSections);
            RefreshStatusSectionsVisualState();
            NarrativeSelectionStatus = string.Format(
                T("Logs_Dashboard_ContextPinnedFormat", "Contexto fijado en {0}: {1}. Usa 'Ver eventos relacionados' para bajar el drill-down sin perder la lectura superior."),
                segment.BucketLabel,
                segment.Summary);
            RaiseNarrativeContextProperties();
        }

        private void ApplyNarrativeSegmentDrillDown(LogStatusTimelineSegment segment)
        {
            if (segment == null)
                return;

            SelectedNarrativeSegmentId = segment.SegmentId;
            _activeNarrativeDrillDown = segment.DrillDown;
            ReconcileNarrativeSelection(StatusSections);
            RefreshStatusSectionsVisualState();
            ApplyNarrativeDrillDownToViews(_latestSnapshot);
            OnPropertyChanged(nameof(HasActiveNarrativeDrillDown));
            OnPropertyChanged(nameof(HasTimelineDrillDownContext));
            NarrativeSelectionStatus = string.Format(
                T("Logs_Dashboard_DrillDownActiveFormat", "Drill-down activo para {0} ({1} - {2}). La lista inferior y la grilla ahora reflejan este segmento."),
                segment.Summary,
                segment.Start.ToString("yyyy-MM-dd HH:mm"),
                segment.End.ToString("yyyy-MM-dd HH:mm"));
            RaiseNarrativeContextProperties();
        }

        private void ClearNarrativeSelection()
        {
            SelectedNarrativeSegmentId = null;
            SelectedNarrativeSegment = null;
            _activeNarrativeDrillDown = null;
            ReconcileNarrativeSelection(StatusSections);
            RefreshStatusSectionsVisualState();
            ApplyNarrativeDrillDownToViews(_latestSnapshot);
            OnPropertyChanged(nameof(HasActiveNarrativeDrillDown));
            OnPropertyChanged(nameof(HasTimelineDrillDownContext));
            NarrativeSelectionStatus = T("Logs_Dashboard_NarrativeContextCleared", "Contexto narrativo limpio. El dashboard vuelve a mostrar solo los filtros generales activos.");
            RaiseNarrativeContextProperties();
        }

        private void ClearTimelineDrillDown()
        {
            if (HasActiveNarrativeDrillDown)
                ClearNarrativeSelection();

            if (!_hasActiveIncidentTimelineDrillDown)
                return;

            _isRefreshingFilters = true;
            SelectedMachineFilter = string.IsNullOrWhiteSpace(_incidentDrillDownMachineFilter) ? AllFilterValue : _incidentDrillDownMachineFilter;
            SelectedSeverityFilter = string.IsNullOrWhiteSpace(_incidentDrillDownSeverityFilter) ? AllFilterValue : _incidentDrillDownSeverityFilter;
            SelectedSourceFilter = string.IsNullOrWhiteSpace(_incidentDrillDownSourceFilter) ? AllFilterValue : _incidentDrillDownSourceFilter;
            SelectedUserFilter = string.IsNullOrWhiteSpace(_incidentDrillDownUserFilter) ? AllFilterValue : _incidentDrillDownUserFilter;
            _isRefreshingFilters = false;

            ClearIncidentTimelineDrillDownContext();
            Refresh();
        }

        private void CaptureIncidentTimelineDrillDownContext()
        {
            if (_hasActiveIncidentTimelineDrillDown)
                return;

            _incidentDrillDownMachineFilter = SelectedMachineFilter;
            _incidentDrillDownSeverityFilter = SelectedSeverityFilter;
            _incidentDrillDownSourceFilter = SelectedSourceFilter;
            _incidentDrillDownUserFilter = SelectedUserFilter;
        }

        private void ClearIncidentTimelineDrillDownContext()
        {
            _hasActiveIncidentTimelineDrillDown = false;
            _incidentDrillDownMachineFilter = null;
            _incidentDrillDownSeverityFilter = null;
            _incidentDrillDownSourceFilter = null;
            _incidentDrillDownUserFilter = null;
            OnPropertyChanged(nameof(HasTimelineDrillDownContext));
        }

        private void EnsureDefaultContext()
        {
            if (_hasAppliedDefaultContext)
                return;

            _hasAppliedDefaultContext = true;
            if (string.IsNullOrWhiteSpace(_currentDeveloperUser))
                return;

            var latestEntry = _dashboardService.GetLatestEntryForUser(_currentDeveloperUser);
            if (latestEntry == null || string.IsNullOrWhiteSpace(latestEntry.MachineName))
                return;

            _selectedMachineFilter = latestEntry.MachineName;
            OnPropertyChanged(nameof(SelectedMachineFilter));
        }

        private LogDashboardQuery BuildQuery()
        {
            return new LogDashboardQuery
            {
                MachineName = SelectedMachineFilter,
                TimeRangeKey = SelectedTimeRangeKey,
                Severity = SelectedSeverityFilter,
                Source = SelectedSourceFilter,
                UserName = SelectedUserFilter,
                SearchText = SearchText
            };
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (IsInteractiveHoverActive)
            {
                _refreshDeferredWhileHover = true;
                return;
            }

            _refreshDeferredWhileHover = false;
            Refresh();
        }

        private void EventRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!_eventRefreshPending)
            {
                _eventRefreshTimer.Stop();
                return;
            }

            if (IsInteractiveHoverActive)
            {
                _refreshDeferredWhileHover = true;
                return;
            }

            _eventRefreshPending = false;
            _refreshDeferredWhileHover = false;
            _eventRefreshTimer.Stop();
            RefreshIfNeeded();
        }

        private void LogService_LogWritten(AppLogEntry entry)
        {
            if (!IsRealTimeView)
                return;

            _eventRefreshPending = true;

            // Siempre programar en el dispatcher de UI para no tocar timers desde hilos de background.
            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                if (!_eventRefreshTimer.IsEnabled)
                    _eventRefreshTimer.Start();
            }));
        }

        private void SetDashboardViewMode(string modeKey)
        {
            var normalized = string.IsNullOrWhiteSpace(modeKey) ? "realtime" : modeKey.Trim();
            if (!string.Equals(normalized, "realtime", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "history", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "realtime";
            }

            if (string.Equals(_dashboardViewModeKey, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            // Guardar/restaurar rango por modo.
            if (string.Equals(_dashboardViewModeKey, "realtime", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(_selectedTimeRangeKey, "2h", StringComparison.OrdinalIgnoreCase))
                    _lastHistoryRangeKey = _selectedTimeRangeKey;
            }
            else
            {
                _lastHistoryRangeKey = _selectedTimeRangeKey;
            }

            _dashboardViewModeKey = normalized;
            OnPropertyChanged(nameof(DashboardViewModeKey));
            OnPropertyChanged(nameof(IsRealTimeView));
            OnPropertyChanged(nameof(IsHistoryView));

            if (IsRealTimeView)
                SelectedTimeRangeKey = "2h";
            else
                SelectedTimeRangeKey = string.IsNullOrWhiteSpace(_lastHistoryRangeKey) ? "7d" : _lastHistoryRangeKey;

            RefreshIfNeeded();
        }

        private void RefreshIfNeeded()
        {
            if (!_isRefreshingFilters)
                Refresh();
        }

        private void ResetMetrics()
        {
            _latestSnapshot = null;
            _activeNarrativeDrillDown = null;
            SelectedNarrativeSegmentId = null;
            SelectedNarrativeSegment = null;
            OnPropertyChanged(nameof(HasActiveNarrativeDrillDown));
            _hoveredNarrativeSegment = null;
            _isNarrativeHoverSourceActive = false;
            _isNarrativeHoverContextActive = false;
            _isDashboardHoverSourceActive = false;
            _isDashboardHoverCardActive = false;
            _refreshDeferredWhileHover = false;
            _hasActiveIncidentTimelineDrillDown = false;
            _incidentDrillDownMachineFilter = null;
            _incidentDrillDownSeverityFilter = null;
            _incidentDrillDownSourceFilter = null;
            _incidentDrillDownUserFilter = null;
            DashboardHoverCard = BuildDefaultHoverCard();
            OnPropertyChanged(nameof(HasTimelineDrillDownContext));
            FilteredEntries.Clear();
            ErrorSeries.Clear();
            WarningSeries.Clear();
            SourceActivity.Clear();
            UserActivity.Clear();
            MachineActivity.Clear();
            LatencyDistribution.Clear();
            RecentCriticalEvents.Clear();
            StatusSections.Clear();
            TotalEventos = 0;
            TotalErrores = 0;
            TotalAdvertencias = 0;
            TotalEquipos = 0;
            LatenciaPromedio = LocalizedText.Get("Logs_NoData", "Sin datos");
            LatenciaPercentil95 = LocalizedText.Get("Logs_NoData", "Sin datos");
            InstrumentationStatus = LocalizedText.Get("Logs_NoData", "Sin datos");
            UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
            ExecutiveStatusTitle = T("Logs_Dashboard_ExecutiveStatusTitle", "Estado ejecutivo del rango filtrado");
            ExecutiveStatusLevel = "limited";
            ExecutiveStatusLabel = T("Logs_Dashboard_PartialCoverage", "Cobertura parcial");
            ExecutiveStatusSummary = T("Logs_Dashboard_NoVisibleDataForFilter", "Sin datos visibles para el filtro actual.");
            ExecutiveStatusDetail = T("Logs_Dashboard_ExistingLogsOnlyDetail", "El resumen depende de logs existentes; no representa uptime.");
            ExecutiveWindowLabel = T("Logs_TimeRange_Last7Days", "Ultimos 7 dias");
            ExecutiveCoverageLabel = T("Logs_Dashboard_ObservableCoverageFallback", "Cobertura observable: 0/5 senales base.");
            ExecutiveLifeSignLabel = T("Logs_Dashboard_NoRecentSignal", "Sin senal reciente");
            ExecutiveLifeSignDetail = T("Logs_Dashboard_WaitingHealthActivity", "Esperando actividad health observable.");
            ExecutiveHeartbeatLabel = T("Logs_Dashboard_NoHeartbeatObservable", "Sin heartbeat observable");
            ExecutiveHeartbeatDetail = T("Logs_Dashboard_HeartbeatAliveOnly", "La app solo reporta heartbeat mientras esta viva.");
            ExecutiveDependencyLabel = T("Logs_Dashboard_NoCoverage", "Sin cobertura");
            ExecutiveDependencyDetail = T("Logs_Dashboard_NoDependenciesObservableYet", "Todavia no hay dependencias observables.");
            ExecutiveIncidentLabel = T("Logs_Dashboard_NoRecentHealthIncidents", "Sin incidentes health recientes");
            ExecutiveIncidentDetail = T("Logs_Dashboard_NoVisibleHealthDegradations", "No hay degradaciones health visibles.");
            NarrativeSelectionStatus = T("Logs_Dashboard_NoNarrativeContextYet", "Sin contexto narrativo disponible por ahora.");
            RaiseNarrativeContextProperties();
        }

        private string BuildStatusMessage(LogDashboardSnapshot snapshot)
        {
            var entriesCount = (snapshot.Entries ?? Array.Empty<AppLogEntry>()).Count;
            var machineText = ResolveFilterDisplay(AvailableMachines, SelectedMachineFilter, T("Logs_Filter_AllMachines_Lower", "todos los equipos"));
            var severityText = ResolveFilterDisplay(AvailableSeverities, SelectedSeverityFilter, T("Logs_Filter_AllSeverities_Lower", "todas las severidades"));
            var sourceText = ResolveFilterDisplay(AvailableSources, SelectedSourceFilter, T("Logs_Filter_AllSources_Lower", "todos los modulos"));
            var userText = ResolveFilterDisplay(AvailableUsers, SelectedUserFilter, T("Logs_Filter_AllUsers_Lower", "todos los usuarios"));
            var rangeText = AvailableTimeRanges.FirstOrDefault(x => string.Equals(x.Key, SelectedTimeRangeKey, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? T("Logs_TimeRange_Last7Days", "Ultimos 7 dias");
            var searchText = string.IsNullOrWhiteSpace(SearchText) ? T("Logs_Dashboard_NoFreeSearch", "sin busqueda libre") : string.Format(T("Logs_Dashboard_SearchFormat", "busqueda '{0}'"), SearchText.Trim());

            var narrativeSuffix = _activeNarrativeDrillDown == null
                ? T("Logs_Dashboard_NoNarrativeDrillDownActive", "Sin drill-down narrativo activo.")
                : string.Format(T("Logs_Dashboard_NarrativeDrillDownActiveFormat", "Drill-down narrativo activo: {0} entre {1:yyyy-MM-dd HH:mm} y {2:yyyy-MM-dd HH:mm}."),
                    SelectedNarrativeSegment?.Summary ?? _activeNarrativeDrillDown.PrimaryEventType,
                    _activeNarrativeDrillDown.RangeStart,
                    _activeNarrativeDrillDown.RangeEnd);

            return string.Format(
                T("Logs_Dashboard_StatusMessageFormat", "Mostrando {0} eventos para {1}, {2}, {3}, {4}, {5} y {6}. Logs: {7}"),
                entriesCount,
                rangeText,
                machineText,
                severityText,
                sourceText,
                userText,
                searchText,
                LogService.PrimaryLogsDirectory) + " " + narrativeSuffix;
        }

        private void ReconcileNarrativeSelection(IEnumerable<LogDashboardSection> sections)
        {
            var allSegments = (sections ?? Array.Empty<LogDashboardSection>())
                .SelectMany(x => x.TimelineSegments ?? Array.Empty<LogStatusTimelineSegment>())
                .ToList();
            var selected = string.IsNullOrWhiteSpace(SelectedNarrativeSegmentId)
                ? null
                : allSegments.FirstOrDefault(x => string.Equals(x.SegmentId, SelectedNarrativeSegmentId, StringComparison.Ordinal));

            foreach (var segment in allSegments)
                segment.IsSelected = selected != null && string.Equals(segment.SegmentId, selected.SegmentId, StringComparison.Ordinal);

            if (selected != null)
            {
                SelectedNarrativeSegment = selected;
                RaiseNarrativeContextProperties();
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectedNarrativeSegmentId))
            {
                SelectedNarrativeSegmentId = null;
                SelectedNarrativeSegment = null;
                _activeNarrativeDrillDown = null;
                OnPropertyChanged(nameof(HasActiveNarrativeDrillDown));
                OnPropertyChanged(nameof(HasTimelineDrillDownContext));
                NarrativeSelectionStatus = T("Logs_Dashboard_SelectedSegmentMissing", "El segmento seleccionado ya no existe en la ventana actual. Se limpio el contexto para no remapearte a otro incidente cualquiera.");
                RaiseNarrativeContextProperties();
                return;
            }

            SelectedNarrativeSegment = null;
            RaiseNarrativeContextProperties();
        }

        private void ApplyNarrativeDrillDownToViews(LogDashboardSnapshot snapshot)
        {
            var safeSnapshot = snapshot ?? new LogDashboardSnapshot();
            var visibleEntries = ApplySegmentDrillDown(safeSnapshot.Entries ?? Array.Empty<AppLogEntry>(), _activeNarrativeDrillDown)
                .Take(250)
                .ToList();
            var visibleTimeline = ApplySegmentDrillDown(safeSnapshot.TimelineEvents ?? safeSnapshot.CriticalEvents ?? Array.Empty<AppLogEntry>(), _activeNarrativeDrillDown)
                .Take(50)
                .ToList();

            ReplaceItems(FilteredEntries, visibleEntries);
            ReplaceItems(RecentCriticalEvents, visibleTimeline);
            ReplaceItems(IncidentDays, BuildIncidentDays(visibleTimeline));
        }

        private void RefreshStatusSectionsVisualState()
        {
            var sections = StatusSections.ToList();
            ReplaceItems(StatusSections, sections);
        }

        private static IReadOnlyList<IncidentDayGroup> BuildIncidentDays(IEnumerable<AppLogEntry> eventsTimeline)
        {
            var timeline = (eventsTimeline ?? Array.Empty<AppLogEntry>())
                .Where(x => x != null)
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            return timeline
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key)
                .Take(14)
                .Select(day => new IncidentDayGroup
                {
                    Date = day.Key,
                    Items = day
                        .OrderByDescending(x => x.Timestamp)
                        .Take(12)
                        .ToList()
                })
                .ToList();
        }

        private static IReadOnlyList<AppLogEntry> ApplySegmentDrillDown(IEnumerable<AppLogEntry> entries, LogSegmentDrillDownContext context)
        {
            IEnumerable<AppLogEntry> filtered = entries ?? Array.Empty<AppLogEntry>();
            if (context == null)
                return filtered.OrderByDescending(x => x.Timestamp).ToList();

            filtered = filtered.Where(x => x.Timestamp >= context.RangeStart && x.Timestamp < context.RangeEnd);

            if (!string.IsNullOrWhiteSpace(context.Source))
                filtered = filtered.Where(x => string.Equals(x.Source ?? string.Empty, context.Source, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(context.MachineName) && !IsNoDataValue(context.MachineName))
                filtered = filtered.Where(x => string.Equals(x.MachineName ?? string.Empty, context.MachineName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(context.UserName) && !IsNoDataValue(context.UserName))
                filtered = filtered.Where(x => string.Equals(x.UserName ?? string.Empty, context.UserName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(context.Dependency))
                filtered = filtered.Where(x => ContainsText(x.Details, "dependency=" + context.Dependency));

            if (!string.IsNullOrWhiteSpace(context.State) && !IsNoDataValue(context.State))
                filtered = filtered.Where(x => ContainsText(x.Details, "state=" + context.State) || string.Equals(x.Level ?? string.Empty, context.State, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(context.Severity) &&
                !string.Equals(context.Severity, "INFO", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Severity, "HEALTH", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Severity, "LATENCY", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(x => string.Equals(x.Level ?? string.Empty, context.Severity, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(context.Severity, "LATENCY", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(x => (x.DurationMs ?? 0L) >= 1000 || x.DurationMs.HasValue);

            if (string.Equals(context.Severity, "HEALTH", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(IsHealthSignal);

            if (string.Equals(context.Severity, "VALIDATION", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(IsValidationSignal);

            if (!string.IsNullOrWhiteSpace(context.SearchText) && !IsNoDataValue(context.SearchText))
                filtered = filtered.Where(x => ContainsText(x.Message, context.SearchText) || ContainsText(x.Details, context.SearchText));

            return filtered.OrderByDescending(x => x.Timestamp).ToList();
        }

        private static bool ContainsText(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   !string.IsNullOrWhiteSpace(search) &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsHealthSignal(AppLogEntry entry)
        {
            if (entry == null)
                return false;

            return string.Equals(entry.Level, "HEALTH", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entry.Source, "App", StringComparison.OrdinalIgnoreCase) ||
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

        private static void ReplaceOptions(ObservableCollection<LogFilterOption> target, IEnumerable<LogFilterOption> items)
        {
            var desired = (items ?? Array.Empty<LogFilterOption>())
                .Where(x => x != null)
                .ToList();

            for (var index = 0; index < desired.Count; index++)
            {
                var desiredValue = desired[index].Value ?? string.Empty;
                var currentIndex = FindOptionIndex(target, desiredValue, index);

                if (currentIndex < 0)
                {
                    target.Insert(index, desired[index]);
                    continue;
                }

                if (currentIndex != index)
                    target.Move(currentIndex, index);
            }

            for (var index = target.Count - 1; index >= desired.Count; index--)
                target.RemoveAt(index);
        }

        private static IEnumerable<LogFilterOption> BuildOptionItems(IEnumerable<string> values, string allLabel, string selectedValue = null)
        {
            yield return new LogFilterOption { Value = AllFilterValue, DisplayName = allLabel };

            var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                AllFilterValue
            };

            var normalizedSelection = (selectedValue ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalizedSelection) &&
                !string.Equals(normalizedSelection, AllFilterValue, StringComparison.OrdinalIgnoreCase) &&
                seenValues.Add(normalizedSelection))
            {
                // Keep the active filter visible even when the latest snapshot has no matches for it.
                yield return new LogFilterOption { Value = normalizedSelection, DisplayName = normalizedSelection };
            }

            foreach (var value in (values ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var normalizedValue = value.Trim();
                if (!seenValues.Add(normalizedValue))
                    continue;

                yield return new LogFilterOption
                {
                    Value = normalizedValue,
                    DisplayName = normalizedValue
                };
            }
        }

        private static string EnsureSelection(ObservableCollection<LogFilterOption> options, string currentValue)
        {
            if (options.Any(x => string.Equals(x.Value, currentValue, StringComparison.OrdinalIgnoreCase)))
                return currentValue;

            return options.FirstOrDefault()?.Value ?? AllFilterValue;
        }

        private LogFilterOption ResolveSelectedOption(
            ObservableCollection<LogFilterOption> options,
            LogFilterOption candidate,
            string currentValue)
        {
            var resolved = ResolveOption(options, candidate?.Value);
            if (resolved != null)
                return resolved;

            if (_isRefreshingFilters)
                return null;

            return ResolveOption(options, EnsureSelection(options, currentValue));
        }

        private LogTimeRangeOption ResolveSelectedTimeRangeOption(
            ObservableCollection<LogTimeRangeOption> options,
            LogTimeRangeOption candidate,
            string currentKey)
        {
            var resolved = ResolveTimeRangeOption(options, candidate?.Key);
            if (resolved != null)
                return resolved;

            if (_isRefreshingFilters)
                return null;

            return ResolveTimeRangeOption(options, string.IsNullOrWhiteSpace(currentKey) ? "7d" : currentKey);
        }

        private static LogFilterOption ResolveOption(IEnumerable<LogFilterOption> options, string value)
        {
            return (options ?? Array.Empty<LogFilterOption>())
                .FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
        }

        private static LogTimeRangeOption ResolveTimeRangeOption(IEnumerable<LogTimeRangeOption> options, string value)
        {
            return (options ?? Array.Empty<LogTimeRangeOption>())
                .FirstOrDefault(x => string.Equals(x.Key, value, StringComparison.OrdinalIgnoreCase));
        }

        private static int FindOptionIndex(ObservableCollection<LogFilterOption> options, string value, int startIndex)
        {
            for (var index = startIndex; index < options.Count; index++)
            {
                if (string.Equals(options[index]?.Value, value, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            return -1;
        }

        private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
                target.Add(item);
        }

        private static int CalculateMax(IEnumerable<int> values)
        {
            var max = (values ?? Array.Empty<int>()).DefaultIfEmpty(0).Max();
            return max <= 0 ? 1 : max;
        }

        private static string ResolveFilterDisplay(ObservableCollection<LogFilterOption> options, string value, string fallback)
        {
            return options.FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? fallback;
        }

        private static string ResolveCurrentDeveloperUser()
        {
            return NormalizeDeveloperUser(LogService.ResolveCurrentAuditUserName());
        }

        private static string NormalizeDeveloperUser(string userName)
        {
            var normalized = (userName ?? string.Empty).Trim();
            if (!DeveloperUsers.Contains(normalized))
                return string.Empty;

            return normalized;
        }

        private void RaiseNarrativeContextProperties()
        {
            OnPropertyChanged(nameof(NarrativeContextTitle));
            OnPropertyChanged(nameof(ContextNarrativeSegment));
            OnPropertyChanged(nameof(NarrativeContextEyebrow));
            OnPropertyChanged(nameof(NarrativeContextSeverityBadge));
            OnPropertyChanged(nameof(NarrativeContextStateBadge));
            OnPropertyChanged(nameof(NarrativeContextStatusIcon));
            OnPropertyChanged(nameof(NarrativeContextPeriodLabel));
            OnPropertyChanged(nameof(NarrativeContextDurationLabel));
            OnPropertyChanged(nameof(NarrativeContextRelatedItems));
            OnPropertyChanged(nameof(HasNarrativeContextRelatedItems));
            OnPropertyChanged(nameof(NarrativeContextStatus));
        }

        private bool IsInteractiveHoverActive =>
            _isNarrativeHoverSourceActive ||
            _isNarrativeHoverContextActive ||
            _isDashboardHoverSourceActive ||
            _isDashboardHoverCardActive;

        private void UpdateInteractiveHoverState(Action updateAction)
        {
            var wasActive = IsInteractiveHoverActive;
            updateAction?.Invoke();
            var isActive = IsInteractiveHoverActive;

            if (wasActive && !isActive && _refreshDeferredWhileHover)
            {
                _refreshDeferredWhileHover = false;
                _eventRefreshPending = false;
                if (_eventRefreshTimer.IsEnabled)
                    _eventRefreshTimer.Stop();
                Refresh();
            }
        }

        private static DashboardHoverCard BuildDefaultHoverCard()
        {
            return new DashboardHoverCard
            {
                StatusLevel = "limited",
                Eyebrow = T("Logs_Dashboard_DefaultHoverEyebrow", "Status center contextual"),
                StatusIcon = "•",
                BadgeLabel = T("Logs_Dashboard_HoverShortLabel", "Hover"),
                SecondaryBadge = T("Logs_Dashboard_NoClickRequired", "Sin click obligatorio"),
                Title = T("Logs_Dashboard_HoverReadingTitle", "Lectura dinámica en hover"),
                Summary = T("Logs_Dashboard_DefaultHoverSummary", "Pasá por encima de cualquier segmento o bloque relevante y el contexto queda visible acá sin depender de tooltips cortos."),
                Detail = T("Logs_Dashboard_DefaultHoverDetail", "Los segmentos narrativos muestran detalle vivo en el panel superior. Las distribuciones, facts y bloques auxiliares actualizan esta tarjeta mientras dure el hover."),
                DurationLabel = T("Logs_Dashboard_HoverDurationLabel", "Mientras dure el hover"),
                ActionHint = T("Logs_Dashboard_DefaultHoverActionHint", "Click solo como acción secundaria para filtrar o fijar contexto; leer no debería requerir clicks."),
                Facts = new[]
                {
                    new LogStatusFact { Label = T("Logs_Dashboard_CoverageTitle", "Cobertura"), Value = T("Logs_Dashboard_DefaultHoverCoverageValue", "Segmentos + distribuciones + facts"), Hint = T("Logs_Dashboard_DefaultHoverCoverageHint", "Áreas del dashboard conectadas a la lectura dinámica.") },
                    new LogStatusFact { Label = T("Logs_Dashboard_RefreshTitle", "Refresh"), Value = T("Logs_Dashboard_DefaultHoverRefreshValue", "Pausado mientras hay hover activo"), Hint = T("Logs_Dashboard_DefaultHoverRefreshHint", "Evita que el autorefresh mate el contexto mientras inspeccionás un bloque.") }
                },
                RelatedItems = Array.Empty<string>()
            };
        }

        private static string ResolveHoverStatus(int count)
        {
            if (count <= 0)
                return "limited";

            if (count >= 10)
                return "attention";

            if (count >= 3)
                return "review";

            return "stable";
        }

        private static string ResolveStatusIcon(string statusLevel, string severity)
        {
            if (string.Equals(statusLevel, "attention", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(severity, "ERROR", StringComparison.OrdinalIgnoreCase))
                return "✕";

            if (string.Equals(statusLevel, "review", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(severity, "WARNING", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(severity, "VALIDATION", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(severity, "LATENCY", StringComparison.OrdinalIgnoreCase))
                return "!";

            if (string.Equals(statusLevel, "stable", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(severity, "HEALTH", StringComparison.OrdinalIgnoreCase))
                return "✓";

            return "•";
        }

        private IReadOnlyList<AppLogEntry> ResolveMetricHoverEntries(string category, LogMetricDistributionItem item)
        {
            var entries = (_latestSnapshot?.Entries ?? Array.Empty<AppLogEntry>())
                .Where(x => x != null)
                .ToList();

            if (item == null || entries.Count == 0)
                return entries;

            if (IsMetricCategory(category, SourceActivityCategory))
            {
                var filterValue = string.IsNullOrWhiteSpace(item.FilterValue) ? item.Label : item.FilterValue;
                return entries
                    .Where(x => string.Equals(x.Source ?? string.Empty, filterValue ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (IsMetricCategory(category, UserActivityCategory))
            {
                var filterValue = string.IsNullOrWhiteSpace(item.FilterValue) ? item.Label : item.FilterValue;
                return entries
                    .Where(x => string.Equals(x.UserName ?? string.Empty, filterValue ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (IsMetricCategory(category, MachineActivityCategory))
            {
                var filterValue = string.IsNullOrWhiteSpace(item.FilterValue) ? item.Label : item.FilterValue;
                return entries
                    .Where(x => string.Equals(x.MachineName ?? string.Empty, filterValue ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (IsMetricCategory(category, LatencyDistributionCategory))
            {
                return entries
                    .Where(x => x.DurationMs.HasValue && MatchesLatencyBand(item.Label, x.DurationMs ?? 0L))
                    .ToList();
            }

            return entries;
        }

        private HoverSemanticState ResolveMetricHoverSemantic(string category, LogMetricDistributionItem item, IReadOnlyList<AppLogEntry> contextEntries)
        {
            var count = item?.Count ?? 0;
            if (IsMetricCategory(category, LatencyDistributionCategory))
            {
                if (string.Equals(item?.Label, ">= 2 s", StringComparison.OrdinalIgnoreCase))
                    return new HoverSemanticState("attention", "LATENCY", T("Logs_Dashboard_CriticalLatency", "Latencia critica"), item.Label ?? T("Logs_Dashboard_SlowBand", "Banda lenta"));

                if (string.Equals(item?.Label, "1-2 s", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item?.Label, "500-999 ms", StringComparison.OrdinalIgnoreCase))
                {
                    return new HoverSemanticState("review", "LATENCY", T("Logs_Dashboard_DegradedLatency", "Latencia degradada"), item.Label ?? T("Logs_Dashboard_MidBand", "Banda intermedia"));
                }

                return new HoverSemanticState(count <= 0 ? "limited" : "stable", "LATENCY", count <= 0 ? T("Logs_Dashboard_NoVisibleLatency", "Sin latencia visible") : T("Logs_Dashboard_NominalLatency", "Latencia nominal"), item?.Label ?? T("Logs_Dashboard_DistributionLabel", "Distribucion"));
            }

            var dominantSeverity = ResolveDominantSeverity(contextEntries);
            if (string.Equals(dominantSeverity, "ERROR", StringComparison.OrdinalIgnoreCase))
                return new HoverSemanticState("attention", dominantSeverity, T("Logs_Dashboard_VisibleIncident", "Incidente visible"), T("Logs_Dashboard_DominantError", "ERROR dominante"));

            if (string.Equals(dominantSeverity, "WARNING", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dominantSeverity, "VALIDATION", StringComparison.OrdinalIgnoreCase))
            {
                return new HoverSemanticState("review", dominantSeverity, T("Logs_Dashboard_ActiveFollowUp", "Seguimiento activo"), dominantSeverity + " " + T("Logs_Dashboard_DominantSuffix", "dominante"));
            }

            if (count <= 0)
                return new HoverSemanticState("limited", dominantSeverity, T("Logs_Dashboard_NoActivity", "Sin actividad"), T("Logs_Dashboard_NoSignalEnough", "Sin senal suficiente"));

            return new HoverSemanticState("stable", dominantSeverity, T("Logs_Dashboard_VisibleActivity", "Actividad visible"), dominantSeverity == "INFO" ? T("Logs_Dashboard_ObservableOperation", "Operacion observable") : dominantSeverity + " " + T("Logs_Dashboard_DominantSuffix", "dominante"));
        }

        private string BuildMetricHoverSummary(string category, LogMetricDistributionItem item, IReadOnlyList<AppLogEntry> contextEntries)
        {
            var count = item?.Count ?? 0;
            if (IsMetricCategory(category, LatencyDistributionCategory))
            {
                if (count <= 0)
                    return F("Logs_Dashboard_NoOperationsInBand", "No hay operaciones visibles en la banda {0} dentro del rango filtrado.", item?.Label ?? T("Logs_Dashboard_SelectedBand", "seleccionada"));

                return F("Logs_Dashboard_OperationsInBand", "{0:N0} operaciones visibles caen en la banda {1}.", count, item?.Label ?? T("Logs_Dashboard_SelectedBand", "seleccionada"));
            }

            if (count <= 0)
                return F("Logs_Dashboard_NoVisibleActivityForBlock", "No hay actividad visible para {0} en el rango filtrado.", string.IsNullOrWhiteSpace(item?.Label) ? T("Logs_Dashboard_ThisBlock", "este bloque") : item.Label);

            var lastObserved = contextEntries.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            return lastObserved == null
                ? F("Logs_Dashboard_VisibleEventsForBlock", "{0:N0} eventos visibles para este bloque.", count)
                : F("Logs_Dashboard_VisibleEventsWithLastSignal", "{0:N0} eventos visibles; ultima senal observable {1:yyyy-MM-dd HH:mm}.", count, lastObserved.Timestamp);
        }

        private string BuildMetricHoverDetail(string category, LogMetricDistributionItem item, IReadOnlyList<AppLogEntry> contextEntries)
        {
            if (contextEntries == null || contextEntries.Count == 0)
                return string.IsNullOrWhiteSpace(item?.SecondaryText)
                    ? T("Logs_Dashboard_NoExactDateForAggregate", "No hay una fecha exacta que mostrar para este agregado; solo se confirma que el bloque no tiene actividad visible en el rango filtrado.")
                    : item.SecondaryText + " " + T("Logs_Dashboard_NoVisibleActivityCurrentWindow", "Sin actividad visible en la ventana actual.");

            var firstObserved = contextEntries.OrderBy(x => x.Timestamp).FirstOrDefault();
            var lastObserved = contextEntries.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            var temporalSentence = firstObserved == null || lastObserved == null
                ? string.Empty
                : firstObserved.Timestamp == lastObserved.Timestamp
                    ? F("Logs_Dashboard_LastObservableSignal", "Ultima senal observable: {0:yyyy-MM-dd HH:mm}. ", lastObserved.Timestamp)
                    : F("Logs_Dashboard_ObservableActivityBetween", "Actividad observable entre {0:yyyy-MM-dd HH:mm} y {1:yyyy-MM-dd HH:mm}. ", firstObserved.Timestamp, lastObserved.Timestamp);

            return temporalSentence + (string.IsNullOrWhiteSpace(item?.SecondaryText)
                ? T("Logs_Dashboard_HoverReadingStaysVisible", "La lectura queda fija mientras mantengas hover. El click sigue siendo opcional y solo sirve para filtrar la grilla inferior.")
                : item.SecondaryText);
        }

        private IReadOnlyList<string> BuildMetricRelatedItems(string category, IReadOnlyList<AppLogEntry> contextEntries)
        {
            var entries = contextEntries ?? Array.Empty<AppLogEntry>();
            var related = new List<string>();

            if (IsMetricCategory(category, SourceActivityCategory))
            {
                related.AddRange(entries.Select(x => x.UserName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).Select(x => T("Logs_UserColumn", "Usuario") + ": " + x));
                related.AddRange(entries.Select(x => x.MachineName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).Select(x => T("Logs_MachineLabel", "Equipo") + ": " + x));
            }
            else if (IsMetricCategory(category, UserActivityCategory))
            {
                related.AddRange(entries.Select(x => x.Source).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).Select(x => T("Logs_Dashboard_ModuleFilter", "Modulo") + ": " + x));
                related.AddRange(entries.Select(x => x.MachineName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).Select(x => T("Logs_MachineLabel", "Equipo") + ": " + x));
            }
            else if (IsMetricCategory(category, MachineActivityCategory))
            {
                related.AddRange(entries.Select(x => x.Source).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).Select(x => T("Logs_Dashboard_ModuleFilter", "Modulo") + ": " + x));
                related.AddRange(entries.Select(x => x.UserName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).Select(x => T("Logs_UserColumn", "Usuario") + ": " + x));
            }
            else if (IsMetricCategory(category, LatencyDistributionCategory))
            {
                related.AddRange(entries.Select(x => x.Source).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).Select(x => T("Logs_Dashboard_ModuleFilter", "Modulo") + ": " + x));
                related.AddRange(entries.Select(x => x.MachineName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).Select(x => T("Logs_MachineLabel", "Equipo") + ": " + x));
            }

            return related
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();
        }

        private LogStatusTimelineSegment ResolvePrimarySectionSegment(LogDashboardSection section)
        {
            return (section?.TimelineSegments ?? Array.Empty<LogStatusTimelineSegment>())
                .Where(x => x != null)
                .OrderByDescending(x => x.ObservableCount)
                .ThenByDescending(x => x.End)
                .FirstOrDefault();
        }

        private string BuildSectionHoverEyebrow(string sectionTitle, LogStatusTimelineSegment primarySegment)
        {
            if (primarySegment != null)
                return string.Format("{0:yyyy-MM-dd HH:mm} · {1}", primarySegment.End, string.IsNullOrWhiteSpace(sectionTitle) ? T("Logs_Dashboard_FactCardFallback", "Fact del dashboard") : sectionTitle);

            return string.Format("{0} · {1}", ResolveCurrentRangeLabel(), string.IsNullOrWhiteSpace(sectionTitle) ? T("Logs_Dashboard_FactCardFallback", "Fact del dashboard") : sectionTitle);
        }

        private string BuildFactHoverDetail(LogDashboardSection section, LogStatusTimelineSegment primarySegment, LogStatusFact fact)
        {
            var detail = string.IsNullOrWhiteSpace(fact?.Hint)
                ? T("Logs_Dashboard_FactHoverDetailFallback", "Este bloque ya no depende de un tooltip efimero: la explicacion queda visible aca mientras mantengas el hover.")
                : fact.Hint;

            if (!string.IsNullOrWhiteSpace(primarySegment?.Summary))
                return detail + " " + T("Logs_Dashboard_RelatedNarrativePrefix", "Narrativa relacionada: ") + primarySegment.Summary;

            if (!string.IsNullOrWhiteSpace(section?.Summary))
                return detail + " " + T("Logs_Dashboard_BlockContextPrefix", "Contexto del bloque: ") + section.Summary;

            return detail;
        }

        private string BuildSectionTemporalContext(LogDashboardSection section, LogStatusTimelineSegment primarySegment)
        {
            if (primarySegment != null)
            {
                return string.Format(
                    "{0:yyyy-MM-dd HH:mm} → {1:yyyy-MM-dd HH:mm}",
                    primarySegment.Start,
                    primarySegment.End);
            }

            return T("Logs_Dashboard_FilteredRangePrefix", "Rango filtrado: ") + ResolveCurrentRangeLabel();
        }

        private string BuildSectionDurationOrVigency(LogDashboardSection section, LogStatusTimelineSegment primarySegment)
        {
            if (primarySegment != null)
                return F("Logs_Dashboard_ValidForBlockUntil", "Vigente para el bloque {0} hasta {1:yyyy-MM-dd HH:mm}.", primarySegment.BucketLabel ?? T("Logs_Dashboard_HistoricalShort", "historico"), primarySegment.End);

            return T("Logs_Dashboard_ValidWithinRangePrefix", "Vigente dentro del rango filtrado (") + ResolveCurrentRangeLabel() + ").";
        }

        private IReadOnlyList<string> BuildFactRelatedItems(LogDashboardSection section, LogStatusTimelineSegment primarySegment, LogStatusFact fact)
        {
            var segmentRelated = (primarySegment?.Detail?.RelatedEventIdsOrLabels ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x));

            var segmentSummaries = (section?.TimelineSegments ?? Array.Empty<LogStatusTimelineSegment>())
                .Where(x => x != null && !string.Equals(x.SegmentId, primarySegment?.SegmentId, StringComparison.Ordinal))
                .Where(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.BucketLabel))
                .OrderByDescending(x => x.End)
                .Take(4)
                .Select(x => string.Format("{0}: {1}", x.BucketLabel ?? T("Logs_Dashboard_FactBlock", "Bloque"), x.Summary ?? x.NarrativeState ?? T("Logs_Dashboard_NoSummary", "Sin resumen")));

            var factPeers = (section?.Facts ?? Array.Empty<LogStatusFact>())
                .Where(x => x != null && !string.Equals(x.Label, fact?.Label, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .Select(x => string.Format("{0}: {1}", x.Label ?? T("Logs_Dashboard_FactCardShortFallback", "Fact"), x.Value ?? T("Logs_Dashboard_NoDataValue", "Sin dato")));

            return segmentRelated
                .Concat(segmentSummaries)
                .Concat(factPeers)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();
        }

        private string BuildHoverEyebrow(string category, IReadOnlyList<AppLogEntry> contextEntries)
        {
            var blockTitle = ResolveMetricCategoryTitle(category);
            var latestEntry = (contextEntries ?? Array.Empty<AppLogEntry>())
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();

            return latestEntry == null
                ? F("Logs_Dashboard_EyebrowRangeFormat", "{0} · {1}", ResolveCurrentRangeLabel(), blockTitle)
                : F("Logs_Dashboard_EyebrowTimestampFormat", "{0:yyyy-MM-dd HH:mm} · {1}", latestEntry.Timestamp, blockTitle);
        }

        private string BuildTemporalContextLabel(IReadOnlyList<AppLogEntry> contextEntries)
        {
            var entries = (contextEntries ?? Array.Empty<AppLogEntry>()).OrderBy(x => x.Timestamp).ToList();
            if (entries.Count == 0)
                return T("Logs_Dashboard_NoExactDateActiveRange", "Sin fecha exacta; rango activo ") + ResolveCurrentRangeLabel();

            if (entries.Count == 1)
                return entries[0].Timestamp.ToString("yyyy-MM-dd HH:mm");

            return string.Format("{0:yyyy-MM-dd HH:mm} → {1:yyyy-MM-dd HH:mm}", entries.First().Timestamp, entries.Last().Timestamp);
        }

        private string BuildDurationOrVigencyLabel(IReadOnlyList<AppLogEntry> contextEntries)
        {
            var entries = (contextEntries ?? Array.Empty<AppLogEntry>()).OrderBy(x => x.Timestamp).ToList();
            if (entries.Count == 0)
                return T("Logs_Dashboard_ValidOnlyInFilteredRangePrefix", "Vigente solo dentro del rango filtrado (") + ResolveCurrentRangeLabel() + T("Logs_Dashboard_ValidOnlyInFilteredRangeSuffix", ").");

            if (entries.Count == 1)
                return T("Logs_Dashboard_LastPointSignalPrefix", "Ultima señal puntual en ") + entries[0].Timestamp.ToString("yyyy-MM-dd HH:mm") + ".";

            var span = entries.Last().Timestamp - entries.First().Timestamp;
            return F(
                "Logs_Dashboard_ObservableActivityDuring",
                "Actividad observable durante {0} entre {1:yyyy-MM-dd HH:mm} y {2:yyyy-MM-dd HH:mm}.",
                FormatElapsed(span),
                entries.First().Timestamp,
                entries.Last().Timestamp);
        }

        private string ResolveCurrentRangeLabel()
        {
            return SelectedTimeRangeOption?.DisplayName ?? T("Logs_TimeRange_Last7Days", "Ultimos 7 dias");
        }

        private static bool IsMetricCategory(string category, string expected)
        {
            return string.Equals(category ?? string.Empty, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveMetricCategoryTitle(string category)
        {
            if (IsMetricCategory(category, SourceActivityCategory))
                return T("Logs_Dashboard_SourceActivityTitle", "Actividad por modulo");

            if (IsMetricCategory(category, UserActivityCategory))
                return T("Logs_Dashboard_UserActivityTitle", "Actividad por usuario");

            if (IsMetricCategory(category, MachineActivityCategory))
                return T("Logs_Dashboard_MachineActivityTitle", "Actividad por equipo");

            if (IsMetricCategory(category, LatencyDistributionCategory))
                return T("Logs_Dashboard_LatencyDistributionTitle", "Distribucion de latencia");

            return T("Logs_Dashboard_GenericBlock", "Bloque del dashboard");
        }

        private static string T(string key, string fallback)
        {
            return LocalizedText.Get(key, fallback);
        }

        private static string F(string key, string fallback, params object[] args)
        {
            return string.Format(T(key, fallback), args);
        }

        private static string ResolveDominantSeverity(IEnumerable<AppLogEntry> entries)
        {
            var safeEntries = (entries ?? Array.Empty<AppLogEntry>()).Where(x => x != null).ToList();
            if (safeEntries.Any(x => string.Equals(x.Level, "ERROR", StringComparison.OrdinalIgnoreCase)))
                return "ERROR";

            if (safeEntries.Any(x => string.Equals(x.Level, "WARNING", StringComparison.OrdinalIgnoreCase)))
                return "WARNING";

            if (safeEntries.Any(IsValidationSignal))
                return "VALIDATION";

            if (safeEntries.Any(x => (x.DurationMs ?? 0L) >= 1000))
                return "LATENCY";

            if (safeEntries.Any(IsHealthSignal))
                return "HEALTH";

            return "INFO";
        }

        private static bool MatchesLatencyBand(string label, long durationMs)
        {
            if (string.Equals(label, "< 250 ms", StringComparison.OrdinalIgnoreCase))
                return durationMs < 250;

            if (string.Equals(label, "250-499 ms", StringComparison.OrdinalIgnoreCase))
                return durationMs >= 250 && durationMs < 500;

            if (string.Equals(label, "500-999 ms", StringComparison.OrdinalIgnoreCase))
                return durationMs >= 500 && durationMs < 1000;

            if (string.Equals(label, "1-2 s", StringComparison.OrdinalIgnoreCase))
                return durationMs >= 1000 && durationMs < 2000;

            if (string.Equals(label, ">= 2 s", StringComparison.OrdinalIgnoreCase))
                return durationMs >= 2000;

            return false;
        }

        private static string FormatElapsed(TimeSpan span)
        {
            if (span.TotalDays >= 1)
                return string.Format("{0:N0} d", Math.Floor(span.TotalDays));

            if (span.TotalHours >= 1)
                return string.Format("{0:N0} h", Math.Floor(span.TotalHours));

            if (span.TotalMinutes >= 1)
                return string.Format("{0:N0} min", Math.Max(Math.Floor(span.TotalMinutes), 1));

            return string.Format("{0:N0} s", Math.Max(Math.Floor(span.TotalSeconds), 1));
        }
    }

    public class DashboardHoverCard
    {
        public string StatusLevel { get; set; }
        public string Eyebrow { get; set; }
        public string StatusIcon { get; set; }
        public string BadgeLabel { get; set; }
        public string SecondaryBadge { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Detail { get; set; }
        public string DurationLabel { get; set; }
        public string ActionHint { get; set; }
        public IReadOnlyList<LogStatusFact> Facts { get; set; }
        public IReadOnlyList<string> RelatedItems { get; set; }
    }

    internal sealed class HoverSemanticState
    {
        public HoverSemanticState(string statusLevel, string severity, string badgeLabel, string secondaryBadge)
        {
            StatusLevel = statusLevel;
            Severity = severity;
            BadgeLabel = badgeLabel;
            SecondaryBadge = secondaryBadge;
        }

        public string StatusLevel { get; private set; }
        public string Severity { get; private set; }
        public string BadgeLabel { get; private set; }
        public string SecondaryBadge { get; private set; }
    }

    public class IncidentDayGroup
    {
        public DateTime Date { get; set; }
        public IReadOnlyList<AppLogEntry> Items { get; set; }
        public string DateLabel => Date.ToString("yyyy-MM-dd");
    }
}
