using System.Text.Json.Serialization;

namespace GitHubReleaseWatcher.Core.Models;

public sealed class RepositorySubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Owner { get; set; }
    public required string Repository { get; set; }
    public required string RepositoryUrl { get; set; }
    public string? LastKnownVersion { get; set; }
    public string? LatestVersion { get; set; }
    public string? LatestReleaseUrl { get; set; }
    public string? LastNotifiedVersion { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public string? ETag { get; set; }
    public RepositoryStatus Status { get; set; } = RepositoryStatus.Checking;
    public string? StatusMessage { get; set; }

    [JsonIgnore]
    public string FullName => $"{Owner}/{Repository}";

    [JsonIgnore]
    public bool HasUpdate => LatestVersion is not null
        && LastKnownVersion is not null
        && !string.Equals(LatestVersion, LastKnownVersion, StringComparison.OrdinalIgnoreCase);
}

public enum RepositoryStatus
{
    Current,
    UpdateAvailable,
    Checking,
    Failed,
    NoReleases
}
