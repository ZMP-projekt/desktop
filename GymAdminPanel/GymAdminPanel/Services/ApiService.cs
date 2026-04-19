using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GymAdminPanel.Models;

namespace GymAdminPanel.Services;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
}

public class ApiService
{
    private readonly HttpClient _httpClient;
    public string Token { get; private set; } = string.Empty;

    public ApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api-j6d6.onrender.com/");
    }
    public async Task<bool> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var requestData = new
        {
            email = email,
            password = password,
            role = "ROLE_USER",
            firstName = firstName,
            lastName = lastName
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", requestData);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                {
                    Token = result.Token;
                    System.Windows.MessageBox.Show("Rejestracja udana!\nZostałeś automatycznie zalogowany.", "Sukces");
                }
                return true;
            }
            else
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(
                    $"Serwer odrzucił rejestrację!\n" +
                    $"Status: {response.StatusCode}\n\n" +
                    $"Szczegóły: {errorDetails}\n\n" +
                    $"URL: auth/register",
                    "Raport z API");
                return false;
            }
        }
        catch (Exception ex) 
        {
            System.Windows.MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd");
            return false;
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var requestData = new { email = email, password = password };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", requestData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                {
                    Token = result.Token;
                    return true;
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<List<Client>> GetClientsAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            System.Windows.MessageBox.Show("Brak tokenu. Zaloguj się najpierw przed pobraniem danych.", "Błąd autoryzacji");
            return new List<Client>();
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/admin/users");
            if (response.IsSuccessStatusCode)
            {
                var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
                return clients ?? new List<Client>();
            }
            else
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(
                    $"Serwer odmówił wydania listy użytkowników!\nStatus: {response.StatusCode}\nSzczegóły: {errorDetails}",
                    "Raport z API");
                return new List<Client>();
            }
        }
        catch (Exception ex)
        {
                System.Windows.MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd krytyczny");
                return new List<Client>();
        }
    }
    public void Logout()
    {
        Token = string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
    public async Task<List<User>> GetUsersAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            System.Windows.MessageBox.Show("Brak tokenu. Zaloguj się najpierw.", "Błąd autoryzacji");
            return new List<User>();
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/admin/users");
            if (response.IsSuccessStatusCode)
            {
                var users = await response.Content.ReadFromJsonAsync<List<User>>();
                return users ?? new List<User>();
            }
            else
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(
                    $"Błąd pobierania użytkowników!\nStatus: {response.StatusCode}\n{errorDetails}",
                    "Błąd API");
                return new List<User>();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd krytyczny");
            return new List<User>();
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd usuwania: {ex.Message}", "Błąd");
            return false;
        }
    }

    public async Task<bool> ChangeUserRoleAsync(int userId, string newRole)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        var requestData = new { role = newRole };

        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/admin/users/{userId}/role", requestData);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd zmiany roli: {ex.Message}", "Błąd");
            return false;
        }
    }

    public async Task<List<GymClass>> GetClassesByDateAsync(DateTime date)
    {
        if (string.IsNullOrEmpty(Token))
        {
            System.Windows.MessageBox.Show("Brak tokenu. Zaloguj się najpierw.", "Błąd autoryzacji");
            return new List<GymClass>();
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            string dateParam = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var response = await _httpClient.GetAsync($"api/classes/by-date?date={Uri.EscapeDataString(dateParam)}");

            if (response.IsSuccessStatusCode)
            {
                var classes = await response.Content.ReadFromJsonAsync<List<GymClass>>();
                return classes ?? new List<GymClass>();
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(
                    $"Błąd pobierania zajęć!\nStatus: {response.StatusCode}\n{error}",
                    "Błąd API");
                return new List<GymClass>();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd");
            return new List<GymClass>();
        }
    }

    public async Task<bool> CreateClassAsync(CreateClassRequest request)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/classes", request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(
                    $"Błąd tworzenia zajęć!\nStatus: {response.StatusCode}\n{error}\n\n" +
                    "Błąd API");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd");
            return false;
        }
    }

    public async Task<bool> DeleteClassAsync(int classId)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.DeleteAsync($"api/classes/{classId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd usuwania: {ex.Message}", "Błąd");
            return false;
        }
    }

    public async Task<List<Participant>> GetClassParticipantsAsync(int classId)
    {
        if (string.IsNullOrEmpty(Token)) return new List<Participant>();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync($"api/classes/{classId}/participants");
            if (response.IsSuccessStatusCode)
            {
                var participants = await response.Content.ReadFromJsonAsync<List<Participant>>();
                return participants ?? new List<Participant>();
            }
            return new List<Participant>();
        }
        catch
        {
            return new List<Participant>();
        }
    }
}