using System.Net;
using System.Text;
using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Core.Services;

namespace GitHubReleaseWatcher.Tests;

[TestClass]
public sealed class GitHubServiceTests
{
    [TestMethod]
    public async Task ExcludesPrereleaseByDefault()
    {
        var service = CreateService("""
            [
              {"tag_name":"v2.0-beta","html_url":"https://x/beta","draft":false,"prerelease":true,"published_at":"2026-01-02T00:00:00Z"},
              {"tag_name":"v1.0","html_url":"https://x/stable","draft":false,"prerelease":false,"published_at":"2026-01-01T00:00:00Z"}
            ]
            """);
        var result = await service.GetLatestReleaseAsync(CreateRepository(), false, null, CancellationToken.None);
        Assert.AreEqual("v1.0", result.Release!.TagName);
    }

    [TestMethod]
    public async Task IncludesPrereleaseWhenEnabled()
    {
        var service = CreateService("""
            [
              {"tag_name":"v2.0-beta","html_url":"https://x/beta","draft":false,"prerelease":true,"published_at":"2026-01-02T00:00:00Z"},
              {"tag_name":"v1.0","html_url":"https://x/stable","draft":false,"prerelease":false,"published_at":"2026-01-01T00:00:00Z"}
            ]
            """);
        var result = await service.GetLatestReleaseAsync(CreateRepository(), true, null, CancellationToken.None);
        Assert.AreEqual("v2.0-beta", result.Release!.TagName);
    }

    [TestMethod]
    public async Task EmptyArrayMeansNoReleases()
    {
        var result = await CreateService("[]").GetLatestReleaseAsync(CreateRepository(), false, null, CancellationToken.None);
        Assert.AreEqual(GitHubResultKind.NoReleases, result.Kind);
    }

    [TestMethod]
    public async Task InvalidJsonReturnsFriendlyFailure()
    {
        var result = await CreateService("not-json").GetLatestReleaseAsync(CreateRepository(), false, null, CancellationToken.None);
        Assert.AreEqual(GitHubResultKind.Failed, result.Kind);
        Assert.IsNotNull(result.FriendlyError);
    }

    [TestMethod]
    public async Task SendsEtagAndHandlesNotModified()
    {
        var handler = new InspectingHandler(HttpStatusCode.NotModified);
        var service = new GitHubService(new HttpClient(handler));
        var repository = CreateRepository();
        repository.ETag = "\"cached-etag\"";
        var result = await service.GetLatestReleaseAsync(repository, false, null, CancellationToken.None);
        Assert.AreEqual(GitHubResultKind.NotModified, result.Kind);
        Assert.AreEqual("\"cached-etag\"", handler.IfNoneMatch);
    }

    [TestMethod]
    public async Task HandlesRateLimitWithoutThrowing()
    {
        var handler = new InspectingHandler(HttpStatusCode.TooManyRequests);
        var result = await new GitHubService(new HttpClient(handler))
            .GetLatestReleaseAsync(CreateRepository(), false, null, CancellationToken.None);
        Assert.AreEqual(GitHubResultKind.RateLimited, result.Kind);
        Assert.IsNotNull(result.FriendlyError);
    }

    [TestMethod]
    public async Task HandlesMissingRepositoryWithoutThrowing()
    {
        var handler = new InspectingHandler(HttpStatusCode.NotFound);
        var result = await new GitHubService(new HttpClient(handler))
            .GetLatestReleaseAsync(CreateRepository(), false, null, CancellationToken.None);
        Assert.AreEqual(GitHubResultKind.NotFound, result.Kind);
    }

    private static GitHubService CreateService(string json) => new(new HttpClient(new StubHandler(json)));
    private static RepositorySubscription CreateRepository() => new() { Owner = "o", Repository = "r", RepositoryUrl = "https://github.com/o/r" };

    private sealed class StubHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private sealed class InspectingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public string? IfNoneMatch { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IfNoneMatch = request.Headers.IfNoneMatch.FirstOrDefault()?.Tag;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
