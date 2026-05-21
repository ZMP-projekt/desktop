using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;

namespace GymAdminPanel.Services;

public partial class LocalizationService : ObservableObject
{
    private const string Polish = "pl";
    private const string English = "en";
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GymAdminPanel");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "language.txt");

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        [Polish] = new()
        {
            ["App.Title"] = "Panel Administratora Siłowni",
            ["App.AdminPanel"] = "PANEL ADMINISTRATORA",
            ["App.AdminPanelTitle"] = "Panel administratora",
            ["App.AdminOnly"] = "Dostęp tylko dla administratorów",
            ["Common.Yes"] = "Tak",
            ["Common.No"] = "Nie",
            ["Common.Today"] = "Dziś",
            ["Common.Search"] = "Szukaj",
            ["Common.Status"] = "Status",
            ["Common.Filter"] = "Filtruj:",
            ["Common.All"] = "Wszystkie",
            ["Common.Loading"] = "Ładowanie...",
            ["Common.Online"] = "Online",
            ["Common.Offline"] = "Offline",
            ["Common.OfflineDataFrom"] = "Offline · dane z {0:dd.MM HH:mm}",
            ["Topbar.Refresh"] = "Odśwież",
            ["Topbar.LanguageTooltip"] = "Zmień język",
            ["Login.Title"] = "Logowanie",
            ["Login.Password"] = "Hasło",
            ["Login.SignIn"] = "Zaloguj się",
            ["Login.SigningIn"] = "Logowanie...",
            ["Login.EmailRequired"] = "Podaj adres e-mail.",
            ["Login.EmailInvalid"] = "Podaj poprawny adres e-mail.",
            ["Login.PasswordRequired"] = "Podaj hasło.",
            ["Login.Failed"] = "Nie udało się zalogować. Sprawdź dane i spróbuj ponownie.",
            ["Login.UnexpectedError"] = "Wystąpił nieoczekiwany błąd logowania. Spróbuj ponownie za chwilę.",
            ["Login.RoleVerificationFailed"] = "Nie udało się zweryfikować roli użytkownika.",
            ["Login.AdminOnlyError"] = "Brak dostępu. Panel administracyjny jest dostępny tylko dla administratorów.",
            ["Login.ConnectionError"] = "Nie można połączyć się z serwerem. Sprawdź internet i spróbuj ponownie.",
            ["Login.Timeout"] = "Serwer nie odpowiedział na czas. Spróbuj ponownie za chwilę.",
            ["Login.GenericError"] = "Wystąpił błąd logowania. Spróbuj ponownie za chwilę.",
            ["Login.InvalidCredentials"] = "Nieprawidłowy e-mail lub hasło.",
            ["Login.BadRequest"] = "Sprawdź poprawność wpisanych danych.",
            ["Login.TooManyRequests"] = "Za dużo prób logowania. Spróbuj ponownie za chwilę.",
            ["Login.ServerProblem"] = "Serwer logowania ma chwilowy problem. Spróbuj ponownie później.",
            ["Nav.Dashboard"] = "Dashboard",
            ["Nav.Users"] = "Użytkownicy",
            ["Nav.Schedule"] = "Harmonogram",
            ["Nav.Trainers"] = "Trenerzy",
            ["Nav.AuditLogs"] = "Logi audytowe",
            ["Nav.Logout"] = "Wyloguj",
            ["Dashboard.Users"] = "Użytkownicy",
            ["Dashboard.TodayClasses"] = "Dzisiejsze zajęcia",
            ["Dashboard.AuditEntries"] = "Wpisy audytu",
            ["Dashboard.TodaySchedule"] = "Dzisiejszy harmonogram",
            ["Dashboard.RecentActions"] = "Ostatnie akcje",
            ["Dashboard.NoClasses"] = "Brak zaplanowanych zajęć",
            ["Dashboard.NoClassesDescription"] = "Na dzisiaj nie ma przypisanych treningów grupowych ani sesji indywidualnych.",
            ["Dashboard.NoRecentActions"] = "Brak ostatnich akcji",
            ["Dashboard.Loading"] = "Ładowanie dashboardu...",
            ["Dashboard.FetchingSummary"] = "Pobieranie podsumowania...",
            ["Dashboard.FetchingKeyData"] = "Pobieranie najważniejszych danych...",
            ["Dashboard.OfflineSummary"] = "Tryb offline: część danych pochodzi z lokalnej kopii",
            ["Dashboard.UpdatedAt"] = "Podsumowanie zaktualizowane: {0:HH:mm}",
            ["Users.Title"] = "Zarządzanie Użytkownikami",
            ["Users.SearchPlaceholder"] = "Filtruj po imieniu, nazwisku, emailu lub roli...",
            ["Users.NameHeader"] = "IMIĘ I NAZWISKO",
            ["Users.RoleHeader"] = "ROLA",
            ["Users.ActionsHeader"] = "AKCJE",
            ["Users.ChangeRoleTooltip"] = "Zmień rolę",
            ["Users.DeleteTooltip"] = "Usuń użytkownika",
            ["Users.Loading"] = "Ładowanie użytkowników...",
            ["Users.Fetching"] = "Pobieranie danych...",
            ["Users.FetchingFromServer"] = "Pobieranie użytkowników z serwera...",
            ["Users.OfflineLoaded"] = "Tryb offline: załadowano {0} użytkowników z lokalnej kopii",
            ["Users.Loaded"] = "Załadowano {0} użytkowników",
            ["Users.NoData"] = "Brak danych lub błąd połączenia",
            ["Users.Footer"] = "Wyświetlono: {0} z {1} użytkowników",
            ["Users.DeleteTitle"] = "Potwierdzenie usunięcia",
            ["Users.DeleteMessage"] = "Czy na pewno chcesz usunąć użytkownika?",
            ["Users.Deleted"] = "Użytkownik {0} został usunięty.",
            ["Users.DeleteFailed"] = "Nie udało się usunąć użytkownika.",
            ["Users.RoleChangeTitle"] = "Zmiana roli",
            ["Users.RoleChangeMessage"] = "Zmienić rolę użytkownika?",
            ["Users.RoleChanged"] = "Rola użytkownika {0} została zmieniona na {1}.",
            ["Users.RoleChangeFailed"] = "Nie udało się zmienić roli użytkownika.",
            ["Schedule.Title"] = "Harmonogram Zajęć",
            ["Schedule.Trainer"] = "Trener",
            ["Schedule.Location"] = "Lokalizacja",
            ["Schedule.Type"] = "Typ zajęć",
            ["Schedule.SearchPlaceholder"] = "Szukaj po nazwie, trenerze lub lokalizacji...",
            ["Schedule.TimeHeader"] = "GODZINA",
            ["Schedule.NameHeader"] = "NAZWA ZAJĘĆ",
            ["Schedule.TrainerHeader"] = "TRENER",
            ["Schedule.LocationHeader"] = "LOKALIZACJA",
            ["Schedule.TypeHeader"] = "TYP",
            ["Schedule.ParticipantsHeader"] = "UCZESTNICY",
            ["Schedule.NoClasses"] = "Brak zajęć dla wybranych filtrów",
            ["Schedule.NoClassesHint"] = "Zmień datę lub filtry, aby zobaczyć inne zajęcia.",
            ["Schedule.Loading"] = "Ładowanie harmonogramu...",
            ["Schedule.AllTrainers"] = "Wszyscy trenerzy",
            ["Schedule.AllLocations"] = "Wszystkie lokalizacje",
            ["Schedule.AllTypes"] = "Wszystkie typy",
            ["Schedule.GroupClasses"] = "Zajęcia grupowe",
            ["Schedule.PersonalTraining"] = "Trening osobisty",
            ["Schedule.AllSpots"] = "Wszystkie miejsca",
            ["Schedule.AvailableSpots"] = "Wolne miejsca",
            ["Schedule.FullClasses"] = "Pełne zajęcia",
            ["Schedule.FetchingForDate"] = "Pobieranie zajęć na {0}...",
            ["Schedule.OfflineForDate"] = "Tryb offline: zajęcia na {0} z lokalnej kopii",
            ["Schedule.ClassesForDate"] = "Zajęcia na {0}",
            ["Schedule.NoClassesForDate"] = "Brak zajęć na {0}",
            ["Schedule.Footer"] = "Wyświetlono: {0} z {1} zajęć  ·  Zapisanych uczestników: {2}",
            ["Trainers.Title"] = "Trenerzy",
            ["Trainers.SearchPlaceholder"] = "Szukaj po imieniu, nazwisku lub specjalizacji...",
            ["Trainers.Loading"] = "Ładowanie trenerów...",
            ["Trainers.Fetching"] = "Pobieranie listy trenerów...",
            ["Trainers.OfflineLoaded"] = "Tryb offline: trenerzy ({0}) z lokalnej kopii",
            ["Trainers.Loaded"] = "Trenerzy ({0})",
            ["Trainers.Footer"] = "Wyświetlono: {0} z {1} trenerów",
            ["Audit.Title"] = "Logi Audytowe",
            ["Audit.SearchPlaceholder"] = "Szukaj po osobie, akcji lub szczegółach...",
            ["Audit.DateHeader"] = "DATA I GODZINA",
            ["Audit.ChangedByHeader"] = "WYKONAŁ",
            ["Audit.ActionHeader"] = "AKCJA",
            ["Audit.DetailsHeader"] = "SZCZEGÓŁY",
            ["Audit.Loading"] = "Ładowanie logów...",
            ["Audit.Fetching"] = "Pobieranie logów audytowych...",
            ["Audit.OfflineLoaded"] = "Tryb offline: załadowano {0} wpisów z lokalnej kopii",
            ["Audit.Loaded"] = "Załadowano {0} wpisów",
            ["Audit.NoData"] = "Brak danych",
            ["Audit.Footer"] = "Wyświetlono: {0} z {1} wpisów",
            ["Audit.NotificationSingle"] = "Nowe zdarzenie: {0}",
            ["Audit.NotificationMultiple"] = "Nowe zdarzenia: {0}. Ostatnie: {1}",
            ["Audit.Action.Booking"] = "ZAPIS",
            ["Audit.Action.CancelBooking"] = "ANULOWANIE",
            ["Audit.Action.CreateClass"] = "NOWE ZAJĘCIA",
            ["Audit.Action.CreateTrainerProfile"] = "PROFIL TRENERA",
            ["Audit.Action.Create"] = "UTWORZENIE",
            ["Audit.Action.PurchaseMembership"] = "ZAKUP KARNETU",
            ["Audit.Action.Reschedule"] = "ZMIANA TERMINU",
            ["Audit.Action.Update"] = "AKTUALIZACJA",
            ["Audit.Action.Delete"] = "USUNIĘCIE",
            ["Audit.Detail.BookingFor"] = "Zapis na: {0}",
            ["Audit.Detail.Canceled"] = "Anulowano: {0}",
            ["Audit.Detail.ClassAt"] = "Zajęcia: {0} w: {1}",
            ["Audit.Detail.IdTo"] = "ID: {0} na {1}",
            ["Audit.Detail.PurchasedMembership"] = "Zakupiono karnet {0} za {1}"
        },
        [English] = new()
        {
            ["App.Title"] = "Gym Admin Panel",
            ["App.AdminPanel"] = "ADMIN PANEL",
            ["App.AdminPanelTitle"] = "Administrator panel",
            ["App.AdminOnly"] = "Administrators only",
            ["Common.Yes"] = "Yes",
            ["Common.No"] = "No",
            ["Common.Today"] = "Today",
            ["Common.Search"] = "Search",
            ["Common.Status"] = "Status",
            ["Common.Filter"] = "Filter:",
            ["Common.All"] = "All",
            ["Common.Loading"] = "Loading...",
            ["Common.Online"] = "Online",
            ["Common.Offline"] = "Offline",
            ["Common.OfflineDataFrom"] = "Offline · data from {0:dd.MM HH:mm}",
            ["Topbar.Refresh"] = "Refresh",
            ["Topbar.LanguageTooltip"] = "Change language",
            ["Login.Title"] = "Sign in",
            ["Login.Password"] = "Password",
            ["Login.SignIn"] = "Sign in",
            ["Login.SigningIn"] = "Signing in...",
            ["Login.EmailRequired"] = "Enter an email address.",
            ["Login.EmailInvalid"] = "Enter a valid email address.",
            ["Login.PasswordRequired"] = "Enter a password.",
            ["Login.Failed"] = "Could not sign in. Check your credentials and try again.",
            ["Login.UnexpectedError"] = "An unexpected sign-in error occurred. Try again in a moment.",
            ["Login.RoleVerificationFailed"] = "Could not verify the user's role.",
            ["Login.AdminOnlyError"] = "Access denied. The admin panel is available only to administrators.",
            ["Login.ConnectionError"] = "Could not connect to the server. Check your internet connection and try again.",
            ["Login.Timeout"] = "The server did not respond in time. Try again in a moment.",
            ["Login.GenericError"] = "A sign-in error occurred. Try again in a moment.",
            ["Login.InvalidCredentials"] = "Invalid email or password.",
            ["Login.BadRequest"] = "Check the entered data.",
            ["Login.TooManyRequests"] = "Too many sign-in attempts. Try again in a moment.",
            ["Login.ServerProblem"] = "The sign-in server has a temporary problem. Try again later.",
            ["Nav.Dashboard"] = "Dashboard",
            ["Nav.Users"] = "Users",
            ["Nav.Schedule"] = "Schedule",
            ["Nav.Trainers"] = "Trainers",
            ["Nav.AuditLogs"] = "Audit logs",
            ["Nav.Logout"] = "Log out",
            ["Dashboard.Users"] = "Users",
            ["Dashboard.TodayClasses"] = "Today's classes",
            ["Dashboard.AuditEntries"] = "Audit entries",
            ["Dashboard.TodaySchedule"] = "Today's schedule",
            ["Dashboard.RecentActions"] = "Recent actions",
            ["Dashboard.NoClasses"] = "No scheduled classes",
            ["Dashboard.NoClassesDescription"] = "There are no group workouts or individual sessions assigned for today.",
            ["Dashboard.NoRecentActions"] = "No recent actions",
            ["Dashboard.Loading"] = "Loading dashboard...",
            ["Dashboard.FetchingSummary"] = "Fetching summary...",
            ["Dashboard.FetchingKeyData"] = "Fetching key data...",
            ["Dashboard.OfflineSummary"] = "Offline mode: some data comes from the local copy",
            ["Dashboard.UpdatedAt"] = "Summary updated: {0:HH:mm}",
            ["Users.Title"] = "User Management",
            ["Users.SearchPlaceholder"] = "Filter by first name, last name, email, or role...",
            ["Users.NameHeader"] = "FULL NAME",
            ["Users.RoleHeader"] = "ROLE",
            ["Users.ActionsHeader"] = "ACTIONS",
            ["Users.ChangeRoleTooltip"] = "Change role",
            ["Users.DeleteTooltip"] = "Delete user",
            ["Users.Loading"] = "Loading users...",
            ["Users.Fetching"] = "Fetching data...",
            ["Users.FetchingFromServer"] = "Fetching users from the server...",
            ["Users.OfflineLoaded"] = "Offline mode: loaded {0} users from the local copy",
            ["Users.Loaded"] = "Loaded {0} users",
            ["Users.NoData"] = "No data or connection error",
            ["Users.Footer"] = "Showing: {0} of {1} users",
            ["Users.DeleteTitle"] = "Delete confirmation",
            ["Users.DeleteMessage"] = "Are you sure you want to delete this user?",
            ["Users.Deleted"] = "User {0} has been deleted.",
            ["Users.DeleteFailed"] = "Could not delete the user.",
            ["Users.RoleChangeTitle"] = "Role change",
            ["Users.RoleChangeMessage"] = "Change this user's role?",
            ["Users.RoleChanged"] = "User {0}'s role has been changed to {1}.",
            ["Users.RoleChangeFailed"] = "Could not change the user's role.",
            ["Schedule.Title"] = "Class Schedule",
            ["Schedule.Trainer"] = "Trainer",
            ["Schedule.Location"] = "Location",
            ["Schedule.Type"] = "Class type",
            ["Schedule.SearchPlaceholder"] = "Search by name, trainer, or location...",
            ["Schedule.TimeHeader"] = "TIME",
            ["Schedule.NameHeader"] = "CLASS NAME",
            ["Schedule.TrainerHeader"] = "TRAINER",
            ["Schedule.LocationHeader"] = "LOCATION",
            ["Schedule.TypeHeader"] = "TYPE",
            ["Schedule.ParticipantsHeader"] = "PARTICIPANTS",
            ["Schedule.NoClasses"] = "No classes for the selected filters",
            ["Schedule.NoClassesHint"] = "Change the date or filters to see other classes.",
            ["Schedule.Loading"] = "Loading schedule...",
            ["Schedule.AllTrainers"] = "All trainers",
            ["Schedule.AllLocations"] = "All locations",
            ["Schedule.AllTypes"] = "All types",
            ["Schedule.GroupClasses"] = "Group classes",
            ["Schedule.PersonalTraining"] = "Personal training",
            ["Schedule.AllSpots"] = "All spots",
            ["Schedule.AvailableSpots"] = "Available spots",
            ["Schedule.FullClasses"] = "Full classes",
            ["Schedule.FetchingForDate"] = "Fetching classes for {0}...",
            ["Schedule.OfflineForDate"] = "Offline mode: classes for {0} from the local copy",
            ["Schedule.ClassesForDate"] = "Classes for {0}",
            ["Schedule.NoClassesForDate"] = "No classes for {0}",
            ["Schedule.Footer"] = "Showing: {0} of {1} classes  ·  Enrolled participants: {2}",
            ["Trainers.Title"] = "Trainers",
            ["Trainers.SearchPlaceholder"] = "Search by first name, last name, or specialization...",
            ["Trainers.Loading"] = "Loading trainers...",
            ["Trainers.Fetching"] = "Fetching trainers...",
            ["Trainers.OfflineLoaded"] = "Offline mode: trainers ({0}) from the local copy",
            ["Trainers.Loaded"] = "Trainers ({0})",
            ["Trainers.Footer"] = "Showing: {0} of {1} trainers",
            ["Audit.Title"] = "Audit Logs",
            ["Audit.SearchPlaceholder"] = "Search by person, action, or details...",
            ["Audit.DateHeader"] = "DATE AND TIME",
            ["Audit.ChangedByHeader"] = "CHANGED BY",
            ["Audit.ActionHeader"] = "ACTION",
            ["Audit.DetailsHeader"] = "DETAILS",
            ["Audit.Loading"] = "Loading logs...",
            ["Audit.Fetching"] = "Fetching audit logs...",
            ["Audit.OfflineLoaded"] = "Offline mode: loaded {0} entries from the local copy",
            ["Audit.Loaded"] = "Loaded {0} entries",
            ["Audit.NoData"] = "No data",
            ["Audit.Footer"] = "Showing: {0} of {1} entries",
            ["Audit.NotificationSingle"] = "New event: {0}",
            ["Audit.NotificationMultiple"] = "New events: {0}. Latest: {1}",
            ["Audit.Action.Booking"] = "BOOKING",
            ["Audit.Action.CancelBooking"] = "CANCEL BOOKING",
            ["Audit.Action.CreateClass"] = "CREATE CLASS",
            ["Audit.Action.CreateTrainerProfile"] = "TRAINER PROFILE",
            ["Audit.Action.Create"] = "CREATE",
            ["Audit.Action.PurchaseMembership"] = "MEMBERSHIP PURCHASE",
            ["Audit.Action.Reschedule"] = "RESCHEDULE",
            ["Audit.Action.Update"] = "UPDATE",
            ["Audit.Action.Delete"] = "DELETE",
            ["Audit.Detail.BookingFor"] = "Booking for: {0}",
            ["Audit.Detail.Canceled"] = "Canceled: {0}",
            ["Audit.Detail.ClassAt"] = "Class: {0} at: {1}",
            ["Audit.Detail.IdTo"] = "ID: {0} to {1}",
            ["Audit.Detail.PurchasedMembership"] = "Purchased {0} membership for {1}"
        }
    };

    public static LocalizationService Instance { get; } = new();

    [ObservableProperty]
    private string _currentLanguage = LoadSavedLanguage();

    public event EventHandler? LanguageChanged;

    private LocalizationService()
    {
        CultureInfo.CurrentUICulture = CreateCulture(CurrentLanguage);
        CultureInfo.CurrentCulture = CreateCulture(CurrentLanguage);
    }

    public string this[string key] => Translate(key);

    public string Translate(string key)
    {
        return TranslateForLanguage(key, CurrentLanguage);
    }

    public string TranslateForLanguage(string key, string languageCode)
    {
        var normalized = string.Equals(languageCode, English, StringComparison.OrdinalIgnoreCase)
            ? English
            : Polish;

        if (_translations.TryGetValue(normalized, out var language) &&
            language.TryGetValue(key, out var value))
            return value;

        return _translations[Polish].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public string Format(string key, params object[] args)
        => string.Format(CultureInfo.CurrentCulture, Translate(key), args);

    public string TranslateAuditAction(string action)
        => TranslateAuditActionForLanguage(action, CurrentLanguage);

    public string TranslateAuditActionForLanguage(string action, string languageCode)
    {
        var normalized = action.Trim().ToUpperInvariant();

        return normalized switch
        {
            "BOOKING" => TranslateForLanguage("Audit.Action.Booking", languageCode),
            "CANCEL_BOOKING" => TranslateForLanguage("Audit.Action.CancelBooking", languageCode),
            "CREATE_CLASS" => TranslateForLanguage("Audit.Action.CreateClass", languageCode),
            "CREATE_TRAINER_PROFILE" => TranslateForLanguage("Audit.Action.CreateTrainerProfile", languageCode),
            "PURCHASE_MEMBERSHIP" => TranslateForLanguage("Audit.Action.PurchaseMembership", languageCode),
            "RESCHEDULE" => TranslateForLanguage("Audit.Action.Reschedule", languageCode),
            "CREATE" => TranslateForLanguage("Audit.Action.Create", languageCode),
            var value when value.Contains("UPDATE") || value.Contains("CHANGE") => TranslateForLanguage("Audit.Action.Update", languageCode),
            var value when value.Contains("DELETE") || value.Contains("REMOVE") => TranslateForLanguage("Audit.Action.Delete", languageCode),
            _ => action.Replace('_', ' ')
        };
    }

    public string TranslateAuditDetails(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return string.Empty;

        const string canceledPrefix = "Anulowano: ";
        const string bookingPrefix = "Zapis na: ";
        const string classPrefix = "Zajęcia: ";
        const string membershipPrefix = "Zakupiono karnet ";

        if (details.StartsWith(canceledPrefix, StringComparison.OrdinalIgnoreCase))
            return Format("Audit.Detail.Canceled", details[canceledPrefix.Length..]);

        if (details.StartsWith(bookingPrefix, StringComparison.OrdinalIgnoreCase))
            return Format("Audit.Detail.BookingFor", details[bookingPrefix.Length..]);

        if (details.StartsWith(classPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var classDetails = details[classPrefix.Length..];
            var separatorIndex = classDetails.IndexOf(" w: ", StringComparison.OrdinalIgnoreCase);

            if (separatorIndex >= 0)
            {
                var className = classDetails[..separatorIndex];
                var location = classDetails[(separatorIndex + 4)..];
                return Format("Audit.Detail.ClassAt", className, location);
            }
        }

        if (details.StartsWith(membershipPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var membershipDetails = details[membershipPrefix.Length..];
            var separatorIndex = membershipDetails.IndexOf(" za ", StringComparison.OrdinalIgnoreCase);

            if (separatorIndex >= 0)
            {
                var membershipName = membershipDetails[..separatorIndex];
                var price = membershipDetails[(separatorIndex + 4)..];
                return Format("Audit.Detail.PurchasedMembership", membershipName, price);
            }
        }

        if (details.StartsWith("ID: ", StringComparison.OrdinalIgnoreCase))
        {
            var separatorIndex = details.IndexOf(" na ", StringComparison.OrdinalIgnoreCase);
            if (separatorIndex >= 0)
            {
                var id = details[4..separatorIndex];
                var target = details[(separatorIndex + 4)..];
                return Format("Audit.Detail.IdTo", id, target);
            }
        }

        return details;
    }

    public void SetLanguage(string language)
    {
        var normalized = string.Equals(language, English, StringComparison.OrdinalIgnoreCase)
            ? English
            : Polish;

        if (CurrentLanguage == normalized)
            return;

        CurrentLanguage = normalized;
    }

    partial void OnCurrentLanguageChanged(string value)
    {
        CultureInfo.CurrentUICulture = CreateCulture(value);
        CultureInfo.CurrentCulture = CreateCulture(value);
        SaveLanguage(value);
        OnPropertyChanged("Item[]");
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string LoadSavedLanguage()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var saved = File.ReadAllText(SettingsPath).Trim().ToLowerInvariant();
                if (saved is English or Polish)
                    return saved;
            }
        }
        catch
        {
        }

        return Polish;
    }

    private static void SaveLanguage(string language)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, language);
        }
        catch
        {
        }
    }

    private static CultureInfo CreateCulture(string language)
        => new(language == English ? "en-US" : "pl-PL");
}
