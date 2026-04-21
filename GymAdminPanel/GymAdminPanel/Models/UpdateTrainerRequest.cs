using System.Text.Json.Serialization;

namespace GymAdminPanel.Models;

public class UpdateTrainerRequest
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("specialization")]
    public string Specialization { get; set; } = string.Empty;

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = string.Empty;

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;
}
