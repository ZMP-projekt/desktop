using System.Text.Json.Serialization;

namespace GymAdminPanel.Models;

public class Trainer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

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

    public string FullName => $"{FirstName} {LastName}".Trim();
    public string NameOrEmail => string.IsNullOrWhiteSpace(FullName) ? Email : FullName;
    public string Initials
    {
        get
        {
            var firstInitial = GetInitial(FirstName);
            var lastInitial = GetInitial(LastName);
            var initials = $"{firstInitial}{lastInitial}";
            return string.IsNullOrWhiteSpace(initials) ? GetInitial(Email) : initials;
        }
    }

    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(Specialization)
                ? NameOrEmail
                : $"{NameOrEmail}  ({Specialization})";
        }
    }

    public override string ToString() => DisplayName;

    private static string GetInitial(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[0].ToString().ToUpperInvariant();
}
