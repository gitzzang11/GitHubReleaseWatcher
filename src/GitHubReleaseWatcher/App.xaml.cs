using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using GitHubReleaseWatcher.Core.Services;
using GitHubReleaseWatcher.Services;
using GitHubReleaseWatcher.ViewModels;

namespace GitHubReleaseWatcher;

public partial class App : System.Windows.Application
{
    private FileLogger? _logger;
    private ThemeService? _themeService;
    private DesktopSessionService? _desktopSessionService;
    private NotificationService? _notificationService;
    private TrayService? _trayService;
    private ReleaseMonitorService? _monitorService;
    private HttpClient? _httpClient;
    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;
    private bool _exiting;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var paths = new AppPaths();
        _logger = new FileLogger(paths.LogFile);
        await _logger.InfoAsync("앱 시작");

        _themeService = new ThemeService();
        _desktopSessionService = new DesktopSessionService(Dispatcher, _logger);
        _notificationService = new NotificationService(_logger, _desktopSessionService);
        _notificationService.Register();
        _notificationService.ReleaseRequested += url => Dispatcher.Invoke(() => BrowserService.Open(url));
        _httpClient = new HttpClient();

        var storage = new AppStorageService(paths, _logger);
        _mainViewModel = new MainViewModel(
            new GitHubService(_httpClient),
            storage,
            new CredentialTokenStore(),
            new StartupService(),
            _notificationService,
            _logger);
        await _mainViewModel.InitializeAsync();

        _mainWindow = new MainWindow(_mainViewModel);
        MainWindow = _mainWindow;
        _trayService = new TrayService();
        _trayService.OpenRequested += () => Dispatcher.Invoke(_mainWindow.ShowAndActivate);
        _trayService.CheckRequested += () => Dispatcher.Invoke(() => _mainViewModel.CheckAllCommand.Execute(null));
        _trayService.ExitRequested += () => Dispatcher.Invoke(ExitApplication);

        _monitorService = new ReleaseMonitorService(() => Dispatcher.InvokeAsync(
            () => _mainViewModel.CheckAllAsync(true)).Task.Unwrap());
        StartMonitor();
        _mainViewModel.CheckIntervalChanged += StartMonitor;

        if (!e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase))
        {
            _mainWindow.Show();
        }

        // 주기 타이머를 기다리지 않고 앱이 시작된 직후 한 번 확인한다.
        await _mainViewModel.CheckAllAsync(true);
    }

    private void StartMonitor()
    {
        if (_mainViewModel is null || _monitorService is null) return;
        _monitorService.Start(TimeSpan.FromMinutes(_mainViewModel.CheckIntervalMinutes));
    }

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        _mainWindow?.Exit();
        Shutdown();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        _monitorService?.Dispose();
        _trayService?.Dispose();
        _notificationService?.Dispose();
        _desktopSessionService?.Dispose();
        _themeService?.Dispose();
        _httpClient?.Dispose();
        if (_logger is not null) await _logger.InfoAsync("앱 종료");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_logger is not null) _ = _logger.ErrorAsync("처리되지 않은 UI 예외", e.Exception);
        System.Windows.MessageBox.Show("예상하지 못한 문제가 발생했습니다. 앱은 계속 실행됩니다.", "GitHub Release Watcher", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (_logger is not null && e.ExceptionObject is Exception ex) _ = _logger.ErrorAsync("처리되지 않은 앱 예외", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (_logger is not null) _ = _logger.ErrorAsync("관찰되지 않은 작업 예외", e.Exception);
        e.SetObserved();
    }
}
