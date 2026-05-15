using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class UsersViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsUsersSortedById()
    {
        var service = await CreateLoggedInServiceAsync(
            """
            [
              {"id":3,"email":"third@test.pl","role":"ROLE_USER","firstName":"Cezary","lastName":"Trzeci"},
              {"id":1,"email":"first@test.pl","role":"ROLE_ADMIN","firstName":"Adam","lastName":"Pierwszy"},
              {"id":2,"email":"second@test.pl","role":"ROLE_TRAINER","firstName":"Beata","lastName":"Druga"}
            ]
            """);

        var viewModel = new UsersViewModel(service);

        await WaitForUsersAsync(viewModel, expectedCount: 3);

        Assert.Equal([1, 2, 3], viewModel.FilteredUsers.Select(u => u.Id));
        Assert.Equal("Wyświetlono: 3 z 3 użytkowników", viewModel.FooterText);
    }

    [Theory]
    [InlineData("adam", 1)]
    [InlineData("druga", 2)]
    [InlineData("third@test", 3)]
    [InlineData("ROLE_TRAINER", 2)]
    public async Task SearchText_FiltersByNameEmailAndRole(string searchText, int expectedUserId)
    {
        var service = await CreateLoggedInServiceAsync(
            """
            [
              {"id":3,"email":"third@test.pl","role":"ROLE_USER","firstName":"Cezary","lastName":"Trzeci"},
              {"id":1,"email":"first@test.pl","role":"ROLE_ADMIN","firstName":"Adam","lastName":"Pierwszy"},
              {"id":2,"email":"second@test.pl","role":"ROLE_TRAINER","firstName":"Beata","lastName":"Druga"}
            ]
            """);

        var viewModel = new UsersViewModel(service);
        await WaitForUsersAsync(viewModel, expectedCount: 3);

        viewModel.SearchText = searchText;

        var user = Assert.Single(viewModel.FilteredUsers);
        Assert.Equal(expectedUserId, user.Id);
        Assert.Equal("Wyświetlono: 1 z 3 użytkowników", viewModel.FooterText);
    }

    [Fact]
    public async Task SearchText_WhenSelectionExists_PreservesSelectedUser()
    {
        var service = await CreateLoggedInServiceAsync(
            """
            [
              {"id":2,"email":"trainer@test.pl","role":"ROLE_TRAINER","firstName":"Beata","lastName":"Druga"},
              {"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN","firstName":"Adam","lastName":"Pierwszy"}
            ]
            """);

        var viewModel = new UsersViewModel(service);
        await WaitForUsersAsync(viewModel, expectedCount: 2);
        viewModel.SelectedUser = viewModel.FilteredUsers.Single(u => u.Id == 2);

        viewModel.SearchText = "ROLE_TRAINER";

        Assert.NotNull(viewModel.SelectedUser);
        Assert.Equal(2, viewModel.SelectedUser.Id);
    }

    private static async Task<ApiService> CreateLoggedInServiceAsync(string usersJson)
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, usersJson));

        var service = new ApiService(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        });

        var loggedIn = await service.LoginAsync("admin@test.pl", "secret");
        Assert.True(loggedIn);

        return service;
    }

    private static async Task WaitForUsersAsync(UsersViewModel viewModel, int expectedCount)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(3);
        while (viewModel.FilteredUsers.Count != expectedCount && DateTime.UtcNow < timeoutAt)
            await Task.Delay(20);

        Assert.Equal(expectedCount, viewModel.FilteredUsers.Count);
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
