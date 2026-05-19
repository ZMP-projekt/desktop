using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Services;
using GymAdminPanel.Tests.Services;
using GymAdminPanel.ViewModels;

namespace GymAdminPanel.Tests.ViewModels;

public class ScheduleViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsClassesSortedAndBuildsFilterOptions()
    {
        var service = await CreateLoggedInServiceAsync(
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":2,"name":"Evening Pilates","trainerName":"Beata","startTime":"2026-05-17T18:00:00Z","endTime":"2026-05-17T19:00:00Z","currentParticipants":12,"maxParticipants":12,"locationName":"Sala B","personalTraining":true},
              {"id":1,"name":"Morning Yoga","trainerName":"Anna","startTime":"2026-05-17T09:00:00Z","endTime":"2026-05-17T10:00:00Z","currentParticipants":4,"maxParticipants":12,"locationName":"Sala A","personalTraining":false}
            ]
            """));

        var viewModel = new ScheduleViewModel(service);

        await WaitForAsync(() => !viewModel.IsLoading && viewModel.FilteredClasses.Count == 2);

        Assert.Equal(["Morning Yoga", "Evening Pilates"], viewModel.FilteredClasses.Select(c => c.Name));
        Assert.Equal(["Wszyscy trenerzy", "Anna", "Beata"], viewModel.TrainerFilters);
        Assert.Equal(["Wszystkie lokalizacje", "Sala A", "Sala B"], viewModel.LocationFilters);
        Assert.Equal("Wyświetlono: 2 z 2 zajęć  ·  Zapisanych uczestników: 16", viewModel.FooterText);
    }

    [Fact]
    public async Task Filters_BySearchTrainerLocationTypeAndAvailability()
    {
        var service = await CreateLoggedInServiceAsync(
            JsonResponse(HttpStatusCode.OK, """
            [
              {"id":1,"name":"Morning Yoga","trainerName":"Anna","startTime":"2026-05-17T09:00:00Z","endTime":"2026-05-17T10:00:00Z","currentParticipants":4,"maxParticipants":12,"locationName":"Sala A","personalTraining":false},
              {"id":2,"name":"Evening Pilates","trainerName":"Beata","startTime":"2026-05-17T18:00:00Z","endTime":"2026-05-17T19:00:00Z","currentParticipants":12,"maxParticipants":12,"locationName":"Sala B","personalTraining":true},
              {"id":3,"name":"Mobility","trainerName":"Anna","startTime":"2026-05-17T12:00:00Z","endTime":"2026-05-17T13:00:00Z","currentParticipants":8,"maxParticipants":8,"locationName":"Sala A","personalTraining":false}
            ]
            """));
        var viewModel = new ScheduleViewModel(service);
        await WaitForAsync(() => !viewModel.IsLoading && viewModel.FilteredClasses.Count == 3);

        viewModel.SelectedTrainerFilter = "Anna";
        viewModel.SelectedLocationFilter = "Sala A";
        viewModel.SelectedTypeFilter = "Zajęcia grupowe";
        viewModel.SelectedAvailabilityFilter = "Pełne zajęcia";
        viewModel.SearchText = "mob";

        var gymClass = Assert.Single(viewModel.FilteredClasses);
        Assert.Equal("Mobility", gymClass.Name);
        Assert.Equal("Wyświetlono: 1 z 3 zajęć  ·  Zapisanych uczestników: 8", viewModel.FooterText);
    }

    [Fact]
    public async Task DateCommands_UpdateSelectedDateAndWeekDays()
    {
        var service = await CreateLoggedInServiceAsync(
            JsonResponse(HttpStatusCode.OK, "[]"),
            JsonResponse(HttpStatusCode.OK, "[]"),
            JsonResponse(HttpStatusCode.OK, "[]"));
        var viewModel = new ScheduleViewModel(service);
        await WaitForAsync(() => !viewModel.IsLoading);
        var initialDate = viewModel.SelectedDate.Date;

        viewModel.NextDayCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsLoading && viewModel.SelectedDate.Date == initialDate.AddDays(1));

        Assert.Equal(initialDate.AddDays(1), viewModel.SelectedDate.Date);
        Assert.Equal(7, viewModel.WeekDays.Count);
        Assert.Contains(viewModel.WeekDays, day => day.Date == viewModel.SelectedDate.Date && day.IsSelected);
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
