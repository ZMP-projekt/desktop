using System;
using System.Text.Json.Serialization;
using GymAdminPanel.Services;

namespace GymAdminPanel.Models;

public class AuditLog
{
    [JsonPropertyName("changedBy")]
    public string ChangedBy { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    public string TimestampDisplay => Timestamp.ToLocalTime().ToString(
        "dd.MM.yyyy  HH:mm:ss",
        System.Globalization.CultureInfo.CurrentCulture);
    public string ActionDisplay => LocalizationService.Instance.TranslateAuditAction(Action);
    public string DetailsDisplay => LocalizationService.Instance.TranslateAuditDetails(Details);
}
