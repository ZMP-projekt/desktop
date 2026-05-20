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
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private List<AuditLog> _allLogs = new();
    private bool _lastLoadFromCache;

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

    private string _selectedActionFilter = LocalizationService.Instance.Translate("Common.All");
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
        _localization.LanguageChanged += OnLanguageChanged;
        _ = LoadLogsAsync();
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        IsLoading = true;
        StatusText = _localization.Translate("Audit.Fetching");

        _allLogs = await _apiService.GetAuditLogsAsync();
        _lastLoadFromCache = _apiService.LastResultFromCache;

        _allLogs = _allLogs.OrderByDescending(l => l.Timestamp).ToList();

        RefreshActionFilters();

        SelectedActionFilter = _localization.Translate("Common.All");
        ApplyFilter();

        RefreshStatusText();

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
                (l.Action?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                l.ActionDisplay.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                l.DetailsDisplay.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedActionFilter != _localization.Translate("Common.All"))
        {
            filtered = filtered.Where(l =>
                l.Action == SelectedActionFilter ||
                l.ActionDisplay == SelectedActionFilter);
        }

        FilteredLogs = new ObservableCollection<AuditLog>(filtered);
        FooterText = _localization.Format("Audit.Footer", FilteredLogs.Count, _allLogs.Count);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedRawAction = ResolveSelectedRawAction();
        RefreshActionFilters();

        if (string.IsNullOrWhiteSpace(selectedRawAction))
            SelectedActionFilter = _localization.Translate("Common.All");
        else
            SelectedActionFilter = _localization.TranslateAuditAction(selectedRawAction);

        ApplyFilter();
        RefreshStatusText();
    }

    private void RefreshActionFilters()
    {
        var actionLabels = _allLogs
            .Select(log => log.ActionDisplay)
            .Distinct()
            .OrderBy(action => action)
            .ToList();

        ActionFilters = new ObservableCollection<string>(
            new[] { _localization.Translate("Common.All") }.Concat(actionLabels));
    }

    private string? ResolveSelectedRawAction()
    {
        if (SelectedActionFilter == "Wszystkie" || SelectedActionFilter == "All")
            return null;

        return _allLogs
            .Select(log => log.Action)
            .FirstOrDefault(action =>
                action == SelectedActionFilter ||
                _localization.TranslateAuditAction(action) == SelectedActionFilter ||
                _localization.TranslateAuditActionForLanguage(action, "pl") == SelectedActionFilter ||
                _localization.TranslateAuditActionForLanguage(action, "en") == SelectedActionFilter);
    }

    private void RefreshStatusText()
    {
        StatusText = _allLogs.Count > 0
            ? _lastLoadFromCache
                ? _localization.Format("Audit.OfflineLoaded", _allLogs.Count)
                : _localization.Format("Audit.Loaded", _allLogs.Count)
            : _localization.Translate("Audit.NoData");
    }
}
