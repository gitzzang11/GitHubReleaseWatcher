using System.Collections.ObjectModel;
using System.ComponentModel;
using GitHubReleaseWatcher.Core.Models;
using GitHubReleaseWatcher.Core.Services;
using GitHubReleaseWatcher.Helpers;
using GitHubReleaseWatcher.Services;

namespace GitHubReleaseWatcher.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IGitHubService _gitHubService;
    private readonly AppStorageService _storage;
    private readonly CredentialTokenStore _tokenStore;
    private readonly StartupService _startupService;
    private readonly NotificationService _notificationService;
    private readonly FileLogger _logger;
    private AppSettings _settings = new();
    private string _repositoryUrl = string.Empty;
    private string? _inputError;
    private string? _bannerMessage;
    private bool _isChecking;
    private bool _isSettingsOpen;
    private DateTimeOffset? _lastFullCheckAt;
    private string? _gitHubToken;

    public MainViewModel(
        IGitHubService gitHubService,
        AppStorageService storage,
        CredentialTokenStore tokenStore,
        StartupService startupService,
        NotificationService notificationService,
        FileLogger logger)
    {
        _gitHubService = gitHubService;
        _storage = storage;
        _tokenStore = tokenStore;
        _startupService = startupService;
        _notificationService = notificationService;
        _logger = logger;
        _notificationService.NotificationDelivered += OnNotificationDelivered;

        AddRepositoryCommand = new AsyncRelayCommand(AddRepositoryAsync, () => !IsChecking && !string.IsNullOrWhiteSpace(RepositoryUrl));
        CheckAllCommand = new AsyncRelayCommand(() => CheckAllAsync(false), () => !IsChecking && Repositories.Count > 0);
        RefreshRepositoryCommand = new AsyncRelayCommand<RepositoryItemViewModel>(CheckSingleAsync, _ => !IsChecking);
        OpenReleaseCommand = new AsyncRelayCommand<RepositoryItemViewModel>(OpenReleaseAsync, item => item.Model.LatestReleaseUrl is not null);
        DeleteRepositoryCommand = new RelayCommand<RepositoryItemViewModel>(item => DeleteRequested?.Invoke(item));
        ToggleSettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);
        TestNotificationCommand = new RelayCommand(SendTestNotification);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
    }

    public ObservableCollection<RepositoryItemViewModel> Repositories { get; } = [];
    public IReadOnlyList<int> CheckIntervals { get; } = AppSettings.AllowedIntervals;

    public string RepositoryUrl
    {
        get => _repositoryUrl;
        set
        {
            if (SetProperty(ref _repositoryUrl, value))
            {
                InputError = null;
                AddRepositoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? InputError { get => _inputError; private set => SetProperty(ref _inputError, value); }
    public string? BannerMessage { get => _bannerMessage; private set { SetProperty(ref _bannerMessage, value); OnPropertyChanged(nameof(HasBanner)); } }
    public bool HasBanner => !string.IsNullOrWhiteSpace(BannerMessage);

    public bool IsChecking
    {
        get => _isChecking;
        private set
        {
            if (!SetProperty(ref _isChecking, value)) return;
            AddRepositoryCommand.RaiseCanExecuteChanged();
            CheckAllCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CheckButtonText));
        }
    }

    public bool IsSettingsOpen { get => _isSettingsOpen; set => SetProperty(ref _isSettingsOpen, value); }
    public string CheckButtonText => IsChecking ? "확인 중…" : "지금 확인";
    public int UpdateCount => Repositories.Count(r => r.HasUpdate);
    public string UpdateSummary => UpdateCount switch
    {
        0 => "모든 저장소가 최신입니다",
        1 => "업데이트 1개",
        _ => $"업데이트 {UpdateCount}개"
    };
    public string LastFullCheckText => _lastFullCheckAt is null
        ? "마지막 전체 확인: 아직 없음"
        : $"마지막 전체 확인: {ToRelativeTime(_lastFullCheckAt.Value)}";

    public bool RunAtStartup { get => _settings.RunAtStartup; set { if (_settings.RunAtStartup == value) return; _settings.RunAtStartup = value; OnPropertyChanged(); } }
    public bool MinimizeToTrayOnClose { get => _settings.MinimizeToTrayOnClose; set { if (_settings.MinimizeToTrayOnClose == value) return; _settings.MinimizeToTrayOnClose = value; OnPropertyChanged(); } }
    public int CheckIntervalMinutes { get => _settings.CheckIntervalMinutes; set { if (_settings.CheckIntervalMinutes == value) return; _settings.CheckIntervalMinutes = value; OnPropertyChanged(); } }
    public bool IncludePrereleases
    {
        get => _settings.IncludePrereleases;
        set
        {
            if (_settings.IncludePrereleases == value) return;
            _settings.IncludePrereleases = value;
            foreach (var repository in Repositories) repository.Model.ETag = null;
            OnPropertyChanged();
        }
    }
    public string? GitHubToken { get => _gitHubToken; set => SetProperty(ref _gitHubToken, value); }

    public AsyncRelayCommand AddRepositoryCommand { get; }
    public AsyncRelayCommand CheckAllCommand { get; }
    public AsyncRelayCommand<RepositoryItemViewModel> RefreshRepositoryCommand { get; }
    public AsyncRelayCommand<RepositoryItemViewModel> OpenReleaseCommand { get; }
    public RelayCommand<RepositoryItemViewModel> DeleteRepositoryCommand { get; }
    public RelayCommand ToggleSettingsCommand { get; }
    public RelayCommand TestNotificationCommand { get; }
    public AsyncRelayCommand SaveSettingsCommand { get; }

    public event Action<RepositoryItemViewModel>? DeleteRequested;
    public event Action? CheckIntervalChanged;

    public async Task InitializeAsync()
    {
        var loaded = await _storage.LoadAsync();
        _settings = loaded.Settings;
        foreach (var repository in loaded.Repositories)
        {
            Repositories.Add(new RepositoryItemViewModel(repository));
        }

        try { _startupService.SetEnabled(_settings.RunAtStartup); }
        catch (Exception ex) { await _logger.ErrorAsync("Windows 시작 프로그램 동기화 실패", ex); }

        try { GitHubToken = _tokenStore.Read(); }
        catch (Exception ex) { await _logger.ErrorAsync("GitHub Token 로드 실패", ex); }

        RaiseSettingsProperties();
        RaiseRepositorySummary();
    }

    public async Task CheckAllAsync(bool isAutomatic)
    {
        if (IsChecking || Repositories.Count == 0) return;
        IsChecking = true;
        BannerMessage = null;
        try
        {
            foreach (var repository in Repositories.ToList())
            {
                await CheckRepositoryCoreAsync(repository);
            }
            _lastFullCheckAt = DateTimeOffset.Now;
            OnPropertyChanged(nameof(LastFullCheckText));
            await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
            await _logger.InfoAsync($"전체 Release 확인 완료: {Repositories.Count}개 저장소");
        }
        catch (Exception ex)
        {
            BannerMessage = "업데이트 확인을 완료하지 못했습니다";
            await _logger.ErrorAsync(isAutomatic ? "자동 전체 확인 실패" : "수동 전체 확인 실패", ex);
        }
        finally
        {
            IsChecking = false;
            RaiseRepositorySummary();
        }
    }

    public async Task DeleteConfirmedAsync(RepositoryItemViewModel item)
    {
        Repositories.Remove(item);
        await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
        CheckAllCommand.RaiseCanExecuteChanged();
        RaiseRepositorySummary();
    }

    private async Task AddRepositoryAsync()
    {
        if (!GitHubUrlParser.TryParse(RepositoryUrl, out var address) || address is null)
        {
            InputError = "github.com의 저장소 URL을 입력해 주세요";
            return;
        }
        if (Repositories.Any(r => string.Equals(r.Model.Owner, address.Owner, StringComparison.OrdinalIgnoreCase)
            && string.Equals(r.Model.Repository, address.Repository, StringComparison.OrdinalIgnoreCase)))
        {
            InputError = "이미 등록된 저장소입니다";
            return;
        }

        IsChecking = true;
        InputError = null;
        try
        {
            var model = new RepositorySubscription
            {
                Owner = address.Owner,
                Repository = address.Repository,
                RepositoryUrl = address.CanonicalUrl,
                Status = RepositoryStatus.Checking
            };
            var result = await _gitHubService.GetLatestReleaseAsync(model, IncludePrereleases, GitHubToken, CancellationToken.None);
            if (result.Kind == GitHubResultKind.NotFound)
            {
                InputError = "저장소를 찾을 수 없습니다";
                return;
            }
            if (result.Kind is GitHubResultKind.Failed or GitHubResultKind.RateLimited)
            {
                InputError = result.FriendlyError ?? "GitHub 확인 실패";
                return;
            }

            ReleaseStateMachine.Apply(model, result, DateTimeOffset.Now);
            Repositories.Insert(0, new RepositoryItemViewModel(model));
            RepositoryUrl = string.Empty;
            await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
            CheckAllCommand.RaiseCanExecuteChanged();
            RaiseRepositorySummary();
        }
        catch (Exception ex)
        {
            InputError = "저장소를 추가하지 못했습니다";
            await _logger.ErrorAsync("저장소 추가 실패", ex);
        }
        finally { IsChecking = false; }
    }

    private async Task CheckSingleAsync(RepositoryItemViewModel repository)
    {
        if (IsChecking) return;
        IsChecking = true;
        try
        {
            await CheckRepositoryCoreAsync(repository);
            await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
        }
        finally
        {
            IsChecking = false;
            RaiseRepositorySummary();
        }
    }

    private async Task CheckRepositoryCoreAsync(RepositoryItemViewModel repository)
    {
        repository.Model.Status = RepositoryStatus.Checking;
        repository.Refresh();
        var result = await _gitHubService.GetLatestReleaseAsync(repository.Model, IncludePrereleases, GitHubToken, CancellationToken.None);
        var transition = ReleaseStateMachine.Apply(repository.Model, result, DateTimeOffset.Now);
        repository.Refresh();
        if (result.Kind is GitHubResultKind.Failed or GitHubResultKind.RateLimited)
        {
            await _logger.ErrorAsync($"저장소 확인 실패: {repository.FullName} - {result.FriendlyError}");
        }
        if (transition.ShouldNotify)
        {
            _notificationService.Show(repository.Model, transition.PreviousVersion);
        }
    }

    private void SendTestNotification()
    {
        var result = _notificationService.ShowTest();
        BannerMessage = result switch
        {
            NotificationSendResult.Delivered => "Windows에 테스트 알림을 보냈습니다",
            NotificationSendResult.Disabled => "Windows 설정에서 이 앱의 알림이 꺼져 있습니다",
            NotificationSendResult.NotRegistered => "Windows 알림을 등록하지 못했습니다. 로그를 확인해 주세요",
            _ => "테스트 알림을 보내지 못했습니다"
        };
    }

    private void OnNotificationDelivered(Guid repositoryId, string version)
    {
        var repository = Repositories.FirstOrDefault(item => item.Model.Id == repositoryId);
        if (repository is null)
        {
            return;
        }

        ReleaseStateMachine.MarkNotified(repository.Model, version);
        repository.Refresh();
        _ = SaveNotificationDeliveryAsync();
    }

    private async Task SaveNotificationDeliveryAsync()
    {
        try
        {
            await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("알림 전달 상태 저장 실패", ex);
        }
    }

    private async Task OpenReleaseAsync(RepositoryItemViewModel repository)
    {
        BrowserService.Open(repository.Model.LatestReleaseUrl);
        ReleaseStateMachine.Acknowledge(repository.Model);
        repository.Refresh();
        RaiseRepositorySummary();
        await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            _settings.Normalize();
            _startupService.SetEnabled(RunAtStartup);
            _tokenStore.Save(GitHubToken);
            await _storage.SaveSettingsAsync(_settings);
            await _storage.SaveRepositoriesAsync(Repositories.Select(r => r.Model));
            IsSettingsOpen = false;
            BannerMessage = "설정을 저장했습니다";
            CheckIntervalChanged?.Invoke();
        }
        catch (Exception ex)
        {
            BannerMessage = "설정을 저장하지 못했습니다";
            await _logger.ErrorAsync("설정 저장 실패", ex);
        }
    }

    private void RaiseSettingsProperties()
    {
        OnPropertyChanged(nameof(RunAtStartup));
        OnPropertyChanged(nameof(MinimizeToTrayOnClose));
        OnPropertyChanged(nameof(CheckIntervalMinutes));
        OnPropertyChanged(nameof(IncludePrereleases));
    }

    private void RaiseRepositorySummary()
    {
        OnPropertyChanged(nameof(UpdateCount));
        OnPropertyChanged(nameof(UpdateSummary));
        OnPropertyChanged(nameof(LastFullCheckText));
    }

    private static string ToRelativeTime(DateTimeOffset value)
    {
        var elapsed = DateTimeOffset.Now - value;
        if (elapsed.TotalSeconds < 45) return "방금 전";
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)elapsed.TotalMinutes)}분 전";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}시간 전";
        return value.LocalDateTime.ToString("M월 d일 HH:mm");
    }
}
