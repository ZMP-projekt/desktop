using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using GymAdminPanel.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private List<User> _allUsers = new();

    [ObservableProperty]
    private ObservableCollection<User> _filteredUsers = new();

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Pobieranie danych...";

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

    public UsersViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadUsersAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        IsLoading = true;
        StatusText = "Pobieranie użytkowników z serwera...";

        var users = await _apiService.GetUsersAsync();
        _allUsers = users;
        ApplyFilter();

        StatusText = _allUsers.Count > 0
            ? _apiService.LastResultFromCache
                ? $"Tryb offline: załadowano {_allUsers.Count} użytkowników z lokalnej kopii"
                : $"Załadowano {_allUsers.Count} użytkowników"
            : "Brak danych lub błąd połączenia";

        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsers
            : _allUsers.Where(u =>
                (u.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.FirstName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.LastName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Role?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        var selectedUserId = SelectedUser?.Id;

        FilteredUsers = new ObservableCollection<User>(filtered.OrderBy(u => u.Id));
        if (selectedUserId.HasValue)
            SelectedUser = FilteredUsers.FirstOrDefault(u => u.Id == selectedUserId.Value);

        FooterText = $"Wyświetlono: {FilteredUsers.Count} z {_allUsers.Count} użytkowników";
    }

    [RelayCommand]
    private async Task DeleteUserAsync(User user)
    {
        if (user == null) return;

        var result = ConfirmDialog.Show(
            "Potwierdzenie usunięcia",
            "Czy na pewno chcesz usunąć użytkownika?",
            $"Email: {user.Email}\nID: {user.Id}",
            ConfirmDialogKind.Warning);

        if (!result) return;

        IsLoading = true;
        var success = await _apiService.DeleteUserAsync(user.Id);

        if (success)
        {
            _allUsers.Remove(user);
            ApplyFilter();
            StatusText = $"Użytkownik {user.Email} został usunięty.";
            _apiService.PublishStatus(AppStatusKind.Success, StatusText);
        }
        else
        {
            StatusText = "Nie udało się usunąć użytkownika.";
            _apiService.PublishStatus(AppStatusKind.Error, StatusText, true);
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task ChangeRoleAsync(User user)
    {
        if (user == null) return;

        var currentRole = string.IsNullOrWhiteSpace(user.Role) ? "ROLE_USER" : user.Role;
        var newRole = GetNextRole(currentRole);

        var result = ConfirmDialog.Show(
            "Zmiana roli",
            "Zmienić rolę użytkownika?",
            $"Email: {user.Email}\n{currentRole} → {newRole}",
            ConfirmDialogKind.Question);

        if (!result) return;

        IsLoading = true;
        var success = await _apiService.ChangeUserRoleAsync(user.Id, newRole);

        if (success)
        {
            user.Role = newRole;
            ApplyFilter();
            StatusText = $"Rola użytkownika {user.Email} została zmieniona na {newRole}.";
            _apiService.PublishStatus(AppStatusKind.Success, StatusText);
        }
        else
        {
            StatusText = "Nie udało się zmienić roli użytkownika.";
        }

        IsLoading = false;
    }

    private static string GetNextRole(string role)
        => role switch
        {
            "ROLE_USER" => "ROLE_TRAINER",
            "ROLE_TRAINER" => "ROLE_ADMIN",
            "ROLE_ADMIN" => "ROLE_USER",
            _ => "ROLE_USER"
        };

    [RelayCommand]
    private void Logout(Window currentWindow)
    {
        _apiService.Logout();
        var loginWindow = new GymAdminPanel.Views.LoginWindow();
        loginWindow.Show();
        currentWindow?.Close();
    }
}
