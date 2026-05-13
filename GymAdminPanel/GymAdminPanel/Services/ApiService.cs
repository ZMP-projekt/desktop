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
    private readonly OfflineCacheService _cacheService = new();
    public string Token { get; private set; } = string.Empty;
    public string LastLoginError { get; private set; } = string.Empty;
    public bool LastResultFromCache { get; private set; }
    public event Action<AppStatus>? StatusChanged;

    public ApiService()
        : this(new HttpClient { BaseAddress = new Uri("https://api-j6d6.onrender.com/") })
    {
    }

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api-j6d6.onrender.com/");
    }
    public async Task<bool> LoginAsync(string email, string password)
    {
        LastLoginError = string.Empty;
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
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Token);

                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser == null)
                    {
                        Logout();
                        LastLoginError = "Nie udało się zweryfikować roli użytkownika.";
                        return false;
                    }

                    if (!string.Equals(currentUser.Role, "ROLE_ADMIN", StringComparison.Ordinal))
                    {
                        Logout();
                        LastLoginError = "Brak dostępu. Panel administracyjny jest dostępny tylko dla administratorów.";
                        return false;
                    }

                    return true;
                }
            }
            LastLoginError = "Błędny e-mail lub hasło!";
            return false;
        }
        catch (Exception ex)
        {
            Logout();
            LastLoginError = $"Błąd logowania: {ex.Message}";
            return false;
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("api/users/me");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<User>();
    }
    public async Task<List<Client>> GetClientsAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            PublishStatus(AppStatusKind.Error, "Brak tokenu. Zaloguj się ponownie.");
            return new List<Client>();
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/admin/users");
            if (response.IsSuccessStatusCode)
            {
                var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
                var result = clients ?? new List<Client>();
                await _cacheService.SaveAsync("clients", result);
                LastResultFromCache = false;
                return result;
            }
            else
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                return await LoadCachedListAsync<Client>(
                    "clients",
                    $"Serwer odmówił wydania listy użytkowników!\nStatus: {response.StatusCode}\nSzczegóły: {errorDetails}",
                    "Raport z API");
            }
        }
        catch (Exception ex)
        {
                return await LoadCachedListAsync<Client>(
                    "clients",
                    $"Błąd połączenia: {ex.Message}",
                    "Błąd krytyczny");
        }
    }
    public void Logout()
    {
        Token = string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public void PublishStatus(AppStatusKind kind, string message, bool canRetry = false)
    {
        StatusChanged?.Invoke(new AppStatus
        {
            Kind = kind,
            Message = message,
            CanRetry = canRetry
        });
    }
    public async Task<List<User>> GetUsersAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            PublishStatus(AppStatusKind.Error, "Brak tokenu. Zaloguj się ponownie.");
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
                var result = users ?? new List<User>();
                await _cacheService.SaveAsync("users", result);
                LastResultFromCache = false;
                return result;
            }
            else
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                return await LoadCachedListAsync<User>(
                    "users",
                    $"Błąd pobierania użytkowników!\nStatus: {response.StatusCode}\n{errorDetails}",
                    "Błąd API");
            }
        }
        catch (Exception ex)
        {
            return await LoadCachedListAsync<User>(
                "users",
                $"Błąd połączenia: {ex.Message}",
                "Błąd krytyczny");
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
            PublishStatus(AppStatusKind.Error, $"Błąd usuwania użytkownika: {ex.Message}", true);
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
            PublishStatus(AppStatusKind.Error, $"Błąd zmiany roli: {ex.Message}", true);
            return false;
        }
    }
    public async Task<List<GymClass>> GetClassesByDateAsync(DateTime date)
    {
        if (string.IsNullOrEmpty(Token))
        {
            PublishStatus(AppStatusKind.Error, "Brak tokenu. Zaloguj się ponownie.");
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
                var result = classes ?? new List<GymClass>();
                await _cacheService.SaveAsync(GetClassesCacheKey(date), result);
                LastResultFromCache = false;
                return result;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                return await LoadCachedListAsync<GymClass>(
                    GetClassesCacheKey(date),
                    $"Błąd pobierania zajęć!\nStatus: {response.StatusCode}\n{error}",
                    "Błąd API");
            }
        }
        catch (Exception ex)
        {
            return await LoadCachedListAsync<GymClass>(
                GetClassesCacheKey(date),
                $"Błąd połączenia: {ex.Message}",
                "Błąd");
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
                PublishStatus(
                    AppStatusKind.Error,
                    $"Nie udało się utworzyć zajęć. Status: {response.StatusCode}. {error}",
                    true);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            PublishStatus(AppStatusKind.Error, $"Błąd połączenia podczas tworzenia zajęć: {ex.Message}", true);
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
            PublishStatus(AppStatusKind.Error, $"Błąd usuwania zajęć: {ex.Message}", true);
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
    public async Task<List<Location>> GetLocationsAsync()
    {
        if (string.IsNullOrEmpty(Token)) return new List<Location>();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/locations");
            if (response.IsSuccessStatusCode)
            {
                var locations = await response.Content.ReadFromJsonAsync<List<Location>>();
                var result = locations ?? new List<Location>();
                await _cacheService.SaveAsync("locations", result);
                LastResultFromCache = false;
                return result;
            }
            return await LoadCachedListAsync<Location>("locations", "Nie udało się pobrać lokalizacji.", "Tryb offline");
        }
        catch
        {
            return await LoadCachedListAsync<Location>("locations", "Brak połączenia podczas pobierania lokalizacji.", "Tryb offline");
        }
    }
    public async Task<List<Trainer>> GetTrainersAsync()
    {
        if (string.IsNullOrEmpty(Token)) return new List<Trainer>();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/trainers");
            if (response.IsSuccessStatusCode)
            {
                var trainers = await response.Content.ReadFromJsonAsync<List<Trainer>>();
                var result = trainers ?? new List<Trainer>();
                await _cacheService.SaveAsync("trainers", result);
                LastResultFromCache = false;
                return result;
            }
            return await LoadCachedListAsync<Trainer>("trainers", "Nie udało się pobrać trenerów.", "Tryb offline");
        }
        catch
        {
            return await LoadCachedListAsync<Trainer>("trainers", "Brak połączenia podczas pobierania trenerów.", "Tryb offline");
        }
    }
    public async Task<List<AuditLog>> GetAuditLogsAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            PublishStatus(AppStatusKind.Error, "Brak tokenu. Zaloguj się ponownie.");
            return new List<AuditLog>();
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/admin/audit-logs");
            if (response.IsSuccessStatusCode)
            {
                var logs = await response.Content.ReadFromJsonAsync<List<AuditLog>>();
                var result = logs ?? new List<AuditLog>();
                await _cacheService.SaveAsync("audit-logs", result);
                LastResultFromCache = false;
                return result;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                return await LoadCachedListAsync<AuditLog>(
                    "audit-logs",
                    $"Błąd pobierania logów!\nStatus: {response.StatusCode}\n{error}",
                    "Błąd API");
            }
        }
        catch (Exception ex)
        {
            return await LoadCachedListAsync<AuditLog>(
                "audit-logs",
                $"Błąd połączenia: {ex.Message}",
                "Błąd");
        }
    }
    public async Task<List<Notification>> GetNotificationsAsync()
    {
        if (string.IsNullOrEmpty(Token)) return new List<Notification>();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.GetAsync("api/notifications");
            if (response.IsSuccessStatusCode)
            {
                var notifications = await response.Content.ReadFromJsonAsync<List<Notification>>();
                var result = notifications ?? new List<Notification>();
                await _cacheService.SaveAsync("notifications", result);
                LastResultFromCache = false;
                return result;
            }
            return await LoadCachedListAsync<Notification>("notifications", "Nie udało się pobrać powiadomień.", "Tryb offline");
        }
        catch (Exception ex)
        {
            return await LoadCachedListAsync<Notification>(
                "notifications",
                $"Błąd pobierania powiadomień: {ex.Message}",
                "Błąd");
        }
    }
    public async Task<bool> MarkNotificationReadAsync(int id)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.PatchAsync(
                $"api/notifications/{id}/read", null);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
    public async Task<bool> DeleteNotificationAsync(int id)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.DeleteAsync($"api/notifications/{id}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
    public async Task<bool> UpdateTrainerAsync(int trainerId, UpdateTrainerRequest request)
    {
        if (string.IsNullOrEmpty(Token)) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/admin/trainers/{trainerId}", request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                PublishStatus(
                    AppStatusKind.Error,
                    $"Nie udało się zaktualizować trenera. Status: {response.StatusCode}. {error}",
                    true);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            PublishStatus(AppStatusKind.Error, $"Błąd połączenia podczas aktualizacji trenera: {ex.Message}", true);
            return false;
        }
    }

    private async Task<List<T>> LoadCachedListAsync<T>(string cacheKey, string errorMessage, string title)
    {
        var cached = await _cacheService.LoadAsync<T>(cacheKey);
        LastResultFromCache = cached.Count > 0;

        if (cached.Count > 0)
        {
            PublishStatus(
                AppStatusKind.Warning,
                $"{errorMessage} Pokazuję ostatnią lokalną kopię danych.",
                true);
            return cached;
        }

        PublishStatus(AppStatusKind.Error, errorMessage, true);
        return new List<T>();
    }

    private static string GetClassesCacheKey(DateTime date)
        => $"classes:{date:yyyy-MM-dd}";
}
