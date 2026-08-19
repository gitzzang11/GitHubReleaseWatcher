using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using GitHubReleaseWatcher.ViewModels;

namespace GitHubReleaseWatcher;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _allowClose;
    private bool _tokenInitialized;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.DeleteRequested += OnDeleteRequested;
        Loaded += OnLoaded;
    }

    public void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    public void Exit()
    {
        _allowClose = true;
        Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TokenBox.Password = _viewModel.GitHubToken ?? string.Empty;
        _tokenInitialized = true;
        RepositoryUrlBox.Focus();
    }

    private async void OnDeleteRequested(RepositoryItemViewModel item)
    {
        var result = System.Windows.MessageBox.Show(
            $"{item.FullName} 저장소 감시를 중지할까요?",
            "저장소 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.DeleteConfirmedAsync(item);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || !_viewModel.MinimizeToTrayOnClose) return;
        e.Cancel = true;
        Hide();
    }

    private void OnTokenChanged(object sender, RoutedEventArgs e)
    {
        if (_tokenInitialized) _viewModel.GitHubToken = TokenBox.Password;
    }

    private void OnRepositoryUrlKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.AddRepositoryCommand.CanExecute(null))
        {
            _viewModel.AddRepositoryCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnOverlayClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsSettingsOpen) _viewModel.IsSettingsOpen = false;
    }
}
