using ConcesionaroCarros.Services;
using ConcesionaroCarros.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ConcesionaroCarros.Views
{
    public partial class LogsView : UserControl
    {
        private readonly DispatcherTimer _hoverReleaseTimer;
        private FrameworkElement _activeHoverSourceElement;
        private LogStatusTimelineSegment _activeNarrativeSegment;
        private LogStatusTimelineSegment _pendingNarrativeSegment;
        private bool _isNarrativeContextHovered;
        private bool _isDashboardCardHovered;
        private long _hoverSessionId;
        private long _pendingHoverSessionId;
        private HoverOwnerKind _activeHoverOwner;
        private HoverOwnerKind _pendingHoverOwner;
        private bool _isHoverSourceHovered;

        private enum HoverOwnerKind
        {
            None,
            Narrative,
            Metric,
            Fact
        }

        public LogsView()
        {
            _hoverReleaseTimer = BuildHoverReleaseTimer(HoverReleaseTimer_Tick);

            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext == null)
                DataContext = new LogsViewModel();

            if (DataContext is LogsViewModel vm)
                vm.StartAutoRefresh();

            Dispatcher.BeginInvoke(new Action(ResetHorizontalScroll));
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LogsViewModel vm)
            {
                vm.SetNarrativeHoverSourceActive(false);
                vm.SetNarrativeContextHoverActive(false);
                vm.SetDashboardHoverSourceActive(false);
                vm.SetDashboardHoverCardActive(false);
                vm.ClearNarrativeHover();
                vm.ClearDashboardHoverCard();
            }

            ResetHoverState();

            if (DataContext is IDisposable disposable)
                disposable.Dispose();
        }

        private void ResetHorizontalScroll()
        {
            var scrollViewer = FindDescendant<ScrollViewer>(LogsDataGrid);
            scrollViewer?.ScrollToHorizontalOffset(0);
        }

        private void NarrativeSegment_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm &&
                sender is FrameworkElement element &&
                element.DataContext is LogStatusTimelineSegment segment)
            {
                BeginHoverSession(element, HoverOwnerKind.Narrative);
                _activeNarrativeSegment = segment;
                vm.SetNarrativeHoverSourceActive(true);
                vm.SetDashboardHoverSourceActive(true);
                vm.PreviewNarrativeSegment(segment);
            }
        }

        private void NarrativeSegment_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm &&
                sender is FrameworkElement element &&
                element.DataContext is LogStatusTimelineSegment segment)
            {
                if (!IsActiveHoverSource(element, HoverOwnerKind.Narrative))
                    return;

                _activeHoverSourceElement = null;
                _isHoverSourceHovered = false;
                vm.SetNarrativeHoverSourceActive(false);
                vm.SetDashboardHoverSourceActive(false);
                ScheduleHoverRelease(HoverOwnerKind.Narrative, segment);
            }
        }

        private void NarrativeContextPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm &&
                CanActivateNarrativeContext())
            {
                StopHoverRelease();
                _isNarrativeContextHovered = true;
                vm.SetNarrativeContextHoverActive(true);
            }
        }

        private void NarrativeContextPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm)
                vm.SetNarrativeContextHoverActive(false);

            _isNarrativeContextHovered = false;

            if (_activeHoverOwner == HoverOwnerKind.Narrative || _pendingHoverOwner == HoverOwnerKind.Narrative)
                ScheduleHoverRelease(HoverOwnerKind.Narrative, _activeNarrativeSegment ?? _pendingNarrativeSegment);
        }

        private void MetricBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm &&
                sender is FrameworkElement element &&
                element.DataContext is LogMetricDistributionItem item)
            {
                DismissNarrativeHover(vm);
                BeginHoverSession(element, HoverOwnerKind.Metric);
                vm.SetDashboardHoverSourceActive(true);
                vm.PreviewMetricHover(element.Tag as string ?? "dashboard-block", item);
            }
        }

        private void MetricBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !IsActiveHoverSource(element, HoverOwnerKind.Metric))
                return;

            if (DataContext is LogsViewModel vm)
                vm.SetDashboardHoverSourceActive(false);

            _activeHoverSourceElement = null;
            _isHoverSourceHovered = false;
            ScheduleHoverRelease(HoverOwnerKind.Metric);
        }

        private void SectionFact_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm &&
                sender is FrameworkElement element &&
                element.DataContext is LogStatusFact fact)
            {
                DismissNarrativeHover(vm);
                BeginHoverSession(element, HoverOwnerKind.Fact);
                vm.SetDashboardHoverSourceActive(true);
                vm.PreviewFactHover(element.Tag as string, fact);
            }
        }

        private void SectionFact_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !IsActiveHoverSource(element, HoverOwnerKind.Fact))
                return;

            if (DataContext is LogsViewModel vm)
                vm.SetDashboardHoverSourceActive(false);

            _activeHoverSourceElement = null;
            _isHoverSourceHovered = false;
            ScheduleHoverRelease(HoverOwnerKind.Fact);
        }

        private void DashboardHoverPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm &&
                (_isHoverSourceHovered || _hoverReleaseTimer.IsEnabled))
            {
                StopHoverRelease();
                _isDashboardCardHovered = true;
                vm.SetDashboardHoverCardActive(true);
            }
        }

        private void DashboardHoverPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is LogsViewModel vm)
                vm.SetDashboardHoverCardActive(false);

            _isDashboardCardHovered = false;
            ScheduleHoverRelease(_activeHoverOwner, _activeNarrativeSegment ?? _pendingNarrativeSegment);
        }

        private void HoverReleaseTimer_Tick(object sender, EventArgs e)
        {
            StopHoverRelease();

            if (_pendingHoverSessionId != _hoverSessionId || ShouldKeepCurrentHoverAlive())
                return;

            if (DataContext is LogsViewModel vm)
            {
                if (_pendingHoverOwner == HoverOwnerKind.Narrative)
                {
                    vm.ClearNarrativeHover(_pendingNarrativeSegment);
                    vm.SetNarrativeHoverSourceActive(false);
                    vm.SetNarrativeContextHoverActive(false);
                    _activeNarrativeSegment = null;
                }

                vm.ClearDashboardHoverCard();
                vm.SetDashboardHoverSourceActive(false);
                vm.SetDashboardHoverCardActive(false);
            }

            _activeHoverOwner = HoverOwnerKind.None;
            _activeHoverSourceElement = null;
            _pendingHoverOwner = HoverOwnerKind.None;
            _pendingHoverSessionId = 0;
            _pendingNarrativeSegment = null;
        }

        private void BeginHoverSession(FrameworkElement sourceElement, HoverOwnerKind owner)
        {
            StopHoverRelease();
            _hoverSessionId++;
            _activeHoverOwner = owner;
            _activeHoverSourceElement = sourceElement;
            _isHoverSourceHovered = true;
            _pendingHoverOwner = HoverOwnerKind.None;
            _pendingHoverSessionId = 0;
            _pendingNarrativeSegment = null;
        }

        private bool IsActiveHoverSource(FrameworkElement element, HoverOwnerKind owner)
        {
            return element != null &&
                   owner == _activeHoverOwner &&
                   ReferenceEquals(element, _activeHoverSourceElement);
        }

        private bool CanActivateNarrativeContext()
        {
            var narrativeHoverActive = _activeHoverOwner == HoverOwnerKind.Narrative || _pendingHoverOwner == HoverOwnerKind.Narrative;
            return narrativeHoverActive && (_isHoverSourceHovered || _hoverReleaseTimer.IsEnabled || _isNarrativeContextHovered);
        }

        private bool ShouldKeepCurrentHoverAlive()
        {
            if (_isHoverSourceHovered || _isDashboardCardHovered)
                return true;

            return _pendingHoverOwner == HoverOwnerKind.Narrative && _isNarrativeContextHovered;
        }

        private void DismissNarrativeHover(LogsViewModel vm)
        {
            if (vm == null)
                return;

            StopHoverRelease();
            _isNarrativeContextHovered = false;
            _activeNarrativeSegment = null;
            _pendingNarrativeSegment = null;
            vm.SetNarrativeHoverSourceActive(false);
            vm.SetNarrativeContextHoverActive(false);
            vm.ClearNarrativeHover();
        }

        private void ScheduleHoverRelease(HoverOwnerKind owner, LogStatusTimelineSegment narrativeSegment = null)
        {
            if (owner == HoverOwnerKind.None)
                return;

            _pendingHoverOwner = owner;
            _pendingHoverSessionId = _hoverSessionId;
            _pendingNarrativeSegment = owner == HoverOwnerKind.Narrative
                ? narrativeSegment
                : null;

            _hoverReleaseTimer.Stop();
            _hoverReleaseTimer.Start();
        }

        private void StopHoverRelease()
        {
            _hoverReleaseTimer.Stop();
        }

        private void ResetHoverState()
        {
            StopHoverRelease();
            _activeHoverSourceElement = null;
            _activeNarrativeSegment = null;
            _pendingNarrativeSegment = null;
            _isHoverSourceHovered = false;
            _isNarrativeContextHovered = false;
            _isDashboardCardHovered = false;
            _pendingHoverSessionId = 0;
            _activeHoverOwner = HoverOwnerKind.None;
            _pendingHoverOwner = HoverOwnerKind.None;
        }

        private static DispatcherTimer BuildHoverReleaseTimer(EventHandler tickHandler)
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            timer.Tick += tickHandler;
            return timer;
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
                return null;

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T typedChild)
                    return typedChild;

                var descendant = FindDescendant<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }
    }
}
