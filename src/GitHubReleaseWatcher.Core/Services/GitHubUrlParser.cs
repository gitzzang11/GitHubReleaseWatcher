using GitHubReleaseWatcher.Core.Models;

namespace GitHubReleaseWatcher.Core.Services;

public static class GitHubUrlParser
{
    public static bool TryParse(string? input, out RepositoryAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(input)
            || !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            return false;
        }

        var owner = Uri.UnescapeDataString(segments[0]).Trim();
        var repository = Uri.UnescapeDataString(segments[1]).Trim();
        if (repository.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repository = repository[..^4];
        }

        if (!IsValidSegment(owner) || !IsValidSegment(repository))
        {
            return false;
        }

        address = new RepositoryAddress(owner, repository,
            $"https://github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}");
        return true;
    }

    private static bool IsValidSegment(string value) => value.Length is > 0 and <= 100
        && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
}

public sealed record RepositoryAddress(string Owner, string Repository, string CanonicalUrl);
