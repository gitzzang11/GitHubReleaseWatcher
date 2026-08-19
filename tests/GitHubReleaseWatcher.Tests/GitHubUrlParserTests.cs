using GitHubReleaseWatcher.Core.Services;

namespace GitHubReleaseWatcher.Tests;

[TestClass]
public sealed class GitHubUrlParserTests
{
    [TestMethod]
    public void ParsesCanonicalRepositoryUrl()
    {
        var success = GitHubUrlParser.TryParse("https://github.com/Flow-Launcher/Flow.Launcher", out var result);
        Assert.IsTrue(success);
        Assert.AreEqual("Flow-Launcher", result!.Owner);
        Assert.AreEqual("Flow.Launcher", result.Repository);
    }

    [TestMethod]
    public void RemovesGitSuffixAndTrailingSlash()
    {
        Assert.IsTrue(GitHubUrlParser.TryParse("https://github.com/owner/repo.git/", out var result));
        Assert.AreEqual("repo", result!.Repository);
        Assert.AreEqual("https://github.com/owner/repo", result.CanonicalUrl);
    }

    [TestMethod]
    [DataRow("owner/repo")]
    [DataRow("https://gitlab.com/owner/repo")]
    [DataRow("https://github.com/owner")]
    [DataRow("https://github.com/owner/repo/issues")]
    [DataRow("javascript:alert(1)")]
    public void RejectsInvalidUrls(string input) => Assert.IsFalse(GitHubUrlParser.TryParse(input, out _));
}
