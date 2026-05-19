using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;

namespace GymAdminPanel.Tests.Services;

public class ApiServiceLoginTests
{
    [Fact]
    public async Task LoginAsync_WhenUserIsAdmin_ReturnsTrueAndKeepsToken()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""));
        var service = CreateService(handler);

        var result = await service.LoginAsync("admin@test.pl", "secret");

        Assert.True(result);
        Assert.Equal("admin-token", service.Token);
        Assert.Empty(service.LastLoginError);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/auth/login", handler.Requests[0].RequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("/api/users/me", handler.Requests[1].RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", handler.Requests[1].Headers.Authorization?.Scheme);
        Assert.Equal("admin-token", handler.Requests[1].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsNotAdmin_ReturnsFalseAndClearsToken()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"user-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":2,"email":"user@test.pl","role":"ROLE_USER"}"""));
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.pl", "secret");

        Assert.False(result);
        Assert.Empty(service.Token);
        Assert.Equal(
            "Brak dostępu. Panel administracyjny jest dostępny tylko dla administratorów.",
            service.LastLoginError);
    }

    [Fact]
    public async Task LoginAsync_WhenCurrentUserCannotBeVerified_ReturnsFalseAndClearsToken()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.Forbidden, """{"message":"Forbidden"}"""));
        var service = CreateService(handler);

        var result = await service.LoginAsync("admin@test.pl", "secret");

        Assert.False(result);
        Assert.Empty(service.Token);
        Assert.Equal("Nie udało się zweryfikować roli użytkownika.", service.LastLoginError);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreInvalid_ReturnsFalseAndDoesNotCallCurrentUser()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.Unauthorized, """{"message":"Unauthorized"}"""));
        var service = CreateService(handler);

        var result = await service.LoginAsync("admin@test.pl", "wrong");

        Assert.False(result);
        Assert.Empty(service.Token);
        Assert.Equal("Nieprawidłowy e-mail lub hasło.", service.LastLoginError);
        Assert.Single(handler.Requests);
        Assert.Equal("/auth/login", handler.Requests[0].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreRejectedWithForbidden_ReturnsInvalidCredentialsAndDoesNotCallCurrentUser()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.Forbidden, """{"message":"Forbidden"}"""));
        var service = CreateService(handler);

        var result = await service.LoginAsync("admin@test.pl", "wrong");

        Assert.False(result);
        Assert.Empty(service.Token);
        Assert.Equal("Nieprawidłowy e-mail lub hasło.", service.LastLoginError);
        Assert.Single(handler.Requests);
        Assert.Equal("/auth/login", handler.Requests[0].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task LoginAsync_WhenServerFails_ReturnsFriendlyMessage()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.InternalServerError, """{"message":"Failure"}"""));
        var service = CreateService(handler);

        var result = await service.LoginAsync("admin@test.pl", "secret");

        Assert.False(result);
        Assert.Empty(service.Token);
        Assert.Equal(
            "Serwer logowania ma chwilowy problem. Spróbuj ponownie później.",
            service.LastLoginError);
    }

    private static ApiService CreateService(HttpMessageHandler handler)
        => ApiServiceTestFactory.Create(handler);

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
