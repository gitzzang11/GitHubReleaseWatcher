using System.IO;
using GitHubReleaseWatcher.Core.Models;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace GitHubReleaseWatcher.Services;

public sealed class NotificationService : IDisposable
{
    private readonly FileLogger _logger;
    private readonly DesktopSessionService _desktopSession;
    private readonly TrayService _trayService;
    private readonly Dictionary<Guid, PendingNotification> _deferredNotifications = [];
    private bool _registered;
    private bool _nativeRegistered;
    private string? _fallbackReleaseUrl;

    public NotificationService(FileLogger logger, DesktopSessionService desktopSession, TrayService trayService)
    {
        _logger = logger;
        _desktopSession = desktopSession;
        _trayService = trayService;
        _desktopSession.DesktopAvailable += FlushDeferredNotifications;
        _trayService.BalloonClicked += OnFallbackBalloonClicked;
    }

    public event Action<string>? ReleaseRequested;
    public event Action<Guid, string>? NotificationDelivered;

    public void Register()
    {
        _registered = true;
        if (!AppNotificationManager.IsSupported())
        {
            _ = _logger.InfoAsync("Windows App SDK 알림 미지원: 트레이 알림 사용");
            return;
        }

        try
        {
            var manager = AppNotificationManager.Default;
            manager.NotificationInvoked += OnNotificationInvoked;
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");
            if (File.Exists(iconPath))
            {
                manager.Register("GitHub Release Watcher", new Uri(iconPath));
            }
            else
            {
                manager.Register();
            }
            _nativeRegistered = true;
            _ = _logger.InfoAsync($"Windows 알림 등록 완료: {manager.Setting}");
        }
        catch (Exception ex)
        {
            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
            _ = _logger.ErrorAsync("Windows 알림 등록 실패: 트레이 알림으로 전환", ex);
        }
    }

    public NotificationSendResult Show(RepositorySubscription repository, string? previousVersion)
    {
        if (!_registered || repository.LatestVersion is null || repository.LatestReleaseUrl is null)
        {
            return NotificationSendResult.NotRegistered;
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
            return NotificationSendResult.Deferred;
        }

        return ShowCore(notification);
    }

    public NotificationSendResult ShowTest()
    {
        var availability = GetAvailability();
        if (availability != NotificationSendResult.Delivered)
        {
            return availability;
        }

        try
        {
            if (CanUseNativeNotifications())
            {
                var notification = new AppNotificationBuilder()
                    .AddText("GitHub Release Watcher")
                    .AddText("테스트 알림이 정상적으로 전달되었습니다.")
                    .BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            else
            {
                _trayService.ShowBalloon("GitHub Release Watcher", "테스트 알림이 정상적으로 전달되었습니다.");
            }
            _ = _logger.InfoAsync("테스트 알림 전달 완료");
            return NotificationSendResult.Delivered;
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync("테스트 알림 표시 실패", ex);
            return NotificationSendResult.Failed;
        }
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
            if (ShowCore(notification) != NotificationSendResult.Delivered)
            {
                _deferredNotifications[notification.RepositoryId] = notification;
            }
        }
    }

    private NotificationSendResult ShowCore(PendingNotification notification)
    {
        var availability = GetAvailability();
        if (availability != NotificationSendResult.Delivered)
        {
            _ = _logger.InfoAsync($"Windows 알림 전달 불가({AppNotificationManager.Default.Setting}): {notification.FullName}");
            return availability;
        }

        try
        {
            var useNative = CanUseNativeNotifications();
            if (useNative)
            {
                var builder = new AppNotificationBuilder()
                    .AddArgument("url", notification.ReleaseUrl)
                    .AddText($"{notification.RepositoryName} 업데이트")
                    .AddText($"{notification.PreviousVersion ?? "새 Release"} → {notification.LatestVersion}")
                    .AddButton(new AppNotificationButton("Release 보기")
                        .AddArgument("url", notification.ReleaseUrl));
                AppNotificationManager.Default.Show(builder.BuildNotification());
            }
            else
            {
                _fallbackReleaseUrl = notification.ReleaseUrl;
                _trayService.ShowBalloon(
                    $"{notification.RepositoryName} 업데이트",
                    $"{notification.PreviousVersion ?? "새 Release"} → {notification.LatestVersion}");
            }
            NotificationDelivered?.Invoke(notification.RepositoryId, notification.LatestVersion);
            _ = _logger.InfoAsync($"알림 전달 완료({(useNative ? "Windows" : "트레이")}): {notification.FullName} {notification.LatestVersion}");
            return NotificationSendResult.Delivered;
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync($"알림 표시 실패: {notification.FullName}", ex);
            return NotificationSendResult.Failed;
        }
    }

    private NotificationSendResult GetAvailability()
    {
        if (!_registered)
        {
            return NotificationSendResult.NotRegistered;
        }

        if (!_nativeRegistered || AppNotificationManager.Default.Setting == AppNotificationSetting.Unsupported)
        {
            return NotificationSendResult.Delivered;
        }

        return AppNotificationManager.Default.Setting == AppNotificationSetting.Enabled
            ? NotificationSendResult.Delivered
            : NotificationSendResult.Disabled;
    }

    private bool CanUseNativeNotifications() => _nativeRegistered
        && AppNotificationManager.Default.Setting == AppNotificationSetting.Enabled;

    private void OnFallbackBalloonClicked()
    {
        if (!string.IsNullOrWhiteSpace(_fallbackReleaseUrl))
        {
            ReleaseRequested?.Invoke(_fallbackReleaseUrl);
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
        _trayService.BalloonClicked -= OnFallbackBalloonClicked;

        if (!_nativeRegistered)
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

public enum NotificationSendResult
{
    Delivered,
    Deferred,
    Disabled,
    NotRegistered,
    Failed
}
