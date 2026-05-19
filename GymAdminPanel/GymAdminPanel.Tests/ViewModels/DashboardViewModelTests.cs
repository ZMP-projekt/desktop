using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class DashboardViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsCountsAndPreviewLists()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":1,"email":"one@test.pl","role":"ROLE_USER"},
              {"id":2,"email":"two@test.pl","role":"ROLE_TRAINER"},
              {"id":3,"email":"three@test.pl","role":"ROLE_ADMIN"}
            ]
            """),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":10,"name":"Morning","trainerName":"Anna","startTime":"2026-05-17T09:00:00Z","endTime":"2026-05-17T10:00:00Z","currentParticipants":3,"maxParticipants":10,"locationName":"Sala A","personalTraining":false},
              {"id":11,"name":"Evening","trainerName":"Adam","startTime":"2026-05-17T18:00:00Z","endTime":"2026-05-17T19:00:00Z","currentParticipants":7,"maxParticipants":12,"locationName":"Sala B","personalTraining":false}
            ]
            """),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"changedBy":"admin@test.pl","action":"OLDER","details":"Older","timestamp":"2026-05-17T08:00:00Z"},
              {"changedBy":"admin@test.pl","action":"NEWER","details":"Newer","timestamp":"2026-05-17T12:00:00Z"}
            ]
            """));
        var service = CreateService(handler);
        await LoginAsync(service);

        var viewModel = new DashboardViewModel(service);

        await WaitForAsync(() => !viewModel.IsLoading && viewModel.UsersCount == 3);

        Assert.Equal(3, viewModel.UsersCount);
        Assert.Equal(2, viewModel.TodayClassesCount);
        Assert.Equal(2, viewModel.AuditLogsCount);
        Assert.True(viewModel.HasTodayClasses);
        Assert.True(viewModel.HasRecentAuditLogs);
        Assert.Equal(["Morning", "Evening"], viewModel.TodayClasses.Select(c => c.Name));
        Assert.Equal(["NEWER", "OLDER"], viewModel.RecentAuditLogs.Select(l => l.Action));
    }

    [Fact]
    public async Task LoadDashboardCommand_WhenNoData_SetsEmptyStates()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, "[]"),
            JsonResponse(HttpStatusCode.OK, "[]"),
            JsonResponse(HttpStatusCode.OK, "[]"));
        var service = CreateService(handler);
        await LoginAsync(service);

        var viewModel = new DashboardViewModel(service);

        await WaitForAsync(() => !viewModel.IsLoading);

        Assert.Equal(0, viewModel.UsersCount);
        Assert.Equal(0, viewModel.TodayClassesCount);
        Assert.Equal(0, viewModel.AuditLogsCount);
        Assert.False(viewModel.HasTodayClasses);
        Assert.False(viewModel.HasRecentAuditLogs);
    }

    private static async Task LoginAsync(ApiService service)
    {
        var loggedIn = await service.LoginAsync("admin@test.pl", "secret");
        Assert.True(loggedIn);
    }

    private static ApiService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return new ApiService(httpClient);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(3);
        while (!condition() && DateTime.UtcNow < timeoutAt)
            await Task.Delay(20);

        Assert.True(condition());
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
