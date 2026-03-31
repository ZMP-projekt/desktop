using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _message = "";

    public RegisterViewModel()
    {
        _apiService = new ApiService();
    }

    [RelayCommand]
    private async  Task RegisterAsync(Window currentWindow)
    {
        Message = "Rejestracja w toku...";

        bool success = await _apiService.RegisterAsync(FirstName, LastName, Email, Password);

        if (success)
        {
            MessageBox.Show("Konto utworzone pomyślnie! Możesz się teraz zalogować.", "Sukces");
            currentWindow.Close();
        }
        else
        {
            Message = "Błąd rejestracji.";
        }
    }
}