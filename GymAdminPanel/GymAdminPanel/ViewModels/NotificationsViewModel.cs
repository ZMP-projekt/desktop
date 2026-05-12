using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private List<Notification> _allNotifications = new();

    [ObservableProperty]
    private ObservableCollection<Notification> _filteredNotifications = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

    [ObservableProperty]
    private int _unreadCount;

    private string _selectedFilter = "Wszystkie";
    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            SetProperty(ref _selectedFilter, value);
            ApplyFilter();
        }
    }

    public List<string> FilterOptions { get; } = new() { "Wszystkie", "Nowe", "Przeczytane" };

    public NotificationsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadNotificationsAsync();
    }

    [RelayCommand]
    private async Task LoadNotificationsAsync()
    {
        IsLoading = true;
        StatusText = "Pobieranie powiadomień...";

        _allNotifications = await _apiService.GetNotificationsAsync();
        _allNotifications = _allNotifications
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        UnreadCount = _allNotifications.Count(n => !n.Read);
        ApplyFilter();

        var offlinePrefix = _apiService.LastResultFromCache ? "Tryb offline: " : "";
        StatusText = UnreadCount > 0
            ? $"{offlinePrefix}{UnreadCount} nieprzeczytanych powiadomień"
            : $"{offlinePrefix}Wszystkie powiadomienia przeczytane";

        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var filtered = SelectedFilter switch
        {
            "Nowe" => _allNotifications.Where(n => !n.Read),
            "Przeczytane" => _allNotifications.Where(n => n.Read),
            _ => _allNotifications.AsEnumerable()
        };

        FilteredNotifications = new ObservableCollection<Notification>(filtered);
        FooterText = $"Wyświetlono: {FilteredNotifications.Count} z {_allNotifications.Count}  ·  " +
                     $"Nieprzeczytanych: {UnreadCount}";
    }

    [RelayCommand]
    private async Task MarkReadAsync(Notification notification)
    {
        if (notification == null || notification.Read) return;

        var success = await _apiService.MarkNotificationReadAsync(notification.Id);
        if (success)
        {
            notification.Read = true;
            UnreadCount = _allNotifications.Count(n => !n.Read);
            ApplyFilter();
            StatusText = UnreadCount > 0
                ? $"{UnreadCount} nieprzeczytanych powiadomień"
                : "Wszystkie powiadomienia przeczytane";
        }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        var unread = _allNotifications.Where(n => !n.Read).ToList();
        if (unread.Count == 0) return;

        IsLoading = true;
        var tasks = unread.Select(n => _apiService.MarkNotificationReadAsync(n.Id));
        await Task.WhenAll(tasks);

        foreach (var n in unread) n.Read = true;
        UnreadCount = 0;
        ApplyFilter();
        StatusText = "Wszystkie powiadomienia przeczytane";
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteNotificationAsync(Notification notification)
    {
        if (notification == null) return;

        var success = await _apiService.DeleteNotificationAsync(notification.Id);
        if (success)
        {
            _allNotifications.Remove(notification);
            if (!notification.Read) UnreadCount--;
            ApplyFilter();
        }
    }

    [RelayCommand]
    private async Task DeleteAllReadAsync()
    {
        var read = _allNotifications.Where(n => n.Read).ToList();
        if (read.Count == 0)
        {
            MessageBox.Show("Brak przeczytanych powiadomień do usunięcia.", "Info");
            return;
        }

        var confirm = MessageBox.Show(
            $"Usunąć {read.Count} przeczytanych powiadomień?",
            "Potwierdzenie",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsLoading = true;
        var tasks = read.Select(n => _apiService.DeleteNotificationAsync(n.Id));
        await Task.WhenAll(tasks);

        foreach (var n in read) _allNotifications.Remove(n);
        ApplyFilter();
        StatusText = $"Usunięto {read.Count} powiadomień";
        IsLoading = false;
    }
}
