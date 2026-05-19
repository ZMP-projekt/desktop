using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Linq;
using System.Threading;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private CancellationTokenSource? _statusAutoDismissCts;
    private bool _isHandlingSessionExpired;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _activeSection = "users";

    [ObservableProperty]
    private string _title = "Panel Administratora Siłowni";

    [ObservableProperty]
    private string _currentSectionTitle = "Dashboard";

    [ObservableProperty]
    private bool _isStatusVisible;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private AppStatusKind _statusKind = AppStatusKind.Info;

    [ObservableProperty]
    private bool _canRetryStatus;

    [ObservableProperty]
    private bool _isOffline;

    [ObservableProperty]
    private DateTime? _lastCacheUpdatedAt;

    public string ConnectionStatusText
    {
        get
        {
            if (!IsOffline)
                return "Online";

            return LastCacheUpdatedAt.HasValue
                ? $"Offline · dane z {LastCacheUpdatedAt.Value.ToLocalTime():dd.MM HH:mm}"
                : "Offline";
        }
    }

    private readonly UsersViewModel _usersViewModel;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly ScheduleViewModel _scheduleViewModel;
    private readonly AuditLogsViewModel _auditLogsViewModel;
    private readonly TrainersViewModel _trainersViewModel;

    public MainViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _dashboardViewModel = new DashboardViewModel(apiService);
        _usersViewModel = new UsersViewModel(apiService);
        _scheduleViewModel = new ScheduleViewModel(apiService);
        _auditLogsViewModel = new AuditLogsViewModel(apiService);
        _trainersViewModel = new TrainersViewModel(apiService);

        _apiService.StatusChanged += OnStatusChanged;
        _apiService.OfflineModeChanged += OnOfflineModeChanged;
        _apiService.CacheTimestampChanged += OnCacheTimestampChanged;
        _apiService.SessionExpired += OnSessionExpired;
        IsOffline = _apiService.IsOffline;
        LastCacheUpdatedAt = _apiService.LastCacheUpdatedAt;

        ShowDashboard();
    }

    partial void OnIsOfflineChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
        _usersViewModel.IsOffline = value;
        _scheduleViewModel.IsOffline = value;
        _trainersViewModel.IsOffline = value;
    }

    partial void OnLastCacheUpdatedAtChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
    }

    private void OnOfflineModeChanged(bool isOffline)
    {
        Application.Current.Dispatcher.Invoke(() => IsOffline = isOffline);
    }

    private void OnCacheTimestampChanged(DateTime? cachedAt)
    {
        Application.Current.Dispatcher.Invoke(() => LastCacheUpdatedAt = cachedAt);
    }

    private void OnSessionExpired(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_isHandlingSessionExpired)
                return;

            _isHandlingSessionExpired = true;

            var loginWindow = new GymAdminPanel.Views.LoginWindow();
            loginWindow.Show();

            foreach (var window in Application.Current.Windows.OfType<GymAdminPanel.Views.MainWindow>().ToList())
            {
                window.Close();
            }
        });
    }

    private void OnStatusChanged(AppStatus status)
    {
        StatusKind = status.Kind;
        StatusMessage = status.Message;
        CanRetryStatus = status.CanRetry;
        IsStatusVisible = !string.IsNullOrWhiteSpace(status.Message);

        if (IsStatusVisible)
            StartStatusAutoDismiss();
    }

    [RelayCommand]
    private void DismissStatus()
    {
        _statusAutoDismissCts?.Cancel();
        IsStatusVisible = false;
        StatusMessage = string.Empty;
        CanRetryStatus = false;
    }

    private async void StartStatusAutoDismiss()
    {
        _statusAutoDismissCts?.Cancel();
        var cts = new CancellationTokenSource();
        _statusAutoDismissCts = cts;

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            if (!cts.IsCancellationRequested)
                DismissStatus();
        }
        catch (TaskCanceledException)
        {
        }
    }

    [RelayCommand]
    private async Task RetryStatusAsync()
    {
        DismissStatus();
        await RefreshCurrentSectionAsync();
    }

    [RelayCommand]
    private async Task RefreshCurrentSectionAsync()
    {
        switch (ActiveSection)
        {
            case "dashboard":
                await _dashboardViewModel.LoadDashboardCommand.ExecuteAsync(null);
                break;
            case "users":
                await _usersViewModel.RefreshCommand.ExecuteAsync(null);
                break;
            case "schedule":
                await _scheduleViewModel.LoadClassesCommand.ExecuteAsync(null);
                break;
            case "trainers":
                await _trainersViewModel.LoadTrainersCommand.ExecuteAsync(null);
                break;
            case "auditlogs":
                await _auditLogsViewModel.LoadLogsCommand.ExecuteAsync(null);
                break;
        }
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        ActiveSection = "dashboard";
        CurrentSectionTitle = "Dashboard";
        var view = new GymAdminPanel.Views.DashboardView();
        view.DataContext = _dashboardViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowUsers()
    {
        ActiveSection = "users";
        CurrentSectionTitle = "Użytkownicy";
        var view = new GymAdminPanel.Views.UsersView();
        view.DataContext = _usersViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowSchedule()
    {
        ActiveSection = "schedule";
        CurrentSectionTitle = "Harmonogram";
        var view = new GymAdminPanel.Views.ScheduleView();
        view.DataContext = _scheduleViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowTrainers()
    {
        ActiveSection = "trainers";
        CurrentSectionTitle = "Trenerzy";
        var view = new GymAdminPanel.Views.TrainersView();
        view.DataContext = _trainersViewModel;
        CurrentView = view;
        _ = _trainersViewModel.LoadTrainersCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ShowAuditLogs()
    {
        ActiveSection = "auditlogs";
        CurrentSectionTitle = "Logi audytowe";
        var view = new GymAdminPanel.Views.AuditLogsView();
        view.DataContext = _auditLogsViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void Logout(Window currentWindow)
    {
        _apiService.Logout();
        var loginWindow = new GymAdminPanel.Views.LoginWindow();
        loginWindow.Show();
        currentWindow?.Close();
    }
}
