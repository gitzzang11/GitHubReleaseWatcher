using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Core.Storage;

namespace GitHubReleaseWatcher.Services;

public sealed class AppStorageService
{
    private readonly AtomicJsonStore<AppSettings> _settings;
    private readonly AtomicJsonStore<List<RepositorySubscription>> _repositories;
    private readonly FileLogger _logger;
    private readonly SemaphoreSlim _repositorySaveGate = new(1, 1);

    public AppStorageService(AppPaths paths, FileLogger logger)
    {
        _settings = new AtomicJsonStore<AppSettings>(paths.SettingsFile);
        _repositories = new AtomicJsonStore<List<RepositorySubscription>>(paths.RepositoriesFile);
        _logger = logger;
    }

    public async Task<(AppSettings Settings, List<RepositorySubscription> Repositories)> LoadAsync()
    {
        AppSettings settings;
        List<RepositorySubscription> repositories;
        try
        {
            settings = await _settings.LoadAsync().ConfigureAwait(false);
            settings.Normalize();
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("설정 로드 실패", ex).ConfigureAwait(false);
            settings = new AppSettings();
        }

        try
        {
            repositories = await _repositories.LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("저장소 목록 로드 실패", ex).ConfigureAwait(false);
            repositories = [];
        }

        return (settings, repositories);
    }

    public Task SaveSettingsAsync(AppSettings settings) => _settings.SaveAsync(settings);
    public async Task SaveRepositoriesAsync(IEnumerable<RepositorySubscription> repositories)
    {
        var snapshot = repositories.ToList();
        await _repositorySaveGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _repositories.SaveAsync(snapshot).ConfigureAwait(false);
        }
        finally
        {
            _repositorySaveGate.Release();
        }
    }
}
