using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Services;
using System;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private string _selectedThemeMode;

        public ICommand UseLightThemeCommand { get; }
        public ICommand UseDarkThemeCommand { get; }

        public string SelectedThemeMode
        {
            get => _selectedThemeMode;
            private set
            {
                if (string.Equals(_selectedThemeMode, value, StringComparison.Ordinal))
                    return;

                _selectedThemeMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightSelected));
                OnPropertyChanged(nameof(IsDarkSelected));
                OnPropertyChanged(nameof(ResolvedThemeMode));
            }
        }

        public bool IsLightSelected => string.Equals(ResolvedThemeMode, ThemeManager.LightMode, StringComparison.Ordinal);
        public bool IsDarkSelected => string.Equals(ResolvedThemeMode, ThemeManager.DarkMode, StringComparison.Ordinal);
        public string ResolvedThemeMode => ThemeManager.ResolveTheme(SelectedThemeMode);

        public SettingsViewModel()
        {
            UseLightThemeCommand = new RelayCommand(_ => SetTheme(ThemeManager.LightMode));
            UseDarkThemeCommand = new RelayCommand(_ => SetTheme(ThemeManager.DarkMode));

            SelectedThemeMode = ThemeManager.CurrentPreference;
        }

        private void SetTheme(string mode)
        {
            ThemeManager.ApplyThemePreference(mode);
            SelectedThemeMode = ThemeManager.CurrentPreference;
        }
    }
}
