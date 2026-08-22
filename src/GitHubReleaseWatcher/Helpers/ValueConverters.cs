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
            RepositoryStatus.UpdateAvailable => "StatusUpdateFgBrush",
            RepositoryStatus.Failed => "StatusFailedFgBrush",
            RepositoryStatus.Checking => "StatusCheckingFgBrush",
            RepositoryStatus.NoReleases => "StatusNoReleasesFgBrush",
            _ => "StatusLatestFgBrush"
        };
        return System.Windows.Application.Current.Resources[key] ?? System.Windows.Application.Current.Resources["PrimaryTextBrush"];
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}

public sealed class StatusBackgroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value switch
        {
            RepositoryStatus.UpdateAvailable => "StatusUpdateBgBrush",
            RepositoryStatus.Failed => "StatusFailedBgBrush",
            RepositoryStatus.Checking => "StatusCheckingBgBrush",
            RepositoryStatus.NoReleases => "StatusNoReleasesBgBrush",
            _ => "StatusLatestBgBrush"
        };
        return System.Windows.Application.Current.Resources[key] ?? System.Windows.Application.Current.Resources["SurfaceBrush"];
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}

public sealed class StatusBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value switch
        {
            RepositoryStatus.UpdateAvailable => "StatusUpdateBorderBrush",
            RepositoryStatus.Failed => "StatusFailedBorderBrush",
            RepositoryStatus.Checking => "StatusCheckingBorderBrush",
            RepositoryStatus.NoReleases => "StatusNoReleasesBorderBrush",
            _ => "StatusLatestBorderBrush"
        };
        return System.Windows.Application.Current.Resources[key] ?? System.Windows.Application.Current.Resources["BorderBrush"];
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

public sealed class ThemeLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        AppThemeMode.System => "시스템 기본",
        AppThemeMode.Light => "라이트 모드",
        AppThemeMode.Dark => "다크 모드",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}
