using System;
using System.Text.Json.Serialization;

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

    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("dd.MM.yyyy  HH:mm:ss");
}
