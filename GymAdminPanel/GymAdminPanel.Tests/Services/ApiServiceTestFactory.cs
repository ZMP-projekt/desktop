using System.IO;
using System.Net.Http;
using GymAdminPanel.Services;

namespace GymAdminPanel.Tests.Services;

internal static class ApiServiceTestFactory
{
    public static ApiService Create(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return Create(httpClient);
    }

    public static ApiService Create(HttpClient httpClient)
    {
        var cachePath = Path.Combine(
            Path.GetTempPath(),
            $"gym-admin-panel-test-cache-{Guid.NewGuid():N}.db");

        return new ApiService(httpClient, new OfflineCacheService(cachePath));
    }
}
