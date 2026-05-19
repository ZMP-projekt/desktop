using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class TrainersViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private List<Trainer> _allTrainers = new();

    [ObservableProperty]
    private ObservableCollection<Trainer> _filteredTrainers = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

    [ObservableProperty]
    private bool _isOffline;

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

    public TrainersViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadTrainersAsync();
    }

    [RelayCommand]
    private async Task LoadTrainersAsync()
    {
        IsLoading = true;
        StatusText = "Pobieranie listy trenerów...";

        _allTrainers = await _apiService.GetRoleVerifiedTrainersAsync();
        ApplyFilter();

        StatusText = _apiService.LastResultFromCache
            ? $"Tryb offline: trenerzy ({_allTrainers.Count}) z lokalnej kopii"
            : $"Trenerzy ({_allTrainers.Count})";
        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allTrainers
            : _allTrainers.Where(t =>
                (t.FirstName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.LastName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Specialization?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        FilteredTrainers = new ObservableCollection<Trainer>(filtered);
        FooterText = $"Wyświetlono: {FilteredTrainers.Count} z {_allTrainers.Count} trenerów";
    }

}
