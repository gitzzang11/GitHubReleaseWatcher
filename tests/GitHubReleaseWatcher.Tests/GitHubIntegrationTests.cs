using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Core.Services;

namespace GitHubReleaseWatcher.Tests;

[TestClass]
public sealed class GitHubIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task FetchesARealPublicRepositoryRelease()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_GITHUB_INTEGRATION_TESTS"), "1", StringComparison.Ordinal))
        {
            Assert.Inconclusive("RUN_GITHUB_INTEGRATION_TESTS=1일 때만 실제 GitHub API를 호출합니다.");
        }

        using var httpClient = new HttpClient();
        var service = new GitHubService(httpClient);
        var repository = new RepositorySubscription
        {
            Owner = "dotnet",
            Repository = "runtime",
            RepositoryUrl = "https://github.com/dotnet/runtime"
        };
        var result = await service.GetLatestReleaseAsync(repository, false, null, CancellationToken.None);
        Assert.AreEqual(GitHubResultKind.Success, result.Kind, result.FriendlyError);
        Assert.IsNotNull(result.Release);
        Assert.IsTrue(result.Release.HtmlUrl.StartsWith("https://github.com/", StringComparison.Ordinal));
    }
}
