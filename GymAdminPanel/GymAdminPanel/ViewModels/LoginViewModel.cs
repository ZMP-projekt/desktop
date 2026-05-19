using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Services;
using GymAdminPanel.Views;
using System.Windows;
using System.Threading.Tasks;
using System;
using System.Net.Mail;

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

    [ObservableProperty]
    private bool _isLoggingIn;

    public bool IsLoginEnabled =>
        !IsLoggingIn &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password);

    public string LoginButtonText => IsLoggingIn ? "Logowanie..." : "Zaloguj się";

    public LoginViewModel()
        : this(new ApiService())
    {
    }

    public LoginViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task LoginAsync(Window currentWindow)
    {
        ErrorMessage = string.Empty;

        if (IsLoggingIn)
            return;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Podaj adres e-mail.";
            return;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Podaj poprawny adres e-mail.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Podaj hasło.";
            return;
        }

        IsLoggingIn = true;

        try
        {
            bool success = await _apiService.LoginAsync(Email.Trim(), Password);

            if (success)
            {
                var mainWindow = new MainWindow();
                mainWindow.DataContext = new MainViewModel(_apiService);
                mainWindow.Show();
                currentWindow.Close();
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastLoginError)
                    ? "Nie udało się zalogować. Sprawdź dane i spróbuj ponownie."
                    : _apiService.LastLoginError;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Wystąpił nieoczekiwany błąd logowania. Spróbuj ponownie za chwilę.";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    partial void OnEmailChanged(string value)
    {
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(IsLoginEnabled));
    }

    partial void OnPasswordChanged(string value)
    {
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(IsLoginEnabled));
    }

    partial void OnIsLoggingInChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoginEnabled));
        OnPropertyChanged(nameof(LoginButtonText));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email.Trim());
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
