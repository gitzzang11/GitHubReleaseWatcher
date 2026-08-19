using Microsoft.Win32;
using System.Windows;

namespace GitHubReleaseWatcher.Services;

public sealed class ThemeService : IDisposable
{
    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        Apply();
    }

    public void Apply()
    {
        var theme = IsLightTheme() ? "LightTheme.xaml" : "DarkTheme.xaml";
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);
        if (existing is not null)
        {
            dictionaries.Remove(existing);
        }
        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri($"Themes/{theme}", UriKind.Relative) });
    }

    private static bool IsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(Apply);

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
