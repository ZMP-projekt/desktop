using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class TrainersViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private List<Trainer> _allTrainers = new();

    [ObservableProperty]
    private ObservableCollection<Trainer> _filteredTrainers = new();

    [ObservableProperty]
    private Trainer? _selectedTrainer;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

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

    [ObservableProperty]
    private bool _isEditPanelVisible;

    [ObservableProperty]
    private string _editFirstName = "";

    [ObservableProperty]
    private string _editLastName = "";

    [ObservableProperty]
    private string _editSpecialization = "";

    [ObservableProperty]
    private string _editBio = "";

    [ObservableProperty]
    private string _editPhotoUrl = "";

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

        _allTrainers = await _apiService.GetTrainersAsync();
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
                (t.Specialization?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        FilteredTrainers = new ObservableCollection<Trainer>(filtered);
        FooterText = $"Wyświetlono: {FilteredTrainers.Count} z {_allTrainers.Count} trenerów";
    }

    [RelayCommand]
    private void OpenEdit(Trainer trainer)
    {
        if (trainer == null) return;

        SelectedTrainer = trainer;
        EditFirstName = trainer.FirstName;
        EditLastName = trainer.LastName;
        EditSpecialization = trainer.Specialization;
        EditBio = trainer.Bio;
        EditPhotoUrl = trainer.PhotoUrl;
        IsEditPanelVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditPanelVisible = false;
        SelectedTrainer = null;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (SelectedTrainer == null) return;

        if (string.IsNullOrWhiteSpace(EditFirstName) || string.IsNullOrWhiteSpace(EditLastName))
        {
            _apiService.PublishStatus(AppStatusKind.Warning, "Imię i nazwisko są wymagane.");
            return;
        }

        var request = new UpdateTrainerRequest
        {
            FirstName = EditFirstName,
            LastName = EditLastName,
            Specialization = EditSpecialization,
            Bio = EditBio,
            PhotoUrl = EditPhotoUrl
        };

        IsLoading = true;
        var success = await _apiService.UpdateTrainerAsync(SelectedTrainer.Id, request);

        if (success)
        {
            SelectedTrainer.FirstName = EditFirstName;
            SelectedTrainer.LastName = EditLastName;
            SelectedTrainer.Specialization = EditSpecialization;
            SelectedTrainer.Bio = EditBio;
            SelectedTrainer.PhotoUrl = EditPhotoUrl;

            IsEditPanelVisible = false;
            SelectedTrainer = null;
            StatusText = "Dane trenera zostały zaktualizowane.";
            _apiService.PublishStatus(AppStatusKind.Success, StatusText);

            await LoadTrainersAsync();
        }
        else
        {
            StatusText = "Nie udało się zaktualizować danych trenera.";
            _apiService.PublishStatus(AppStatusKind.Error, StatusText, true);
        }

        IsLoading = false;
    }
}
