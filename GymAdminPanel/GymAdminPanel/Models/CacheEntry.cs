using System;
using System.ComponentModel.DataAnnotations;

namespace GymAdminPanel.Models;

public class CacheEntry
{
    [Key]
    public string CacheKey { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
