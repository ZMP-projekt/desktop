using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _activeSection = "users";

    [ObservableProperty]
    private string _title = "Panel Administratora Siłowni";

    [ObservableProperty]
    private int _unreadNotificationsCount;

    [ObservableProperty]
    private bool _isStatusVisible;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private AppStatusKind _statusKind = AppStatusKind.Info;

    [ObservableProperty]
    private bool _canRetryStatus;

    private readonly UsersViewModel _usersViewModel;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly ScheduleViewModel _scheduleViewModel;
    private readonly AuditLogsViewModel _auditLogsViewModel;
    private readonly NotificationsViewModel _notificationsViewModel;
    private readonly TrainersViewModel _trainersViewModel;

    public MainViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _dashboardViewModel = new DashboardViewModel(apiService);
        _usersViewModel = new UsersViewModel(apiService);
        _scheduleViewModel = new ScheduleViewModel(apiService);
        _auditLogsViewModel = new AuditLogsViewModel(apiService);
        _notificationsViewModel = new NotificationsViewModel(apiService);
        _trainersViewModel = new TrainersViewModel(apiService);

        _notificationsViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NotificationsViewModel.UnreadCount))
                UnreadNotificationsCount = _notificationsViewModel.UnreadCount;
        };

        _apiService.StatusChanged += OnStatusChanged;

        ShowDashboard();
    }

    private void OnStatusChanged(AppStatus status)
    {
        StatusKind = status.Kind;
        StatusMessage = status.Message;
        CanRetryStatus = status.CanRetry;
        IsStatusVisible = !string.IsNullOrWhiteSpace(status.Message);
    }

    [RelayCommand]
    private void DismissStatus()
    {
        IsStatusVisible = false;
        StatusMessage = string.Empty;
        CanRetryStatus = false;
    }

    [RelayCommand]
    private async Task RetryStatusAsync()
    {
        DismissStatus();

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
            case "notifications":
                await _notificationsViewModel.LoadNotificationsCommand.ExecuteAsync(null);
                break;
        }
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        ActiveSection = "dashboard";
        var view = new GymAdminPanel.Views.DashboardView();
        view.DataContext = _dashboardViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowUsers()
    {
        ActiveSection = "users";
        var view = new GymAdminPanel.Views.UsersView();
        view.DataContext = _usersViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowSchedule()
    {
        ActiveSection = "schedule";
        var view = new GymAdminPanel.Views.ScheduleView();
        view.DataContext = _scheduleViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowTrainers()
    {
        ActiveSection = "trainers";
        var view = new GymAdminPanel.Views.TrainersView();
        view.DataContext = _trainersViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowAuditLogs()
    {
        ActiveSection = "auditlogs";
        var view = new GymAdminPanel.Views.AuditLogsView();
        view.DataContext = _auditLogsViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowNotifications()
    {
        ActiveSection = "notifications";
        var view = new GymAdminPanel.Views.NotificationsView();
        view.DataContext = _notificationsViewModel;
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
