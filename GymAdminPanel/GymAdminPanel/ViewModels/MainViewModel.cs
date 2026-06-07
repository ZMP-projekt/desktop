using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan AuditLogPollingInterval = TimeSpan.FromSeconds(15);
    private readonly ApiService _apiService;
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private CancellationTokenSource? _statusAutoDismissCts;
    private readonly CancellationTokenSource _auditLogPollingCts = new();
    private readonly HashSet<string> _seenAuditLogKeys = new(StringComparer.Ordinal);
    private bool _auditLogPollingInitialized;
    private bool _isHandlingSessionExpired;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _activeSection = "users";

    [ObservableProperty]
    private string _title = LocalizationService.Instance.Translate("App.Title");

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
                return _localization.Translate("Common.Online");

            return LastCacheUpdatedAt.HasValue
                ? _localization.Format("Common.OfflineDataFrom", LastCacheUpdatedAt.Value.ToLocalTime())
                : _localization.Translate("Common.Offline");
        }
    }

    private readonly UsersViewModel? _usersViewModel;
    private readonly DashboardViewModel? _dashboardViewModel;
    private readonly ScheduleViewModel? _scheduleViewModel;
    private readonly AuditLogsViewModel? _auditLogsViewModel;
    private readonly TrainersViewModel? _trainersViewModel;

    public MainViewModel(ApiService apiService)
        : this(apiService, startAuditLogPolling: true, initializeViewModels: true)
    {
    }

    internal MainViewModel(
        ApiService apiService,
        bool startAuditLogPolling,
        bool initializeViewModels)
    {
        _apiService = apiService;

        if (initializeViewModels)
        {
            _dashboardViewModel = new DashboardViewModel(apiService);
            _usersViewModel = new UsersViewModel(apiService);
            _scheduleViewModel = new ScheduleViewModel(apiService);
            _auditLogsViewModel = new AuditLogsViewModel(apiService);
            _trainersViewModel = new TrainersViewModel(apiService);
        }

        _apiService.StatusChanged += OnStatusChanged;
        _apiService.OfflineModeChanged += OnOfflineModeChanged;
        _apiService.CacheTimestampChanged += OnCacheTimestampChanged;
        _apiService.SessionExpired += OnSessionExpired;
        _localization.LanguageChanged += OnLanguageChanged;
        IsOffline = _apiService.IsOffline;
        LastCacheUpdatedAt = _apiService.LastCacheUpdatedAt;

        if (initializeViewModels)
            ShowDashboard();

        if (startAuditLogPolling)
            _ = StartAuditLogPollingAsync();
    }

    partial void OnIsOfflineChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
        if (_usersViewModel != null)
            _usersViewModel.IsOffline = value;

        if (_scheduleViewModel != null)
            _scheduleViewModel.IsOffline = value;

        if (_trainersViewModel != null)
            _trainersViewModel.IsOffline = value;
    }

    partial void OnLastCacheUpdatedAtChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
    }

    private void OnOfflineModeChanged(bool isOffline)
    {
        RunOnUiThread(() => IsOffline = isOffline);
    }

    private void OnCacheTimestampChanged(DateTime? cachedAt)
    {
        RunOnUiThread(() => LastCacheUpdatedAt = cachedAt);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Title = _localization.Translate("App.Title");
        OnPropertyChanged(nameof(ConnectionStatusText));
        CurrentSectionTitle = ActiveSection switch
        {
            "dashboard" => _localization.Translate("Nav.Dashboard"),
            "users" => _localization.Translate("Nav.Users"),
            "schedule" => _localization.Translate("Nav.Schedule"),
            "trainers" => _localization.Translate("Nav.Trainers"),
            "auditlogs" => _localization.Translate("Nav.AuditLogs"),
            _ => CurrentSectionTitle
        };
    }

    private void OnSessionExpired(string message)
    {
        RunOnUiThread(() =>
        {
            if (_isHandlingSessionExpired)
                return;

            _isHandlingSessionExpired = true;
            _auditLogPollingCts.Cancel();

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

    private async Task StartAuditLogPollingAsync()
    {
        while (!_auditLogPollingCts.Token.IsCancellationRequested)
        {
            try
            {
                await PollAuditLogsAsync(_auditLogPollingCts.Token);
                await Task.Delay(AuditLogPollingInterval, _auditLogPollingCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                try
                {
                    await Task.Delay(AuditLogPollingInterval, _auditLogPollingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task PollAuditLogsAsync(CancellationToken cancellationToken)
    {
        var logs = await _apiService.GetAuditLogsAsync();
        if (cancellationToken.IsCancellationRequested || _apiService.LastResultFromCache)
            return;

        var currentKeys = logs
            .Select(BuildAuditLogKey)
            .ToHashSet(StringComparer.Ordinal);

        if (!_auditLogPollingInitialized)
        {
            _seenAuditLogKeys.Clear();
            foreach (var key in currentKeys)
                _seenAuditLogKeys.Add(key);

            _auditLogPollingInitialized = true;
            return;
        }

        var newLogs = logs
            .Where(log => !_seenAuditLogKeys.Contains(BuildAuditLogKey(log)))
            .OrderByDescending(log => log.Timestamp)
            .ToList();

        foreach (var key in currentKeys)
            _seenAuditLogKeys.Add(key);

        if (newLogs.Count == 0)
            return;

        var newestLog = newLogs.First();
        var message = newLogs.Count == 1
            ? _localization.Format("Audit.NotificationSingle", newestLog.ActionDisplay)
            : _localization.Format("Audit.NotificationMultiple", newLogs.Count, newestLog.ActionDisplay);

        RunOnUiThread(() =>
        {
            _apiService.PublishStatus(AppStatusKind.Info, message);
            if (ActiveSection == "auditlogs" && _auditLogsViewModel != null)
                _ = _auditLogsViewModel.LoadLogsCommand.ExecuteAsync(null);
        });
    }

    internal Task PollAuditLogsOnceAsync()
        => PollAuditLogsAsync(CancellationToken.None);

    private static string BuildAuditLogKey(AuditLog log)
        => string.Join(
            "|",
            log.Timestamp.ToUniversalTime().Ticks,
            log.ChangedBy ?? string.Empty,
            log.Action ?? string.Empty,
            log.Details ?? string.Empty);

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
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
                if (_dashboardViewModel != null)
                    await _dashboardViewModel.LoadDashboardCommand.ExecuteAsync(null);
                break;
            case "users":
                if (_usersViewModel != null)
                    await _usersViewModel.RefreshCommand.ExecuteAsync(null);
                break;
            case "schedule":
                if (_scheduleViewModel != null)
                    await _scheduleViewModel.LoadClassesCommand.ExecuteAsync(null);
                break;
            case "trainers":
                if (_trainersViewModel != null)
                    await _trainersViewModel.LoadTrainersCommand.ExecuteAsync(null);
                break;
            case "auditlogs":
                if (_auditLogsViewModel != null)
                    await _auditLogsViewModel.LoadLogsCommand.ExecuteAsync(null);
                break;
        }
    }

    [RelayCommand]
    private void SwitchLanguage(string language)
    {
        _localization.SetLanguage(language);
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        ActiveSection = "dashboard";
        CurrentSectionTitle = _localization.Translate("Nav.Dashboard");
        var view = new GymAdminPanel.Views.DashboardView();
        view.DataContext = _dashboardViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowUsers()
    {
        ActiveSection = "users";
        CurrentSectionTitle = _localization.Translate("Nav.Users");
        var view = new GymAdminPanel.Views.UsersView();
        view.DataContext = _usersViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowSchedule()
    {
        ActiveSection = "schedule";
        CurrentSectionTitle = _localization.Translate("Nav.Schedule");
        var view = new GymAdminPanel.Views.ScheduleView();
        view.DataContext = _scheduleViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void ShowTrainers()
    {
        ActiveSection = "trainers";
        CurrentSectionTitle = _localization.Translate("Nav.Trainers");
        var view = new GymAdminPanel.Views.TrainersView();
        view.DataContext = _trainersViewModel;
        CurrentView = view;
        if (_trainersViewModel != null)
            _ = _trainersViewModel.LoadTrainersCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ShowAuditLogs()
    {
        ActiveSection = "auditlogs";
        CurrentSectionTitle = _localization.Translate("Nav.AuditLogs");
        var view = new GymAdminPanel.Views.AuditLogsView();
        view.DataContext = _auditLogsViewModel;
        CurrentView = view;
    }

    [RelayCommand]
    private void Logout(Window currentWindow)
    {
        _auditLogPollingCts.Cancel();
        _apiService.Logout();
        var loginWindow = new GymAdminPanel.Views.LoginWindow();
        loginWindow.Show();
        currentWindow?.Close();
    }

    public void Dispose()
    {
        _statusAutoDismissCts?.Cancel();
        _statusAutoDismissCts?.Dispose();
        _auditLogPollingCts.Cancel();
        _auditLogPollingCts.Dispose();

        _apiService.StatusChanged -= OnStatusChanged;
        _apiService.OfflineModeChanged -= OnOfflineModeChanged;
        _apiService.CacheTimestampChanged -= OnCacheTimestampChanged;
        _apiService.SessionExpired -= OnSessionExpired;
        _localization.LanguageChanged -= OnLanguageChanged;

        GC.SuppressFinalize(this);
    }
}
