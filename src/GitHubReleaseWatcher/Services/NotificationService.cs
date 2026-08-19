using GitHubReleaseWatcher.Core.Models;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace GitHubReleaseWatcher.Services;

public sealed class NotificationService : IDisposable
{
    private readonly FileLogger _logger;
    private bool _registered;

    public NotificationService(FileLogger logger) => _logger = logger;

    public event Action<string>? ReleaseRequested;

    public void Register()
    {
        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync("Windows 알림 등록 실패", ex);
        }
    }

    public void Show(RepositorySubscription repository, string? previousVersion)
    {
        if (!_registered || repository.LatestVersion is null || repository.LatestReleaseUrl is null)
        {
            return;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddArgument("url", repository.LatestReleaseUrl)
                .AddText($"{repository.Repository} 업데이트")
                .AddText($"{previousVersion ?? "새 Release"} → {repository.LatestVersion}")
                .AddButton(new AppNotificationButton("Release 보기")
                    .AddArgument("url", repository.LatestReleaseUrl));
            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync($"알림 표시 실패: {repository.FullName}", ex);
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        try
        {
            if (args.Arguments.TryGetValue("url", out var url) && !string.IsNullOrWhiteSpace(url))
            {
                ReleaseRequested?.Invoke(url);
            }
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync("알림 동작 처리 실패", ex);
        }
    }

    public void Dispose()
    {
        if (!_registered)
        {
            return;
        }

        try
        {
            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
            AppNotificationManager.Default.Unregister();
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync("Windows 알림 해제 실패", ex);
        }
    }
}
