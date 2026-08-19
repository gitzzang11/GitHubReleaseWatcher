using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GitHubReleaseWatcher.Core.Models;

namespace GitHubReleaseWatcher.Helpers;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not Visibility.Visible;
}

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value switch
        {
            RepositoryStatus.UpdateAvailable => "WarningBrush",
            RepositoryStatus.Failed => "DangerBrush",
            RepositoryStatus.Checking => "AccentBrush",
            RepositoryStatus.NoReleases => "MutedTextBrush",
            _ => "SuccessBrush"
        };
        return System.Windows.Application.Current.Resources[key];
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}

public sealed class IntervalLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        60 => "1시간",
        180 => "3시간",
        360 => "6시간",
        int minutes => $"{minutes}분",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}
