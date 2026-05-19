using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using GymAdminPanel.Data;
using GymAdminPanel.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAdminPanel.Services;

public class OfflineCacheService
{
    private readonly Func<AppDbContext> _createDbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed record CacheLoadResult<T>(List<T> Items, DateTime? CachedAt);

    public OfflineCacheService()
        : this(() => new AppDbContext())
    {
    }

    public OfflineCacheService(string databasePath)
        : this(() => new AppDbContext(databasePath))
    {
    }

    public OfflineCacheService(Func<AppDbContext> createDbContext)
    {
        _createDbContext = createDbContext;
    }

    public async Task SaveAsync<T>(string cacheKey, List<T> items)
    {
        await using var db = _createDbContext();
        await EnsureCacheTableAsync(db);

        var json = JsonSerializer.Serialize(items, JsonOptions);
        var entry = await db.CacheEntries.FindAsync(cacheKey);

        if (entry == null)
        {
            db.CacheEntries.Add(new CacheEntry
            {
                CacheKey = cacheKey,
                PayloadJson = json,
                CachedAt = DateTime.UtcNow
            });
        }
        else
        {
            entry.PayloadJson = json;
            entry.CachedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<T>> LoadAsync<T>(string cacheKey)
    {
        var result = await LoadWithMetadataAsync<T>(cacheKey);
        return result.Items;
    }

    public async Task<List<T>> LoadListsByKeyPrefixAsync<T>(string cacheKeyPrefix)
    {
        await using var db = _createDbContext();
        await EnsureCacheTableAsync(db);

        var payloads = await db.CacheEntries
            .Where(entry => entry.CacheKey.StartsWith(cacheKeyPrefix))
            .Select(entry => entry.PayloadJson)
            .ToListAsync();

        var items = new List<T>();
        foreach (var payload in payloads)
        {
            if (string.IsNullOrWhiteSpace(payload))
                continue;

            try
            {
                var cachedItems = JsonSerializer.Deserialize<List<T>>(payload, JsonOptions);
                if (cachedItems != null)
                    items.AddRange(cachedItems);
            }
            catch
            {
                // Ignore malformed cache entries and keep any usable cached lists.
            }
        }

        return items;
    }

    public async Task<CacheLoadResult<T>> LoadWithMetadataAsync<T>(string cacheKey)
    {
        await using var db = _createDbContext();
        await EnsureCacheTableAsync(db);

        var entry = await db.CacheEntries.FindAsync(cacheKey);
        if (entry == null || string.IsNullOrWhiteSpace(entry.PayloadJson))
            return new CacheLoadResult<T>(new List<T>(), null);

        try
        {
            var items = JsonSerializer.Deserialize<List<T>>(entry.PayloadJson, JsonOptions) ?? new List<T>();
            return new CacheLoadResult<T>(items, entry.CachedAt);
        }
        catch
        {
            return new CacheLoadResult<T>(new List<T>(), null);
        }
    }

    private static async Task EnsureCacheTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "CacheEntries" (
                "CacheKey" TEXT NOT NULL CONSTRAINT "PK_CacheEntries" PRIMARY KEY,
                "PayloadJson" TEXT NOT NULL,
                "CachedAt" TEXT NOT NULL
            );
            """);
    }
}
