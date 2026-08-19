using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Helpers;

namespace GitHubReleaseWatcher.ViewModels;

public sealed class RepositoryItemViewModel : ObservableObject
{
    public RepositoryItemViewModel(RepositorySubscription model) => Model = model;

    public RepositorySubscription Model { get; }
    public string Name => Model.Repository;
    public string FullName => Model.FullName;
    public string? LastKnownVersion => Model.LastKnownVersion;
    public string? LatestVersion => Model.LatestVersion;
    public bool HasUpdate => Model.HasUpdate;
    public RepositoryStatus Status => Model.Status;
    public string StatusText => Model.Status switch
    {
        RepositoryStatus.Current => "최신",
        RepositoryStatus.UpdateAvailable => "업데이트 있음",
        RepositoryStatus.Checking => "확인 중",
        RepositoryStatus.Failed => Model.StatusMessage ?? "확인 실패",
        RepositoryStatus.NoReleases => "Release 없음",
        _ => "알 수 없음"
    };
    public string StatusGlyph => Model.Status switch
    {
        RepositoryStatus.Current => "✓",
        RepositoryStatus.UpdateAvailable => "↑",
        RepositoryStatus.Checking => "↻",
        RepositoryStatus.Failed => "!",
        RepositoryStatus.NoReleases => "—",
        _ => "·"
    };
    public string VersionText => Model.LatestVersion is null
        ? "버전 정보 없음"
        : Model.HasUpdate ? $"{Model.LastKnownVersion}  →  {Model.LatestVersion}" : Model.LatestVersion;
    public string LastCheckedText => Model.LastCheckedAt is null
        ? "아직 확인하지 않음"
        : ToRelativeTime(Model.LastCheckedAt.Value);

    public void Refresh()
    {
        OnPropertyChanged(nameof(LastKnownVersion));
        OnPropertyChanged(nameof(LatestVersion));
        OnPropertyChanged(nameof(HasUpdate));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(LastCheckedText));
    }

    private static string ToRelativeTime(DateTimeOffset value)
    {
        var elapsed = DateTimeOffset.Now - value;
        if (elapsed.TotalSeconds < 45) return "방금 전";
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)elapsed.TotalMinutes)}분 전";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}시간 전";
        return value.LocalDateTime.ToString("M월 d일 HH:mm");
    }
}
