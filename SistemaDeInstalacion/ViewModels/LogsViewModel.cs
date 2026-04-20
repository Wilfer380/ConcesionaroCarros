using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcesionaroCarros.ViewModels
{
    public class LogsViewModel : BaseViewModel, IDisposable, ILocalizableViewModel
    {
        private static readonly HashSet<string> DeveloperUsers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "wandica",
                "maicolj"
            };

        private readonly LogDashboardService _dashboardService = new LogDashboardService();
        private readonly DispatcherTimer _autoRefreshTimer;
        private readonly string _currentDeveloperUser;
        private bool _isRefreshingFilters;
        private string _statusMessage;
        private string _selectedMachineFilter;
        private string _selectedDateFilter;
        private int _totalEventos;
        private int _totalErrores;
        private int _totalAdvertencias;
        private int _totalEquipos;
        private string _latenciaPromedio;
        private string _ultimoError;

        public ObservableCollection<AppLogEntry> AllEntries { get; } =
            new ObservableCollection<AppLogEntry>();

        public ObservableCollection<AppLogEntry> FilteredEntries { get; } =
            new ObservableCollection<AppLogEntry>();

        public ObservableCollection<LogMachineOption> AvailableMachines { get; } =
            new ObservableCollection<LogMachineOption>();

        public ObservableCollection<string> AvailableDates { get; } =
            new ObservableCollection<string>();

        public ICommand RefreshCommand { get; }

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
                OnPropertyChanged();
                if (!_isRefreshingFilters)
                    RefreshDatesAndEntries();
            }
        }

        public string SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                _selectedDateFilter = value;
                OnPropertyChanged();
                if (!_isRefreshingFilters)
                    ReloadEntries();
            }
        }

        public bool CanSelectDate =>
            !string.IsNullOrWhiteSpace(SelectedMachineFilter) &&
            AvailableDates.Count > 0;

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

        public string UltimoError
        {
            get => _ultimoError;
            set
            {
                _ultimoError = value;
                OnPropertyChanged();
            }
        }

        public LogsViewModel()
        {
            _currentDeveloperUser = ResolveCurrentDeveloperUser();
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

            RefreshCommand = new RelayCommand(_ => Refresh());
            Refresh();
        }

        public void StartAutoRefresh()
        {
            if (!_autoRefreshTimer.IsEnabled)
                _autoRefreshTimer.Start();
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
                RebuildMachines();
                AutoSelectCurrentDeveloperContext();
                RebuildDates();
                _isRefreshingFilters = false;

                if (!string.IsNullOrWhiteSpace(SelectedMachineFilter) && !string.IsNullOrWhiteSpace(SelectedDateFilter))
                {
                    ReloadEntries();
                }
                else
                {
                    ResetEntriesView();
                    TotalEquipos = _dashboardService.GetAvailableMachines().Count;
                    StatusMessage = LocalizedText.Get("Logs_SelectMachineAndDateStatus", "Selecciona un equipo y luego una fecha para cargar los logs.");
                    UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
                }
            }
            catch (Exception ex)
            {
                _isRefreshingFilters = false;
                StatusMessage = LocalizedText.Get("Logs_LoadErrorStatus", "No fue posible cargar los logs.");
                UltimoError = ex.Message;
            }
        }

        public override void RefreshLocalization()
        {
            Refresh();
        }

        public void Dispose()
        {
            StopAutoRefresh();
            _autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
        }

        private void RebuildMachines()
        {
            AvailableMachines.Clear();

            foreach (var machine in _dashboardService.GetAvailableMachines())
            {
                AvailableMachines.Add(new LogMachineOption
                {
                    MachineName = machine,
                    DisplayName = BuildMachineDisplayName(machine)
                });
            }

            OnPropertyChanged(nameof(SelectedMachineFilter));
        }

        private void RebuildDates()
        {
            var selected = SelectedDateFilter;
            AvailableDates.Clear();

            if (!string.IsNullOrWhiteSpace(SelectedMachineFilter))
            {
                foreach (var date in _dashboardService.GetAvailableDates(SelectedMachineFilter))
                    AvailableDates.Add(date);
            }

            if (AvailableDates.Count == 0)
                SelectedDateFilter = null;
            else if (!string.IsNullOrWhiteSpace(selected) && AvailableDates.Contains(selected))
                SelectedDateFilter = selected;
            else
                SelectedDateFilter = null;

            OnPropertyChanged(nameof(CanSelectDate));
        }

        private void ApplyFilters()
        {
            FilteredEntries.Clear();
            foreach (var entry in AllEntries.OrderByDescending(x => x.Timestamp).Take(250))
                FilteredEntries.Add(entry);
        }

        private void RefreshDatesAndEntries()
        {
            try
            {
                _isRefreshingFilters = true;
                RebuildDates();
                _isRefreshingFilters = false;
                ResetEntriesView();

                if (string.IsNullOrWhiteSpace(SelectedMachineFilter))
                {
                    StatusMessage = LocalizedText.Get("Logs_SelectMachineStatus", "Selecciona un equipo para ver sus carpetas de fechas.");
                    UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
                    return;
                }

                StatusMessage = AvailableDates.Count == 0
                    ? string.Format(LocalizedText.Get("Logs_NoDatesForMachineStatus", "El equipo {0} no tiene carpetas de logs disponibles."), SelectedMachineFilter)
                    : string.Format(LocalizedText.Get("Logs_MachineSelectedStatus", "Equipo {0} seleccionado. Ahora elige una fecha."), SelectedMachineFilter);
                UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
            }
            catch (Exception ex)
            {
                _isRefreshingFilters = false;
                StatusMessage = LocalizedText.Get("Logs_FilterRefreshErrorStatus", "No fue posible actualizar los filtros de logs.");
                UltimoError = ex.Message;
            }
        }

        private void ReloadEntries()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedMachineFilter) || string.IsNullOrWhiteSpace(SelectedDateFilter))
                {
                    ResetEntriesView();
                    StatusMessage = string.IsNullOrWhiteSpace(SelectedMachineFilter)
                        ? LocalizedText.Get("Logs_SelectMachineToLoadStatus", "Selecciona un equipo para cargar los logs.")
                        : LocalizedText.Get("Logs_SelectDateToLoadStatus", "Selecciona una fecha del equipo para cargar los logs.");
                    UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
                    return;
                }

                var entries = _dashboardService.LoadEntries(SelectedMachineFilter, SelectedDateFilter);
                var summary = _dashboardService.BuildSummary(entries);

                AllEntries.Clear();
                foreach (var entry in entries)
                    AllEntries.Add(entry);

                TotalEventos = summary.TotalEvents;
                TotalErrores = summary.ErrorCount;
                TotalAdvertencias = summary.WarningCount;
                TotalEquipos = _dashboardService.GetAvailableMachines().Count;
                LatenciaPromedio = summary.AverageLatencyMs <= 0
                    ? LocalizedText.Get("Logs_NoData", "Sin datos")
                    : summary.AverageLatencyMs.ToString("N0") + " ms";
                UltimoError = summary.LatestError == null
                    ? LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes")
                    : $"{summary.LatestError.Timestamp:yyyy-MM-dd HH:mm:ss} - {summary.LatestError.Message}";

                ApplyFilters();
                StatusMessage = BuildStatusMessage(entries.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = LocalizedText.Get("Logs_FilteredLoadErrorStatus", "No fue posible cargar los logs filtrados.");
                UltimoError = ex.Message;
            }
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            SilentRefreshCurrentSelection();
        }

        private void SilentRefreshCurrentSelection()
        {
            try
            {
                var currentMachine = SelectedMachineFilter;
                var currentDate = SelectedDateFilter;

                _isRefreshingFilters = true;
                RebuildMachines();

                if (string.IsNullOrWhiteSpace(currentMachine) ||
                    !AvailableMachines.Any(x => string.Equals(x.MachineName, currentMachine, StringComparison.OrdinalIgnoreCase)))
                {
                    _isRefreshingFilters = false;
                    ResetEntriesView();
                    StatusMessage = LocalizedText.Get("Logs_SelectMachineAndDateStatus", "Selecciona un equipo y luego una fecha para cargar los logs.");
                    UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
                    return;
                }

                if (!string.Equals(SelectedMachineFilter, currentMachine, StringComparison.OrdinalIgnoreCase))
                    SelectedMachineFilter = currentMachine;

                RebuildDates();

                if (string.IsNullOrWhiteSpace(currentDate) || !AvailableDates.Contains(currentDate))
                {
                    if (!string.IsNullOrWhiteSpace(currentDate) && AvailableDates.Count > 0)
                        SelectedDateFilter = AvailableDates[0];
                    else
                        SelectedDateFilter = currentDate;
                }
                else if (!string.Equals(SelectedDateFilter, currentDate, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedDateFilter = currentDate;
                }

                _isRefreshingFilters = false;

                if (!string.IsNullOrWhiteSpace(SelectedMachineFilter) && !string.IsNullOrWhiteSpace(SelectedDateFilter))
                    ReloadEntries();
                else
                {
                    ResetEntriesView();
                    StatusMessage = string.IsNullOrWhiteSpace(SelectedMachineFilter)
                        ? LocalizedText.Get("Logs_SelectMachineToLoadStatus", "Selecciona un equipo para cargar los logs.")
                        : LocalizedText.Get("Logs_SelectDateToLoadStatus", "Selecciona una fecha del equipo para cargar los logs.");
                    UltimoError = LocalizedText.Get("Logs_NoRecentErrors", "Sin errores recientes");
                }
            }
            catch
            {
                _isRefreshingFilters = false;
            }
        }

        private string BuildStatusMessage(int count)
        {
            var machineText = string.IsNullOrWhiteSpace(SelectedMachineFilter)
                ? LocalizedText.Get("Logs_NoMachineSelected", "sin equipo seleccionado")
                : string.Format(LocalizedText.Get("Logs_MachineFormat", "equipo {0}"), SelectedMachineFilter);

            var dateText = string.IsNullOrWhiteSpace(SelectedDateFilter)
                ? LocalizedText.Get("Logs_NoDateSelected", "sin fecha seleccionada")
                : string.Format(LocalizedText.Get("Logs_DateFormat", "fecha {0}"), SelectedDateFilter);

            return string.Format(
                LocalizedText.Get("Logs_LoadedStatus", "Logs cargados: {0} eventos de {1}, {2}, desde {3}"),
                count,
                machineText,
                dateText,
                LogService.PrimaryLogsDirectory);
        }

        private void ResetEntriesView()
        {
            AllEntries.Clear();
            FilteredEntries.Clear();
            TotalEventos = 0;
            TotalErrores = 0;
            TotalAdvertencias = 0;
            LatenciaPromedio = LocalizedText.Get("Logs_NoData", "Sin datos");
        }

        private void AutoSelectCurrentDeveloperContext()
        {
            SelectedMachineFilter = null;
            SelectedDateFilter = null;

            if (string.IsNullOrWhiteSpace(_currentDeveloperUser))
                return;

            var latestEntry = _dashboardService.GetLatestEntryForUser(_currentDeveloperUser);
            if (latestEntry == null)
                return;

            if (AvailableMachines.Any(x => string.Equals(x.MachineName, latestEntry.MachineName, StringComparison.OrdinalIgnoreCase)))
                SelectedMachineFilter = latestEntry.MachineName;

            if (!string.IsNullOrWhiteSpace(SelectedMachineFilter))
            {
                var dates = _dashboardService.GetAvailableDates(SelectedMachineFilter);
                var preferredDate = latestEntry.Timestamp.ToString("yyyy-MM-dd");
                SelectedDateFilter = dates.FirstOrDefault(x => string.Equals(x, preferredDate, StringComparison.OrdinalIgnoreCase))
                                     ?? dates.FirstOrDefault();
            }
        }

        private string BuildMachineDisplayName(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
                return string.Empty;

            var latestEntry = _dashboardService.GetLatestEntryForMachine(machineName);
            var machineUser = NormalizeDeveloperUser(latestEntry?.UserName);
            if (string.IsNullOrWhiteSpace(machineUser))
                return machineName;

            return $"{machineName} - {machineUser}";
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
    }
}
