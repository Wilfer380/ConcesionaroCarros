using ConcesionaroCarros.Properties;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ConcesionaroCarros.ViewModels;

namespace ConcesionaroCarros.Services
{
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        private const string IndexerPropertyName = "Item[]";
        private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("es");
        private static readonly Lazy<LocalizationService> LazyInstance =
            new Lazy<LocalizationService>(() => new LocalizationService());

        private readonly ObservableCollection<LanguageOption> _availableLanguages;
        private LanguageOption _selectedLanguage;

        private LocalizationService()
        {
            _availableLanguages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption("PT", "Português", "pt-BR"),
                new LanguageOption("EN", "English", "en"),
                new LanguageOption("ES", "Español", "es")
            };

            AvailableLanguages = new ReadOnlyObservableCollection<LanguageOption>(_availableLanguages);
            _selectedLanguage = _availableLanguages.First(option => option.Code == "ES");
            ApplyCulture(_selectedLanguage.Culture, false);
        }

        public static LocalizationService Instance => LazyInstance.Value;

        public event EventHandler LanguageChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyObservableCollection<LanguageOption> AvailableLanguages { get; }

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (value == null || string.Equals(_selectedLanguage?.Code, value.Code, StringComparison.OrdinalIgnoreCase))
                    return;

                var matchingOption = _availableLanguages.FirstOrDefault(option =>
                    string.Equals(option.Code, value.Code, StringComparison.OrdinalIgnoreCase));

                _selectedLanguage = matchingOption ?? value;
                ApplyCulture(_selectedLanguage.Culture, true);
            }
        }

        public CultureInfo CurrentCulture => _selectedLanguage.Culture;

        public string this[string key] => GetString(key);

        public string GetString(string key, string fallback)
        {
            var value = GetString(key);
            if (!string.Equals(value, $"[{key}]", StringComparison.Ordinal))
                return value;

            return fallback ?? value;
        }

        public void Initialize()
        {
            ApplyCulture(_selectedLanguage.Culture, false);
        }

        public void ApplyToWindow(Window window)
        {
            if (window == null)
                return;

            window.Language = XmlLanguage.GetLanguage(CurrentCulture.IetfLanguageTag);
        }

        public string GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var value = Resources.ResourceManager.GetString(key, CurrentCulture);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = Resources.ResourceManager.GetString(key, DefaultCulture);
            return !string.IsNullOrWhiteSpace(value) ? value : $"[{key}]";
        }

        private void ApplyCulture(CultureInfo culture, bool notify)
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Resources.Culture = culture;

            foreach (var window in GetOpenWindows())
            {
                ApplyToWindow(window);
            }

            if (!notify)
                return;

            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged(IndexerPropertyName);
            RefreshOpenViewModels();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
            LogService.Info("Localization", $"Idioma aplicado: {_selectedLanguage.Code} ({culture.Name})");
        }

        private void RefreshOpenViewModels()
        {
            var refreshedContexts = new HashSet<object>(ReferenceEqualityComparer.Instance);

            foreach (var window in GetOpenWindows())
                RefreshFromDependencyObject(window, refreshedContexts);
        }

        private static IEnumerable<Window> GetOpenWindows()
        {
            if (Application.Current == null)
                return Enumerable.Empty<Window>();

            return Application.Current.Windows.Cast<Window>();
        }

        private static void RefreshFromDependencyObject(DependencyObject dependencyObject, ISet<object> refreshedContexts)
        {
            if (dependencyObject == null)
                return;

            if (dependencyObject is FrameworkElement frameworkElement)
                RefreshViewModel(frameworkElement.DataContext, refreshedContexts);

            foreach (var child in LogicalTreeHelper.GetChildren(dependencyObject).OfType<DependencyObject>())
                RefreshFromDependencyObject(child, refreshedContexts);
        }

        private static void RefreshViewModel(object dataContext, ISet<object> refreshedContexts)
        {
            if (dataContext == null || !refreshedContexts.Add(dataContext))
                return;

            if (dataContext is ILocalizableViewModel localizableViewModel)
                localizableViewModel.RefreshLocalization();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public sealed class LanguageOption
        {
            public LanguageOption(string code, string displayName, string cultureName)
            {
                Code = code;
                DisplayName = displayName;
                Culture = CultureInfo.GetCultureInfo(cultureName);
            }

            public string Code { get; }
            public string DisplayName { get; }
            public CultureInfo Culture { get; }
            public string Label => $"{Code} ({DisplayName})";
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
