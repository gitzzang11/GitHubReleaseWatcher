using System.IO;

namespace GitHubReleaseWatcher.Services;

public sealed class AppPaths
{
    public AppPaths(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GitHubReleaseWatcher");
    }

    public string Root { get; }
    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string RepositoriesFile => Path.Combine(Root, "repositories.json");
    public string LogFile => Path.Combine(Root, "logs", "app.log");
}
