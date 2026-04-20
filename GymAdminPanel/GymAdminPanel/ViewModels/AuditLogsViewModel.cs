using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class AuditLogsViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private List<AuditLog> _allLogs = new();

    [ObservableProperty]
    private ObservableCollection<AuditLog> _filteredLogs = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

    // Filtrowanie
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            ApplyFilter();
        }
    }

    private string _selectedActionFilter = "Wszystkie";
    public string SelectedActionFilter
    {
        get => _selectedActionFilter;
        set
        {
            SetProperty(ref _selectedActionFilter, value);
            ApplyFilter();
        }
    }

    [ObservableProperty]
    private ObservableCollection<string> _actionFilters = new();

    public AuditLogsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadLogsAsync();
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        IsLoading = true;
        StatusText = "Pobieranie logów audytowych...";

        _allLogs = await _apiService.GetAuditLogsAsync();

        _allLogs = _allLogs.OrderByDescending(l => l.Timestamp).ToList();

        var actions = _allLogs
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        ActionFilters = new ObservableCollection<string>(
            new[] { "Wszystkie" }.Concat(actions));

        SelectedActionFilter = "Wszystkie";
        ApplyFilter();

        StatusText = _allLogs.Count > 0
            ? $"Załadowano {_allLogs.Count} wpisów"
            : "Brak danych";

        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var filtered = _allLogs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(l =>
                (l.ChangedBy?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (l.Details?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (l.Action?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (SelectedActionFilter != "Wszystkie")
        {
            filtered = filtered.Where(l => l.Action == SelectedActionFilter);
        }

        FilteredLogs = new ObservableCollection<AuditLog>(filtered);
        FooterText = $"Wyświetlono: {FilteredLogs.Count} z {_allLogs.Count} wpisów";
    }
}
