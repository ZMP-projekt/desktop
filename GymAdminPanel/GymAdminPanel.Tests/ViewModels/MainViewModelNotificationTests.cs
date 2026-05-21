using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using GymAdminPanel.Tests.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class MainViewModelNotificationTests
{
    [Fact]
    public async Task PollAuditLogsOnceAsync_WhenFirstPollLoadsExistingLogs_DoesNotShowNotification()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"changedBy":"admin@test.pl","action":"BASELINE","details":"Existing log","timestamp":"2026-05-21T10:00:00Z"}
            ]
            """));
        var service = ApiServiceTestFactory.Create(handler);
        await LoginAsync(service);
        AppStatus? status = null;
        service.StatusChanged += value => status = value;
        using var viewModel = CreateNotificationOnlyViewModel(service);

        await viewModel.PollAuditLogsOnceAsync();

        Assert.Null(status);
        Assert.False(viewModel.IsStatusVisible);
    }

    [Fact]
    public async Task PollAuditLogsOnceAsync_WhenNewLogAppears_ShowsInfoNotification()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"changedBy":"admin@test.pl","action":"BASELINE","details":"Existing log","timestamp":"2026-05-21T10:00:00Z"}
            ]
            """),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"changedBy":"admin@test.pl","action":"BASELINE","details":"Existing log","timestamp":"2026-05-21T10:00:00Z"},
              {"changedBy":"system@test.pl","action":"ALERT","details":"New log","timestamp":"2026-05-21T10:01:00Z"}
            ]
            """));
        var service = ApiServiceTestFactory.Create(handler);
        await LoginAsync(service);
        AppStatus? status = null;
        service.StatusChanged += value => status = value;
        using var viewModel = CreateNotificationOnlyViewModel(service);

        await viewModel.PollAuditLogsOnceAsync();
        await viewModel.PollAuditLogsOnceAsync();

        Assert.NotNull(status);
        Assert.Equal(AppStatusKind.Info, status.Kind);
        Assert.Contains("ALERT", status.Message);
        Assert.True(viewModel.IsStatusVisible);
        Assert.Contains("ALERT", viewModel.StatusMessage);
    }

    [Fact]
    public async Task PollAuditLogsOnceAsync_WhenLogsComeFromCache_DoesNotShowNotification()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"changedBy":"system@test.pl","action":"CACHED_ALERT","details":"Cached only","timestamp":"2026-05-21T10:01:00Z"}
            ]
            """),
            JsonResponse(HttpStatusCode.InternalServerError, """{"message":"Failure"}"""));
        var service = ApiServiceTestFactory.Create(handler);
        await LoginAsync(service);
        var cachedLogs = await service.GetAuditLogsAsync();
        Assert.Single(cachedLogs);

        AppStatus? status = null;
        service.StatusChanged += value => status = value;
        using var viewModel = CreateNotificationOnlyViewModel(service);

        await viewModel.PollAuditLogsOnceAsync();

        Assert.True(service.LastResultFromCache);
        Assert.Null(status);
        Assert.False(viewModel.IsStatusVisible);
    }

    private static MainViewModel CreateNotificationOnlyViewModel(ApiService service)
        => new(service, startAuditLogPolling: false, initializeViewModels: false);

    private static async Task LoginAsync(ApiService service)
    {
        var loggedIn = await service.LoginAsync("admin@test.pl", "secret");
        Assert.True(loggedIn);
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
                throw new InvalidOperationException("No response configured for the request.");

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
