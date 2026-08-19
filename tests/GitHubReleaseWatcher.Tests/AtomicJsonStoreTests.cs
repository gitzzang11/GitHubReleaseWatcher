using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Core.Storage;

namespace GitHubReleaseWatcher.Tests;

[TestClass]
public sealed class AtomicJsonStoreTests
{
    [TestMethod]
    public async Task SavesAndRestoresSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GitHubReleaseWatcher.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var store = new AtomicJsonStore<AppSettings>(path);
            await store.SaveAsync(new AppSettings { CheckIntervalMinutes = 180, IncludePrereleases = true });
            var loaded = await store.LoadAsync();
            Assert.AreEqual(180, loaded.CheckIntervalMinutes);
            Assert.IsTrue(loaded.IncludePrereleases);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
