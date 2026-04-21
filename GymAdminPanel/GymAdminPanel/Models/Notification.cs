using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GymAdminPanel.Models;

public partial class Notification : ObservableObject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("read")]
    [ObservableProperty]
    private bool _read;

    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy  HH:mm");
    public string StatusLabel => Read ? "Przeczytane" : "Nowe";
}
