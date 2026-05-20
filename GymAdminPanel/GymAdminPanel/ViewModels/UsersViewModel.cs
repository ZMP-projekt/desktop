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
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private List<User> _allUsers = new();
    private bool _lastLoadFromCache;

    [ObservableProperty]
    private ObservableCollection<User> _filteredUsers = new();

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = LocalizationService.Instance.Translate("Users.Fetching");

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

    public UsersViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _localization.LanguageChanged += OnLanguageChanged;
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
        StatusText = _localization.Translate("Users.FetchingFromServer");

        var users = await _apiService.GetUsersAsync();
        _allUsers = users;
        _lastLoadFromCache = _apiService.LastResultFromCache;
        ApplyFilter();

        RefreshStatusText();

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

        FooterText = _localization.Format("Users.Footer", FilteredUsers.Count, _allUsers.Count);
    }

    [RelayCommand]
    private async Task DeleteUserAsync(User user)
    {
        if (user == null) return;

        var result = ConfirmDialog.Show(
            _localization.Translate("Users.DeleteTitle"),
            _localization.Translate("Users.DeleteMessage"),
            $"Email: {user.Email}\nID: {user.Id}",
            ConfirmDialogKind.Warning);

        if (!result) return;

        IsLoading = true;
        var success = await _apiService.DeleteUserAsync(user.Id);

        if (success)
        {
            _allUsers.Remove(user);
            ApplyFilter();
            StatusText = _localization.Format("Users.Deleted", user.Email);
            _apiService.PublishStatus(AppStatusKind.Success, StatusText);
        }
        else
        {
            StatusText = _localization.Translate("Users.DeleteFailed");
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
            _localization.Translate("Users.RoleChangeTitle"),
            _localization.Translate("Users.RoleChangeMessage"),
            $"Email: {user.Email}\n{currentRole} → {newRole}",
            ConfirmDialogKind.Question);

        if (!result) return;

        IsLoading = true;
        var success = await _apiService.ChangeUserRoleAsync(user.Id, newRole);

        if (success)
        {
            user.Role = newRole;
            ApplyFilter();
            StatusText = _localization.Format("Users.RoleChanged", user.Email, newRole);
            _apiService.PublishStatus(AppStatusKind.Success, StatusText);
        }
        else
        {
            StatusText = _localization.Translate("Users.RoleChangeFailed");
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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        StatusText = _allUsers.Count > 0
            ? _lastLoadFromCache
                ? _localization.Format("Users.OfflineLoaded", _allUsers.Count)
                : _localization.Format("Users.Loaded", _allUsers.Count)
            : _localization.Translate("Users.NoData");
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
