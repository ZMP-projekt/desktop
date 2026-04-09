using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace GymAdminPanel.Models;

public partial class User : ObservableObject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    [ObservableProperty]
    private string _role = string.Empty;
}
