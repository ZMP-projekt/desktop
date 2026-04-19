using System;
using System.Text.Json.Serialization;

namespace GymAdminPanel.Models;

public class GymClass
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("trainerName")]
    public string TrainerName { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("currentParticipants")]
    public int CurrentParticipants { get; set; }

    [JsonPropertyName("maxParticipants")]
    public int MaxParticipants { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("userEnrolled")]
    public bool UserEnrolled { get; set; }

    [JsonPropertyName("personalTraining")]
    public bool PersonalTraining { get; set; }

    // Pomocnicze
    public string TimeRange => $"{StartTime:HH:mm} – {EndTime:HH:mm}";
    public string ParticipantsDisplay => $"{CurrentParticipants}/{MaxParticipants}";
    public bool IsFull => CurrentParticipants >= MaxParticipants;
    public string TypeLabel => PersonalTraining ? "Trening osobisty" : "Zajęcia grupowe";
}
