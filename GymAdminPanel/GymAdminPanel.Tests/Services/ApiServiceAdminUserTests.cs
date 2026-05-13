using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;

namespace GymAdminPanel.Tests.Services;

public class ApiServiceAdminUserTests
{
    [Fact]
    public async Task DeleteUserAsync_WhenTokenExists_SendsDeleteRequestWithBearerToken()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.NoContent, ""));
        var service = CreateService(handler);
        await service.LoginAsync("admin@test.pl", "secret");

        var result = await service.DeleteUserAsync(123);

        Assert.True(result);
        Assert.Equal(3, handler.Requests.Count);
        var deleteRequest = handler.Requests[2];
        Assert.Equal(HttpMethod.Delete, deleteRequest.Method);
        Assert.Equal("/api/admin/users/123", deleteRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", deleteRequest.Headers.Authorization?.Scheme);
        Assert.Equal("admin-token", deleteRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenTokenIsMissing_ReturnsFalseWithoutCallingApi()
    {
        var handler = new QueueHttpMessageHandler();
        var service = CreateService(handler);

        var result = await service.DeleteUserAsync(123);

        Assert.False(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WhenTokenExists_SendsPatchRequestWithNewRole()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, ""));
        var service = CreateService(handler);
        await service.LoginAsync("admin@test.pl", "secret");

        var result = await service.ChangeUserRoleAsync(456, "ROLE_ADMIN");

        Assert.True(result);
        Assert.Equal(3, handler.Requests.Count);
        var patchRequest = handler.Requests[2];
        Assert.Equal(HttpMethod.Patch, patchRequest.Method);
        Assert.Equal("/api/admin/users/456/role", patchRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", patchRequest.Headers.Authorization?.Scheme);
        Assert.Equal("admin-token", patchRequest.Headers.Authorization?.Parameter);

        var body = await patchRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"role\":\"ROLE_ADMIN\"", body);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WhenTokenIsMissing_ReturnsFalseWithoutCallingApi()
    {
        var handler = new QueueHttpMessageHandler();
        var service = CreateService(handler);

        var result = await service.ChangeUserRoleAsync(456, "ROLE_ADMIN");

        Assert.False(result);
        Assert.Empty(handler.Requests);
    }

    private static ApiService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return new ApiService(httpClient);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for the request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
