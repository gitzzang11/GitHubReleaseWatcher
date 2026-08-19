using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;

namespace GitHubReleaseWatcher.Services;

public sealed class DesktopSessionService : IDisposable
{
    private const uint DesktopSwitchDesktop = 0x0100;

    private readonly Dispatcher _dispatcher;
    private readonly FileLogger _logger;
    private readonly CancellationTokenSource _pollCancellation = new();
    private bool _isDesktopAvailable;
    private bool _sessionEventsRegistered;
    private bool _disposed;

    public DesktopSessionService(Dispatcher dispatcher, FileLogger logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _isDesktopAvailable = IsInteractiveDesktopAvailable();

        try
        {
            SystemEvents.SessionSwitch += OnSessionSwitch;
            _sessionEventsRegistered = true;
        }
        catch (Exception ex)
        {
            _ = _logger.ErrorAsync("Windows 세션 이벤트 등록 실패", ex);
        }

        _ = PollDesktopStateAsync(_pollCancellation.Token);
    }

    public bool IsDesktopAvailable => _isDesktopAvailable;

    public event Action? DesktopAvailable;

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock:
            case SessionSwitchReason.ConsoleDisconnect:
            case SessionSwitchReason.RemoteDisconnect:
            case SessionSwitchReason.SessionLogoff:
                ReportDesktopState(false);
                break;

            case SessionSwitchReason.SessionUnlock:
            case SessionSwitchReason.ConsoleConnect:
            case SessionSwitchReason.RemoteConnect:
            case SessionSwitchReason.SessionLogon:
                ReportDesktopState(IsInteractiveDesktopAvailable());
                break;
        }
    }

    private async Task PollDesktopStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                ReportDesktopState(IsInteractiveDesktopAvailable());
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("바탕화면 상태 확인 실패", ex);
        }
    }

    private void ReportDesktopState(bool isAvailable)
    {
        if (_disposed)
        {
            return;
        }

        _dispatcher.BeginInvoke(() =>
        {
            if (_disposed || _isDesktopAvailable == isAvailable)
            {
                return;
            }

            _isDesktopAvailable = isAvailable;
            if (isAvailable)
            {
                DesktopAvailable?.Invoke();
            }
        });
    }

    private static bool IsInteractiveDesktopAvailable()
    {
        var desktop = OpenInputDesktop(0, false, DesktopSwitchDesktop);
        if (desktop == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return SwitchDesktop(desktop);
        }
        finally
        {
            CloseDesktop(desktop);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pollCancellation.Cancel();
        _pollCancellation.Dispose();

        if (_sessionEventsRegistered)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint desiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SwitchDesktop(IntPtr desktop);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseDesktop(IntPtr desktop);
}
