using System.Text;
using System.IO;

namespace GitHubReleaseWatcher.Services;

public sealed class FileLogger
{
    private const long MaxBytes = 1_000_000;
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileLogger(string path) => _path = path;

    public Task InfoAsync(string message) => WriteAsync("INFO", message);
    public Task ErrorAsync(string message, Exception? exception = null) =>
        WriteAsync("ERROR", exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}");

    private async Task WriteAsync(string level, string message)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(directory);
            RotateIfNeeded();
            var sanitized = message.Replace('\r', ' ').Replace('\n', ' ');
            await File.AppendAllTextAsync(_path,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {sanitized}{Environment.NewLine}",
                Encoding.UTF8).ConfigureAwait(false);
        }
        catch
        {
            // Logging must never terminate the app.
        }
        finally
        {
            _gate.Release();
        }
    }

    private void RotateIfNeeded()
    {
        var file = new FileInfo(_path);
        if (!file.Exists || file.Length < MaxBytes)
        {
            return;
        }

        var archive = _path + ".1";
        File.Move(_path, archive, true);
    }
}
