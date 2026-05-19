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

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Pobieranie podsumowania...";

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
        _ = LoadDashboardAsync();
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        StatusText = "Pobieranie najważniejszych danych...";

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

        StatusText = usersFromCache || classesFromCache || auditLogsFromCache
            ? "Tryb offline: część danych pochodzi z lokalnej kopii"
            : $"Podsumowanie zaktualizowane: {DateTime.Now:HH:mm}";

        IsLoading = false;
    }

    partial void OnTodayClassesChanged(ObservableCollection<GymClass> value)
        => OnPropertyChanged(nameof(HasTodayClasses));

    partial void OnRecentAuditLogsChanged(ObservableCollection<AuditLog> value)
        => OnPropertyChanged(nameof(HasRecentAuditLogs));
}
