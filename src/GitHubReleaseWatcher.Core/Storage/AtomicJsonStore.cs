using System.Text.Json;

namespace GitHubReleaseWatcher.Core.Storage;

public sealed class AtomicJsonStore<T> where T : new()
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options;

    public AtomicJsonStore(string path)
    {
        _path = path;
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return new T();
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false)
            ?? new T();
    }

    public async Task SaveAsync(T value, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("저장 경로가 올바르지 않습니다.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";

        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
            16 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporaryPath, _path, true);
    }
}
