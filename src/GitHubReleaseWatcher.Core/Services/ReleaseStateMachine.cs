using GitHubReleaseWatcher.Core.Models;

namespace GitHubReleaseWatcher.Core.Services;

public static class ReleaseStateMachine
{
    public static ReleaseTransition Apply(
        RepositorySubscription repository,
        GitHubReleaseResult result,
        DateTimeOffset checkedAt)
    {
        repository.LastCheckedAt = checkedAt;

        switch (result.Kind)
        {
            case GitHubResultKind.NotModified:
                repository.Status = repository.HasUpdate
                    ? RepositoryStatus.UpdateAvailable
                    : repository.LatestVersion is null ? RepositoryStatus.NoReleases : RepositoryStatus.Current;
                repository.StatusMessage = null;
                return ReleaseTransition.None;

            case GitHubResultKind.NoReleases:
                repository.Status = RepositoryStatus.NoReleases;
                repository.StatusMessage = "공개된 Release가 없습니다";
                repository.ETag = result.ETag ?? repository.ETag;
                return ReleaseTransition.None;

            case GitHubResultKind.Success when result.Release is not null:
                repository.ETag = result.ETag ?? repository.ETag;
                return ApplyRelease(repository, result.Release);

            default:
                repository.Status = RepositoryStatus.Failed;
                repository.StatusMessage = result.FriendlyError ?? "GitHub 확인 실패";
                return ReleaseTransition.None;
        }
    }

    private static ReleaseTransition ApplyRelease(RepositorySubscription repository, ReleaseInfo release)
    {
        var isBaseline = string.IsNullOrWhiteSpace(repository.LatestVersion);
        var previousVersion = repository.LatestVersion;
        repository.LatestVersion = release.TagName;
        repository.LatestReleaseUrl = release.HtmlUrl;
        repository.StatusMessage = null;

        if (isBaseline)
        {
            repository.LastKnownVersion = release.TagName;
            repository.LastNotifiedVersion = release.TagName;
            repository.Status = RepositoryStatus.Current;
            return new ReleaseTransition(false, true, null, release.TagName, release.HtmlUrl);
        }

        var changed = !string.Equals(previousVersion, release.TagName, StringComparison.OrdinalIgnoreCase);
        var shouldNotify = changed
            && !string.Equals(repository.LastNotifiedVersion, release.TagName, StringComparison.OrdinalIgnoreCase);

        if (shouldNotify)
        {
            repository.LastNotifiedVersion = release.TagName;
        }

        repository.Status = repository.HasUpdate ? RepositoryStatus.UpdateAvailable : RepositoryStatus.Current;
        return new ReleaseTransition(shouldNotify, changed, previousVersion, release.TagName, release.HtmlUrl);
    }

    public static void Acknowledge(RepositorySubscription repository)
    {
        if (repository.LatestVersion is null)
        {
            return;
        }

        repository.LastKnownVersion = repository.LatestVersion;
        repository.Status = RepositoryStatus.Current;
    }
}

public sealed record ReleaseTransition(
    bool ShouldNotify,
    bool Changed,
    string? PreviousVersion,
    string? LatestVersion,
    string? ReleaseUrl)
{
    public static ReleaseTransition None { get; } = new(false, false, null, null, null);
}
