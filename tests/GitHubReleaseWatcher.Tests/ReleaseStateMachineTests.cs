using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Core.Services;

namespace GitHubReleaseWatcher.Tests;

[TestClass]
public sealed class ReleaseStateMachineTests
{
    [TestMethod]
    public void FirstReleaseBecomesBaselineWithoutNotification()
    {
        var repository = CreateRepository();
        var transition = Apply(repository, "v1.0.0");
        Assert.IsFalse(transition.ShouldNotify);
        Assert.AreEqual("v1.0.0", repository.LastKnownVersion);
        Assert.AreEqual("v1.0.0", repository.LastNotifiedVersion);
        Assert.AreEqual(RepositoryStatus.Current, repository.Status);
    }

    [TestMethod]
    public void SameReleaseDoesNotNotify()
    {
        var repository = CreateRepository();
        Apply(repository, "v1.0.0");
        var transition = Apply(repository, "v1.0.0");
        Assert.IsFalse(transition.ShouldNotify);
        Assert.IsFalse(repository.HasUpdate);
    }

    [TestMethod]
    public void NewReleaseRemainsPendingUntilDeliveryIsConfirmed()
    {
        var repository = CreateRepository();
        Apply(repository, "v1.0.0");
        var first = Apply(repository, "v1.1.0");
        var beforeDelivery = Apply(repository, "v1.1.0");
        Assert.IsTrue(first.ShouldNotify);
        Assert.AreEqual("v1.0.0", first.PreviousVersion);
        Assert.AreEqual("v1.1.0", repository.LatestVersion);
        Assert.IsTrue(repository.HasUpdate);
        Assert.IsTrue(beforeDelivery.ShouldNotify);

        ReleaseStateMachine.MarkNotified(repository, "v1.1.0");
        var afterDelivery = Apply(repository, "v1.1.0");
        Assert.IsFalse(afterDelivery.ShouldNotify);
    }

    [TestMethod]
    public void NotModifiedRetriesAnUndeliveredNotification()
    {
        var repository = CreateRepository();
        Apply(repository, "v1.0.0");
        Apply(repository, "v1.1.0");

        var transition = ReleaseStateMachine.Apply(repository,
            new GitHubReleaseResult(GitHubResultKind.NotModified), DateTimeOffset.UtcNow);

        Assert.IsTrue(transition.ShouldNotify);
        Assert.AreEqual("v1.0.0", transition.PreviousVersion);
        Assert.AreEqual("v1.1.0", transition.LatestVersion);
    }

    [TestMethod]
    public void AcknowledgingAReleasePreventsALaterNotification()
    {
        var repository = CreateRepository();
        Apply(repository, "v1.0.0");
        Apply(repository, "v1.1.0");

        ReleaseStateMachine.Acknowledge(repository);
        var transition = Apply(repository, "v1.1.0");

        Assert.IsFalse(transition.ShouldNotify);
        Assert.AreEqual("v1.1.0", repository.LastKnownVersion);
        Assert.AreEqual("v1.1.0", repository.LastNotifiedVersion);
    }

    [TestMethod]
    public void NoReleaseIsHandledWithoutFailure()
    {
        var repository = CreateRepository();
        var transition = ReleaseStateMachine.Apply(repository,
            new GitHubReleaseResult(GitHubResultKind.NoReleases), DateTimeOffset.UtcNow);
        Assert.IsFalse(transition.ShouldNotify);
        Assert.AreEqual(RepositoryStatus.NoReleases, repository.Status);
    }

    private static ReleaseTransition Apply(RepositorySubscription repository, string version) =>
        ReleaseStateMachine.Apply(repository,
            new GitHubReleaseResult(GitHubResultKind.Success,
                new ReleaseInfo(version, $"https://github.com/o/r/releases/tag/{version}", false, DateTimeOffset.UtcNow)),
            DateTimeOffset.UtcNow);

    private static RepositorySubscription CreateRepository() => new()
    {
        Owner = "o", Repository = "r", RepositoryUrl = "https://github.com/o/r"
    };
}
