using Microsoft.Win32;
using System.Windows;
using GitHubReleaseWatcher.Core.Models;

namespace GitHubReleaseWatcher.Services;

public sealed class ThemeService : IDisposable
{
    private AppThemeMode _currentMode = AppThemeMode.System;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public AppThemeMode CurrentMode => _currentMode;

    public void Apply(AppThemeMode mode)
    {
        _currentMode = mode;
        var useLight = mode switch
        {
            AppThemeMode.Light => true,
            AppThemeMode.Dark => false,
            _ => IsSystemLightTheme()
        };

        var themeFile = useLight ? "LightTheme.xaml" : "DarkTheme.xaml";
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);
        if (existing is not null)
        {
            dictionaries.Remove(existing);
        }
        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri($"Themes/{themeFile}", UriKind.Relative) });
    }

    private static bool IsSystemLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_currentMode == AppThemeMode.System)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => Apply(AppThemeMode.System));
        }
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
