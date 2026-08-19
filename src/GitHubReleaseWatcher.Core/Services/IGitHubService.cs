using GitHubReleaseWatcher.Core.Models;

namespace GitHubReleaseWatcher.Core.Services;

public interface IGitHubService
{
    Task<GitHubReleaseResult> GetLatestReleaseAsync(
        RepositorySubscription repository,
        bool includePrereleases,
        string? token,
        CancellationToken cancellationToken);
}
