using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GitHubReleaseWatcher.Core.Models;

namespace GitHubReleaseWatcher.Core.Services;

public sealed class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubReleaseWatcher/1.0");
        }
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<GitHubReleaseResult> GetLatestReleaseAsync(
        RepositorySubscription repository,
        bool includePrereleases,
        string? token,
        CancellationToken cancellationToken)
    {
        var owner = Uri.EscapeDataString(repository.Owner);
        var name = Uri.EscapeDataString(repository.Repository);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{owner}/{name}/releases?per_page=30");

        if (!string.IsNullOrWhiteSpace(repository.ETag))
        {
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(repository.ETag));
        }
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        }

        try
        {
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var etag = response.Headers.ETag?.Tag;

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return new GitHubReleaseResult(GitHubResultKind.NotModified, ETag: repository.ETag);
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new GitHubReleaseResult(GitHubResultKind.NotFound, FriendlyError: "저장소를 찾을 수 없습니다");
            }
            if (response.StatusCode == HttpStatusCode.TooManyRequests || IsRateLimited(response))
            {
                return new GitHubReleaseResult(GitHubResultKind.RateLimited,
                    ETag: etag,
                    FriendlyError: "GitHub 요청 한도에 도달했습니다",
                    RateLimitReset: GetRateLimitReset(response));
            }
            if (!response.IsSuccessStatusCode)
            {
                return new GitHubReleaseResult(GitHubResultKind.Failed, ETag: etag, FriendlyError: "GitHub 확인 실패");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var releases = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(
                stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            var latest = releases?
                .Where(r => !r.Draft && (includePrereleases || !r.Prerelease))
                .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                .FirstOrDefault();

            return latest is null
                ? new GitHubReleaseResult(GitHubResultKind.NoReleases, ETag: etag)
                : new GitHubReleaseResult(GitHubResultKind.Success,
                    new ReleaseInfo(latest.TagName ?? "(버전 없음)", latest.HtmlUrl ?? repository.RepositoryUrl,
                        latest.Prerelease, latest.PublishedAt ?? latest.CreatedAt ?? DateTimeOffset.MinValue), etag);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new GitHubReleaseResult(GitHubResultKind.Failed, FriendlyError: "GitHub 응답 시간이 초과됐습니다");
        }
        catch (HttpRequestException)
        {
            return new GitHubReleaseResult(GitHubResultKind.Failed, FriendlyError: "네트워크 연결을 확인해 주세요");
        }
        catch (JsonException)
        {
            return new GitHubReleaseResult(GitHubResultKind.Failed, FriendlyError: "GitHub 응답을 읽을 수 없습니다");
        }
    }

    private static bool IsRateLimited(HttpResponseMessage response) => response.StatusCode == HttpStatusCode.Forbidden
        && response.Headers.TryGetValues("X-RateLimit-Remaining", out var values)
        && values.FirstOrDefault() == "0";

    private static DateTimeOffset? GetRateLimitReset(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
            && long.TryParse(values.FirstOrDefault(), out var seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private sealed class GitHubReleaseDto
    {
        public string? TagName { get; set; }
        public string? HtmlUrl { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
