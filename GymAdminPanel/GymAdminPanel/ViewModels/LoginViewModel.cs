using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Services;
using GymAdminPanel.Views;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System;
using System.Net.Mail;

namespace GymAdminPanel.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly LocalizationService _localization = LocalizationService.Instance;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private int _passwordLength;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoggingIn;

    public bool IsLoginEnabled =>
        !IsLoggingIn &&
        !string.IsNullOrWhiteSpace(Email) &&
        PasswordLength > 0;

    public string LoginButtonText => IsLoggingIn
        ? _localization.Translate("Login.SigningIn")
        : _localization.Translate("Login.SignIn");

    public LoginViewModel()
        : this(new ApiService())
    {
    }

    public LoginViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _localization.LanguageChanged += OnLanguageChanged;
    }

    [RelayCommand]
    private void SwitchLanguage(string language)
    {
        _localization.SetLanguage(language);
    }

    [RelayCommand]
    private async Task LoginAsync(object? passwordSource)
    {
        ErrorMessage = string.Empty;

        if (IsLoggingIn)
            return;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = _localization.Translate("Login.EmailRequired");
            return;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = _localization.Translate("Login.EmailInvalid");
            return;
        }

        if (!TryReadPassword(passwordSource, out var password))
        {
            ErrorMessage = _localization.Translate("Login.PasswordRequired");
            return;
        }

        IsLoggingIn = true;
        var loginSucceeded = false;

        try
        {
            bool success = await _apiService.LoginAsync(Email.Trim(), password);
            loginSucceeded = success;

            if (success)
            {
                var mainWindow = new MainWindow();
                mainWindow.DataContext = new MainViewModel(_apiService);
                mainWindow.Show();
                if (passwordSource is PasswordBox passwordInput)
                    Window.GetWindow(passwordInput)?.Close();
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastLoginError)
                    ? _localization.Translate("Login.Failed")
                    : _apiService.LastLoginError;
            }
        }
        catch (Exception)
        {
            ErrorMessage = _localization.Translate("Login.UnexpectedError");
        }
        finally
        {
            password = string.Empty;
            if (loginSucceeded && passwordSource is PasswordBox passwordInput)
            {
                passwordInput.Clear();
                PasswordLength = 0;
            }

            IsLoggingIn = false;
        }
    }

    partial void OnEmailChanged(string value)
    {
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(IsLoginEnabled));
    }

    partial void OnPasswordLengthChanged(int value)
    {
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(IsLoginEnabled));
    }

    partial void OnIsLoggingInChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoginEnabled));
        OnPropertyChanged(nameof(LoginButtonText));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(LoginButtonText));
    }

    private static bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();
        return MailAddress.TryCreate(trimmedEmail, out var address) &&
               string.Equals(address.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadPassword(object? passwordSource, out string password)
    {
        password = passwordSource switch
        {
            PasswordBox passwordInput => passwordInput.Password,
            string passwordText => passwordText,
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(password);
    }
}
