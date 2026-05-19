using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;
using GymAdminPanel.Tests.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class TrainersViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsRoleVerifiedTrainersAndSortsByName()
    {
        var service = await CreateLoggedInServiceAsync(
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":2,"firstName":"Beata","lastName":"Coach","specialization":"Pilates","bio":"Bio","photoUrl":"photo"},
              {"id":1,"firstName":"Adam","lastName":"Trainer","specialization":"Strength","bio":"Bio","photoUrl":"photo"}
            ]
            """),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":2,"email":"beata@test.pl","role":"ROLE_TRAINER","firstName":"Beata","lastName":"Coach"},
              {"id":1,"email":"adam@test.pl","role":"ROLE_TRAINER","firstName":"Adam","lastName":"Trainer"},
              {"id":3,"email":"user@test.pl","role":"ROLE_USER","firstName":"User","lastName":"Regular"}
            ]
            """));

        var viewModel = new TrainersViewModel(service);

        await WaitForAsync(() => !viewModel.IsLoading && viewModel.FilteredTrainers.Count == 2);

        Assert.Equal(["Adam Trainer", "Beata Coach"], viewModel.FilteredTrainers.Select(t => t.FullName));
        Assert.Equal("Trenerzy (2)", viewModel.StatusText);
        Assert.Equal("Wyświetlono: 2 z 2 trenerów", viewModel.FooterText);
    }

    [Fact]
    public async Task SearchText_FiltersByNameEmailAndSpecialization()
    {
        var service = await CreateLoggedInServiceAsync(
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":1,"firstName":"Adam","lastName":"Trainer","specialization":"Strength","bio":"Bio","photoUrl":"photo"},
              {"id":2,"firstName":"Beata","lastName":"Coach","specialization":"Pilates","bio":"Bio","photoUrl":"photo"}
            ]
            """),
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":1,"email":"adam@test.pl","role":"ROLE_TRAINER","firstName":"Adam","lastName":"Trainer"},
              {"id":2,"email":"beata@test.pl","role":"ROLE_TRAINER","firstName":"Beata","lastName":"Coach"}
            ]
            """));
        var viewModel = new TrainersViewModel(service);
        await WaitForAsync(() => !viewModel.IsLoading && viewModel.FilteredTrainers.Count == 2);

        viewModel.SearchText = "pilates";

        var trainer = Assert.Single(viewModel.FilteredTrainers);
        Assert.Equal("Beata", trainer.FirstName);
        Assert.Equal("Wyświetlono: 1 z 2 trenerów", viewModel.FooterText);
    }

    private static async Task<ApiService> CreateLoggedInServiceAsync(params HttpResponseMessage[] responses)
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(responses).ToArray());
        var service = ApiServiceTestFactory.Create(handler);

        var loggedIn = await service.LoginAsync("admin@test.pl", "secret");
        Assert.True(loggedIn);

        return service;
    }

    private static HttpResponseMessage[] LoginResponses() =>
    [
        JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
        JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}""")
    ];

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
