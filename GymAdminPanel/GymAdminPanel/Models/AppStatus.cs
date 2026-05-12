namespace GymAdminPanel.Models;

public enum AppStatusKind
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class AppStatus
{
    public AppStatusKind Kind { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool CanRetry { get; init; }
}
