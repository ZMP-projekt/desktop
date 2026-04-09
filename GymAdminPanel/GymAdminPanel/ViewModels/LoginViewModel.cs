using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Services;
using GymAdminPanel.Views;
using GymAdminPanel.ViewModels;
using System.Windows;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel()
    {
        _apiService = new ApiService();
    }

    [RelayCommand]
    private async Task LoginAsync(Window currentWindow)
    {
        ErrorMessage = string.Empty;
        bool success = await _apiService.LoginAsync(Email, Password);

        if (success)
        {
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel(_apiService);
            mainWindow.Show();
            currentWindow.Close();
        }
        else
        {
            ErrorMessage = "Błędny e-mail lub hasło!";
        }
    }

    [RelayCommand]
    private void OpenRegister()
    {
        var registerWindow = new GymAdminPanel.Views.RegisterWindow();
        registerWindow.ShowDialog();
    }
}