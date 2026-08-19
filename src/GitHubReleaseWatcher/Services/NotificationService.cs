using GitHubReleaseWatcher.Core.Models;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace GitHubReleaseWatcher.Services;

public sealed class NotificationService : IDisposable
{
    private readonly FileLogger _logger;
    private readonly DesktopSessionService _desktopSession;
    private readonly Dictionary<Guid, PendingNotification> _deferredNotifications = [];
    private bool _registered;

    public NotificationService(FileLogger logger, DesktopSessionService desktopSession)
    {
        _logger = logger;
        _desktopSession = desktopSession;
        _desktopSession.DesktopAvailable += FlushDeferredNotifications;
    }

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

        var notification = new PendingNotification(
            repository.Id,
            repository.Repository,
            repository.FullName,
            previousVersion,
            repository.LatestVersion,
            repository.LatestReleaseUrl);

        if (!_desktopSession.IsDesktopAvailable)
        {
            _deferredNotifications[repository.Id] = notification;
            _ = _logger.InfoAsync($"잠금 해제까지 알림 보류: {repository.FullName} {repository.LatestVersion}");
            return;
        }

        ShowCore(notification);
    }

    private void FlushDeferredNotifications()
    {
        if (!_registered || !_desktopSession.IsDesktopAvailable || _deferredNotifications.Count == 0)
        {
            return;
        }

        var pending = _deferredNotifications.Values.ToList();
        _deferredNotifications.Clear();
        foreach (var notification in pending)
        {
            ShowCore(notification);
        }
    }

    private void ShowCore(PendingNotification notification)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddArgument("url", notification.ReleaseUrl)
                .AddText($"{notification.RepositoryName} 업데이트")
                .AddText($"{notification.PreviousVersion ?? "새 Release"} → {notification.LatestVersion}")
                .AddButton(new AppNotificationButton("Release 보기")
                    .AddArgument("url", notification.ReleaseUrl));
            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync($"알림 표시 실패: {notification.FullName}", ex);
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
        _desktopSession.DesktopAvailable -= FlushDeferredNotifications;

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

    private sealed record PendingNotification(
        Guid RepositoryId,
        string RepositoryName,
        string FullName,
        string? PreviousVersion,
        string LatestVersion,
        string ReleaseUrl);
}
