using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Services;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _activeSection = "users";

    [ObservableProperty]
    private string _title = "Panel Administratora Siłowni";

    private readonly UsersViewModel _usersViewModel;

    public MainViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _usersViewModel = new UsersViewModel(apiService);

        ShowUsers();
    }

    [RelayCommand]
    private void ShowUsers()
    {
        ActiveSection = "users";
        var view = new GymAdminPanel.Views.UsersView();
        view.DataContext = _usersViewModel;
        CurrentView = view;
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
