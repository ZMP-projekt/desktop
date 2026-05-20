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
    private static LocalizationService Localization => LocalizationService.Instance;
    private readonly HttpClient _httpClient;
    private readonly OfflineCacheService _cacheService;
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
        : this(httpClient, new OfflineCacheService())
    {
    }

    public ApiService(HttpClient httpClient, OfflineCacheService cacheService)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
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
                        LastLoginError = Localization.Translate("Login.RoleVerificationFailed");
                        return false;
                    }

                    if (!string.Equals(currentUser.Role, "ROLE_ADMIN", StringComparison.Ordinal))
                    {
                        Logout();
                        LastLoginError = Localization.Translate("Login.AdminOnlyError");
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
            LastLoginError = Localization.Translate("Login.ConnectionError");
            return false;
        }
        catch (TaskCanceledException)
        {
            Logout();
            LastLoginError = Localization.Translate("Login.Timeout");
            return false;
        }
        catch (Exception)
        {
            Logout();
            LastLoginError = Localization.Translate("Login.GenericError");
            return false;
        }
    }

    private static string GetLoginErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => Localization.Translate("Login.InvalidCredentials"),
        HttpStatusCode.Forbidden => Localization.Translate("Login.InvalidCredentials"),
        HttpStatusCode.BadRequest => Localization.Translate("Login.BadRequest"),
        HttpStatusCode.TooManyRequests => Localization.Translate("Login.TooManyRequests"),
        >= HttpStatusCode.InternalServerError => Localization.Translate("Login.ServerProblem"),
        _ => Localization.Translate("Login.Failed")
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
                LastResultFromCache = false;
                SetOfflineMode(false);
                await TrySaveCacheAsync("users", result);
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
                LastResultFromCache = false;
                SetOfflineMode(false);
                await TrySaveCacheAsync(GetClassesCacheKey(date), result);
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
                LastResultFromCache = false;
                SetOfflineMode(false);
                await TrySaveCacheAsync("locations", result);
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
                LastResultFromCache = false;
                SetOfflineMode(false);
                await TrySaveCacheAsync("trainers", result);
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

    public async Task<List<Trainer>> GetRoleVerifiedTrainersAsync()
    {
        var trainers = await GetTrainersAsync();
        var trainersFromCache = LastResultFromCache;

        var users = await GetUsersAsync();
        var usersFromCache = LastResultFromCache;
        LastResultFromCache = trainersFromCache || usersFromCache;

        if (users.Count == 0)
            return new List<Trainer>();

        var trainerUsers = users
            .Where(IsTrainerRole)
            .ToList();

        var profileCandidates = GetProfileCandidates(
            await LoadScheduledTrainerNameCountsAsync(),
            trainers);

        var usedProfileIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trainersWithProfiles = new List<Trainer>();
        var usersWithoutProfiles = new List<User>();

        foreach (var user in trainerUsers)
        {
            var profile = FindProfileForUser(user, trainers);
            if (profile == null)
            {
                usersWithoutProfiles.Add(user);
                continue;
            }

            usedProfileIdentities.Add(BuildProfileIdentity(profile));
            trainersWithProfiles.Add(CreateTrainerFromUser(user, profile));
        }

        foreach (var user in usersWithoutProfiles)
        {
            var profile = profileCandidates
                .FirstOrDefault(profile => !usedProfileIdentities.Contains(BuildProfileIdentity(profile)));

            if (profile != null)
                usedProfileIdentities.Add(BuildProfileIdentity(profile));

            trainersWithProfiles.Add(CreateTrainerFromUser(user, profile));
        }

        return trainersWithProfiles
            .OrderBy(trainer => trainer.FullName)
            .ThenBy(trainer => trainer.Email)
            .ToList();
    }

    private static bool IsTrainerRole(User user)
        => string.Equals(user.Role, "ROLE_TRAINER", StringComparison.Ordinal) ||
           string.Equals(user.Role, "TRAINER", StringComparison.Ordinal);

    private static string BuildPersonKey(string? firstName, string? lastName)
        => $"{firstName} {lastName}".Trim();

    private static Trainer? FindProfileForUser(User user, List<Trainer> trainers)
        => trainers.FirstOrDefault(trainer => MatchesTrainerUser(trainer, user)) ??
           trainers.FirstOrDefault(trainer => EmailLocalPartMatchesName(user.Email, trainer));

    private async Task<Dictionary<string, int>> LoadScheduledTrainerNameCountsAsync()
    {
        var cachedClasses = await _cacheService.LoadListsByKeyPrefixAsync<GymClass>("classes:");

        return cachedClasses
            .Select(gymClass => NormalizeLookupValue(gymClass.TrainerName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static List<Trainer> GetProfileCandidates(
        Dictionary<string, int> scheduledTrainerNameCounts,
        List<Trainer> trainers)
    {
        return trainers
            .Where(HasProfileDisplayData)
            .OrderByDescending(trainer => scheduledTrainerNameCounts
                .GetValueOrDefault(NormalizeLookupValue(BuildPersonKey(trainer.FirstName, trainer.LastName))))
            .ThenByDescending(trainer => HasUsefulProfileValue(trainer.Bio, "Brak opisu"))
            .ThenByDescending(trainer => HasUsefulProfileValue(trainer.Specialization, "Do uzupełnienia"))
            .ThenBy(trainer => trainer.FullName)
            .ToList();
    }

    private static bool HasProfileDisplayData(Trainer trainer)
        => !string.IsNullOrWhiteSpace(BuildPersonKey(trainer.FirstName, trainer.LastName));

    private static bool HasUsefulProfileValue(string? value, string defaultValue)
        => !string.IsNullOrWhiteSpace(value) &&
           !string.Equals(value.Trim(), defaultValue, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesTrainerUser(Trainer trainer, User user)
        => trainer.UserId == user.Id ||
           (trainer.Id > 0 && trainer.Id == user.Id) ||
           (!string.IsNullOrWhiteSpace(trainer.Email) &&
            string.Equals(trainer.Email, user.Email, StringComparison.OrdinalIgnoreCase)) ||
           (!string.IsNullOrWhiteSpace(BuildPersonKey(user.FirstName, user.LastName)) &&
            string.Equals(
                BuildPersonKey(trainer.FirstName, trainer.LastName),
                BuildPersonKey(user.FirstName, user.LastName),
                StringComparison.OrdinalIgnoreCase));

    private static bool EmailLocalPartMatchesName(string? email, Trainer trainer)
    {
        var localPart = email?.Split('@')[0].Trim();
        if (string.IsNullOrWhiteSpace(localPart))
            return false;

        return NamePartMatches(localPart, trainer.FirstName) ||
               NamePartMatches(localPart, trainer.LastName);
    }

    private static bool NamePartMatches(string localPart, string? namePart)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            return false;

        var normalizedLocalPart = NormalizeLookupValue(localPart);
        var normalizedNamePart = NormalizeLookupValue(namePart);

        return normalizedNamePart.Length >= 3 &&
               normalizedLocalPart.Contains(normalizedNamePart, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLookupValue(string value)
        => new(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

    private static string BuildProfileIdentity(Trainer trainer)
        => NormalizeLookupValue(
            $"{trainer.FirstName}|{trainer.LastName}|{trainer.Specialization}|{trainer.Bio}|{trainer.PhotoUrl}");

    private static Trainer CreateTrainerFromUser(User user, Trainer? profile)
        => new()
        {
            Id = profile?.Id > 0 ? profile.Id : user.Id,
            UserId = user.Id,
            Email = user.Email,
            FirstName = !string.IsNullOrWhiteSpace(profile?.FirstName) ? profile.FirstName : user.FirstName,
            LastName = !string.IsNullOrWhiteSpace(profile?.LastName) ? profile.LastName : user.LastName,
            Specialization = !string.IsNullOrWhiteSpace(profile?.Specialization) ? profile.Specialization : "Do uzupełnienia",
            Bio = !string.IsNullOrWhiteSpace(profile?.Bio) ? profile.Bio : "Brak opisu",
            PhotoUrl = profile?.PhotoUrl ?? string.Empty
        };

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
                LastResultFromCache = false;
                SetOfflineMode(false);
                await TrySaveCacheAsync("audit-logs", result);
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

    private async Task TrySaveCacheAsync<T>(string cacheKey, List<T> items)
    {
        try
        {
            await _cacheService.SaveAsync(cacheKey, items);
        }
        catch
        {
            SetLastCacheUpdatedAt(null);
        }
    }

    private static string GetClassesCacheKey(DateTime date)
        => $"classes:{date:yyyy-MM-dd}";
}
