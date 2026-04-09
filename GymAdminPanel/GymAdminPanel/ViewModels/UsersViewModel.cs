using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
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
            ? $"Załadowano {_allUsers.Count} użytkowników"
            : "Brak danych lub błąd połączenia";

        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsers
            : _allUsers.Where(u =>
                (u.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Role?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        FilteredUsers = new ObservableCollection<User>(filtered);
        FooterText = $"Wyświetlono: {FilteredUsers.Count} z {_allUsers.Count} użytkowników";
    }

    [RelayCommand]
    private async Task DeleteUserAsync(User user)
    {
        if (user == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć użytkownika?\n\nEmail: {user.Email}\nID: {user.Id}",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;
        var success = await _apiService.DeleteUserAsync(user.Id);

        if (success)
        {
            _allUsers.Remove(user);
            ApplyFilter();
            StatusText = $"Użytkownik {user.Email} został usunięty.";
        }
        else
        {
            StatusText = "Nie udało się usunąć użytkownika.";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task ChangeRoleAsync(User user)
    {
        if (user == null) return;

        // Prosta lista ról — możesz rozbudować o osobne okno dialogowe
        var newRole = user.Role == "ROLE_USER" ? "ROLE_ADMIN" : "ROLE_USER";

        var result = MessageBox.Show(
            $"Zmienić rolę użytkownika {user.Email}?\n\n{user.Role}  →  {newRole}",
            "Zmiana roli",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;
        var success = await _apiService.ChangeUserRoleAsync(user.Id, newRole);

        if (success)
        {
            user.Role = newRole;
            ApplyFilter(); // odśwież widok
            StatusText = $"Rola użytkownika {user.Email} została zmieniona na {newRole}.";
        }
        else
        {
            StatusText = "Nie udało się zmienić roli.";
        }

        IsLoading = false;
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
