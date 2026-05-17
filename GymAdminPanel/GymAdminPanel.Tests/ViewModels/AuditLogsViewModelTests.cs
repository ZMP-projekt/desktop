using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class AuditLogsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsLogsSortedDescendingAndBuildsActionFilters()
    {
        var service = await CreateLoggedInServiceAsync(
            """
            [
              {"changedBy":"admin@test.pl","action":"UPDATE","details":"Updated trainer","timestamp":"2026-05-17T08:00:00Z"},
              {"changedBy":"system@test.pl","action":"CREATE","details":"Created class","timestamp":"2026-05-17T10:00:00Z"},
              {"changedBy":"admin@test.pl","action":"DELETE","details":"Deleted user","timestamp":"2026-05-17T09:00:00Z"}
            ]
            """);

        var viewModel = new AuditLogsViewModel(service);

        await WaitForAsync(() => !viewModel.IsLoading);
        Assert.Equal(3, viewModel.FilteredLogs.Count);

        Assert.Equal(["CREATE", "DELETE", "UPDATE"], viewModel.FilteredLogs.Select(l => l.Action));
        Assert.Equal(["Wszystkie", "CREATE", "DELETE", "UPDATE"], viewModel.ActionFilters);
        Assert.Equal("Wyświetlono: 3 z 3 wpisów", viewModel.FooterText);
    }

    [Fact]
    public async Task Filters_ByActionAndSearchText()
    {
        var service = await CreateLoggedInServiceAsync(
            """
            [
              {"changedBy":"admin@test.pl","action":"UPDATE","details":"Updated trainer profile","timestamp":"2026-05-17T08:00:00Z"},
              {"changedBy":"system@test.pl","action":"CREATE","details":"Created yoga class","timestamp":"2026-05-17T10:00:00Z"},
              {"changedBy":"owner@test.pl","action":"DELETE","details":"Deleted inactive user","timestamp":"2026-05-17T09:00:00Z"}
            ]
            """);

        var viewModel = new AuditLogsViewModel(service);
        await WaitForAsync(() => !viewModel.IsLoading);
        Assert.Equal(3, viewModel.FilteredLogs.Count);

        viewModel.SelectedActionFilter = "CREATE";
        var createLog = Assert.Single(viewModel.FilteredLogs);
        Assert.Equal("Created yoga class", createLog.Details);

        viewModel.SelectedActionFilter = "Wszystkie";
        viewModel.SearchText = "owner";
        var ownerLog = Assert.Single(viewModel.FilteredLogs);
        Assert.Equal("DELETE", ownerLog.Action);
        Assert.Equal("Wyświetlono: 1 z 3 wpisów", viewModel.FooterText);
    }

    private static async Task<ApiService> CreateLoggedInServiceAsync(string logsJson)
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, logsJson));

        var service = new ApiService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        });

        var loggedIn = await service.LoginAsync("admin@test.pl", "secret");
        Assert.True(loggedIn);

        return service;
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
