namespace GitHubReleaseWatcher.Core.Models;

public sealed record ReleaseInfo(string TagName, string HtmlUrl, bool IsPrerelease, DateTimeOffset PublishedAt);

public sealed record GitHubReleaseResult(
    GitHubResultKind Kind,
    ReleaseInfo? Release = null,
    string? ETag = null,
    string? FriendlyError = null,
    DateTimeOffset? RateLimitReset = null);

public enum GitHubResultKind
{
    Success,
    NotModified,
    NoReleases,
    NotFound,
    RateLimited,
    Failed
}
