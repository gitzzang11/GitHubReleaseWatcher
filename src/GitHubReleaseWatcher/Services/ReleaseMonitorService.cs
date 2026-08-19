namespace GitHubReleaseWatcher.Services;

public sealed class ReleaseMonitorService(Func<Task> checkAsync) : IDisposable
{
    private CancellationTokenSource? _cancellation;

    public void Start(TimeSpan interval)
    {
        Stop();
        _cancellation = new CancellationTokenSource();
        _ = RunAsync(interval, _cancellation.Token);
    }

    private async Task RunAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) await checkAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void Stop() { _cancellation?.Cancel(); _cancellation?.Dispose(); _cancellation = null; }
    public void Dispose() => Stop();
}
