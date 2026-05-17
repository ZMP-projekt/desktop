using System.Text.Json.Serialization;

namespace GymAdminPanel.Models;

public class Location
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    public string DisplayName => $"{Name} — {City}, {Address}";

    public override string ToString() => DisplayName;
}
