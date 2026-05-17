using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
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
    public bool IsOffline { get; private set; }
    public DateTime? LastCacheUpdatedAt { get; private set; }
    public event Action<AppStatus>? StatusChanged;
    public event Action<bool>? OfflineModeChanged;
    public event Action<DateTime?>? CacheTimestampChanged;
    public event Action<string>? SessionExpired;

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
            LastLoginError = GetLoginErrorMessage(response.StatusCode);
            return false;
        }
        catch (HttpRequestException)
        {
            Logout();
            LastLoginError = "Nie można połączyć się z serwerem. Sprawdź internet i spróbuj ponownie.";
            return false;
        }
        catch (TaskCanceledException)
        {
            Logout();
            LastLoginError = "Serwer nie odpowiedział na czas. Spróbuj ponownie za chwilę.";
            return false;
        }
        catch (Exception)
        {
            Logout();
            LastLoginError = "Wystąpił błąd logowania. Spróbuj ponownie za chwilę.";
            return false;
        }
    }

    private static string GetLoginErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "Nieprawidłowy e-mail lub hasło.",
        HttpStatusCode.Forbidden => "Brak dostępu. Panel administracyjny jest dostępny tylko dla administratorów.",
        HttpStatusCode.BadRequest => "Sprawdź poprawność wpisanych danych.",
        HttpStatusCode.TooManyRequests => "Za dużo prób logowania. Spróbuj ponownie za chwilę.",
        >= HttpStatusCode.InternalServerError => "Serwer logowania ma chwilowy problem. Spróbuj ponownie później.",
        _ => "Nie udało się zalogować. Sprawdź dane i spróbuj ponownie."
    };

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
                SetOfflineMode(false);
                return result;
            }
            else
            {
                if (HandleAuthorizationFailure(response.StatusCode))
                    return new List<Client>();

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

    private void SetOfflineMode(bool isOffline)
    {
        if (IsOffline == isOffline)
            return;

        IsOffline = isOffline;
        OfflineModeChanged?.Invoke(IsOffline);
    }

    private void SetLastCacheUpdatedAt(DateTime? cachedAt)
    {
        LastCacheUpdatedAt = cachedAt;
        CacheTimestampChanged?.Invoke(LastCacheUpdatedAt);
    }

    private bool BlockWriteWhenOffline()
    {
        if (!IsOffline)
            return false;

        PublishStatus(
            AppStatusKind.Warning,
            "Tryb offline: możesz przeglądać ostatnią lokalną kopię danych, ale zmiany wymagają połączenia z API.",
            true);
        return true;
    }

    private bool HandleAuthorizationFailure(HttpStatusCode statusCode)
    {
        if (statusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
            return false;

        var message = statusCode == HttpStatusCode.Unauthorized
            ? "Sesja wygasła. Zaloguj się ponownie."
            : "Brak uprawnień administratora. Zaloguj się na konto z odpowiednimi uprawnieniami.";

        Logout();
        SetOfflineMode(false);
        PublishStatus(AppStatusKind.Error, message);
        SessionExpired?.Invoke(message);
        return true;
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
                SetOfflineMode(false);
                return result;
            }
            else
            {
                if (HandleAuthorizationFailure(response.StatusCode))
                    return new List<User>();

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
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/users/{userId}");
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            SetOfflineMode(true);
            PublishStatus(AppStatusKind.Error, $"Błąd usuwania użytkownika: {ex.Message}", true);
            return false;
        }
    }
    public async Task<bool> ChangeUserRoleAsync(int userId, string newRole)
    {
        if (string.IsNullOrEmpty(Token))
        {
            PublishStatus(AppStatusKind.Error, "Brak tokenu. Zaloguj się ponownie.");
            return false;
        }
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var roleParam = Uri.EscapeDataString(newRole);
            var response = await _httpClient.PatchAsync(
                $"api/admin/users/{userId}/role?role={roleParam}", null);
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                PublishStatus(
                    AppStatusKind.Error,
                    $"Błąd zmiany roli: {response.StatusCode}. {error}",
                    true);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            SetOfflineMode(true);
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
                SetOfflineMode(false);
                return result;
            }
            else
            {
                if (HandleAuthorizationFailure(response.StatusCode))
                    return new List<GymClass>();

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
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/classes", request);
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

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
            SetOfflineMode(true);
            PublishStatus(AppStatusKind.Error, $"Błąd połączenia podczas tworzenia zajęć: {ex.Message}", true);
            return false;
        }
    }
    public async Task<bool> DeleteClassAsync(int classId)
    {
        if (string.IsNullOrEmpty(Token)) return false;
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.DeleteAsync($"api/classes/{classId}");
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            SetOfflineMode(true);
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
            if (HandleAuthorizationFailure(response.StatusCode))
                return new List<Participant>();

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
                SetOfflineMode(false);
                return result;
            }
            if (HandleAuthorizationFailure(response.StatusCode))
                return new List<Location>();

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
                SetOfflineMode(false);
                return result;
            }
            if (HandleAuthorizationFailure(response.StatusCode))
                return new List<Trainer>();

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
                SetOfflineMode(false);
                return result;
            }
            else
            {
                if (HandleAuthorizationFailure(response.StatusCode))
                    return new List<AuditLog>();

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
                SetOfflineMode(false);
                return result;
            }
            if (HandleAuthorizationFailure(response.StatusCode))
                return new List<Notification>();

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
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.PatchAsync(
                $"api/notifications/{id}/read", null);
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

            return response.IsSuccessStatusCode;
        }
        catch
        {
            SetOfflineMode(true);
            return false;
        }
    }
    public async Task<bool> DeleteNotificationAsync(int id)
    {
        if (string.IsNullOrEmpty(Token)) return false;
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.DeleteAsync($"api/notifications/{id}");
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

            return response.IsSuccessStatusCode;
        }
        catch
        {
            SetOfflineMode(true);
            return false;
        }
    }
    public async Task<bool> UpdateTrainerAsync(int trainerId, UpdateTrainerRequest request)
    {
        if (string.IsNullOrEmpty(Token)) return false;
        if (BlockWriteWhenOffline()) return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);

        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/admin/trainers/{trainerId}", request);
            if (HandleAuthorizationFailure(response.StatusCode))
                return false;

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
            SetOfflineMode(true);
            PublishStatus(AppStatusKind.Error, $"Błąd połączenia podczas aktualizacji trenera: {ex.Message}", true);
            return false;
        }
    }

    private async Task<List<T>> LoadCachedListAsync<T>(string cacheKey, string errorMessage, string title)
    {
        var cached = await _cacheService.LoadWithMetadataAsync<T>(cacheKey);
        LastResultFromCache = cached.Items.Count > 0;
        SetLastCacheUpdatedAt(cached.CachedAt);
        SetOfflineMode(true);

        if (cached.Items.Count > 0)
        {
            return cached.Items;
        }

        PublishStatus(AppStatusKind.Error, errorMessage, true);
        return new List<T>();
    }

    private static string GetClassesCacheKey(DateTime date)
        => $"classes:{date:yyyy-MM-dd}";
}
