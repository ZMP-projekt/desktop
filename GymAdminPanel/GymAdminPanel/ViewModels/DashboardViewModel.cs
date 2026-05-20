using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private bool _lastLoadUsedCache;
    private DateTime _lastUpdatedAt = DateTime.Now;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = LocalizationService.Instance.Translate("Dashboard.FetchingSummary");

    [ObservableProperty]
    private int _usersCount;

    [ObservableProperty]
    private int _todayClassesCount;

    [ObservableProperty]
    private int _auditLogsCount;

    [ObservableProperty]
    private ObservableCollection<GymClass> _todayClasses = new();

    [ObservableProperty]
    private ObservableCollection<AuditLog> _recentAuditLogs = new();

    public bool HasTodayClasses => TodayClasses.Count > 0;

    public bool HasRecentAuditLogs => RecentAuditLogs.Count > 0;

    public DashboardViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _localization.LanguageChanged += OnLanguageChanged;
        _ = LoadDashboardAsync();
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        StatusText = _localization.Translate("Dashboard.FetchingKeyData");

        var users = await _apiService.GetUsersAsync();
        var usersFromCache = _apiService.LastResultFromCache;

        var classes = await _apiService.GetClassesByDateAsync(DateTime.Today);
        var classesFromCache = _apiService.LastResultFromCache;

        var auditLogs = await _apiService.GetAuditLogsAsync();
        var auditLogsFromCache = _apiService.LastResultFromCache;

        UsersCount = users.Count;
        TodayClassesCount = classes.Count;
        AuditLogsCount = auditLogs.Count;

        TodayClasses = new ObservableCollection<GymClass>(
            classes.OrderBy(c => c.StartTime).Take(5));

        RecentAuditLogs = new ObservableCollection<AuditLog>(
            auditLogs.OrderByDescending(l => l.Timestamp).Take(5));

        _lastLoadUsedCache = usersFromCache || classesFromCache || auditLogsFromCache;
        _lastUpdatedAt = DateTime.Now;
        RefreshStatusText();

        IsLoading = false;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshStatusText();
        OnPropertyChanged(nameof(RecentAuditLogs));
    }

    private void RefreshStatusText()
    {
        StatusText = _lastLoadUsedCache
            ? _localization.Translate("Dashboard.OfflineSummary")
            : _localization.Format("Dashboard.UpdatedAt", _lastUpdatedAt);
    }

    partial void OnTodayClassesChanged(ObservableCollection<GymClass> value)
        => OnPropertyChanged(nameof(HasTodayClasses));

    partial void OnRecentAuditLogsChanged(ObservableCollection<AuditLog> value)
        => OnPropertyChanged(nameof(HasRecentAuditLogs));
}
