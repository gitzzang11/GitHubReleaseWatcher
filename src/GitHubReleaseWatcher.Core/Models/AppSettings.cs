namespace GitHubReleaseWatcher.Core.Models;

public sealed class AppSettings
{
    public bool RunAtStartup { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public int CheckIntervalMinutes { get; set; } = 30;
    public bool IncludePrereleases { get; set; }

    public static readonly int[] AllowedIntervals = [15, 30, 60, 180, 360];

    public void Normalize()
    {
        if (!AllowedIntervals.Contains(CheckIntervalMinutes))
        {
            CheckIntervalMinutes = 30;
        }
    }
}
