using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Services;
using GymAdminPanel.Views;
using System.Windows;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public LoginViewModel()
    {
        _apiService = new ApiService();
    }

    [RelayCommand]
    private async Task LoginAsync(Window currentWindow)
    {
        ErrorMessage = "Logowanie...";

        bool success = await _apiService.LoginAsync(Email, Password);

        if (success)
        {
            var mainViewModel = new MainViewModel(_apiService);
            var mainWindow = new MainWindow();
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