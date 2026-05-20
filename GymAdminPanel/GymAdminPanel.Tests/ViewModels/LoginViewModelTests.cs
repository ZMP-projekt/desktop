using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;
using GymAdminPanel.Tests.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_WhenEmailIsInvalid_ShowsValidationErrorWithoutCallingApi()
    {
        var handler = new QueueHttpMessageHandler();
        var viewModel = new LoginViewModel(ApiServiceTestFactory.Create(handler))
        {
            Email = "ddddd",
            PasswordLength = "secret".Length
        };

        await viewModel.LoginCommand.ExecuteAsync("secret");

        Assert.Equal("Podaj poprawny adres e-mail.", viewModel.ErrorMessage);
        Assert.Empty(handler.Requests);
        Assert.True(viewModel.IsLoginEnabled);
    }

    [Fact]
    public async Task LoginCommand_WhenApiRejectsCredentials_ShowsApiLoginError()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.Forbidden, """{"message":"Forbidden"}"""));
        var viewModel = new LoginViewModel(ApiServiceTestFactory.Create(handler))
        {
            Email = "admin@test.pl",
            PasswordLength = "wrong".Length
        };

        await viewModel.LoginCommand.ExecuteAsync("wrong");

        Assert.Equal("Nieprawidłowy e-mail lub hasło.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoggingIn);
        Assert.True(viewModel.IsLoginEnabled);
        Assert.Equal("wrong".Length, viewModel.PasswordLength);
        Assert.Single(handler.Requests);
        Assert.Equal("/auth/login", handler.Requests[0].RequestUri?.AbsolutePath);
    }

    [Fact]
    public void LoginInputs_UpdateLoginEnabledAndClearErrors()
    {
        var viewModel = new LoginViewModel(ApiServiceTestFactory.Create(new QueueHttpMessageHandler()))
        {
            ErrorMessage = "Błąd"
        };

        viewModel.Email = "admin@test.pl";
        viewModel.PasswordLength = "secret".Length;

        Assert.True(viewModel.IsLoginEnabled);
        Assert.Empty(viewModel.ErrorMessage);
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
                throw new InvalidOperationException("No response configured for the request.");

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
