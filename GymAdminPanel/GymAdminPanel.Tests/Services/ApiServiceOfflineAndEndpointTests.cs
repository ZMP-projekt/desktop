using System.Net;
using System.Net.Http;
using System.Text;
using GymAdminPanel.Models;
using GymAdminPanel.Services;

namespace GymAdminPanel.Tests.Services;

public class ApiServiceOfflineAndEndpointTests
{
    [Fact]
    public async Task ReadEndpoint_WhenApiFailsAndCacheExists_ReturnsCachedDataAndMarksOffline()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, """[{"id":91,"name":"Cached Gym","city":"Warszawa","address":"Testowa 1"}]"""),
            JsonResponse(HttpStatusCode.InternalServerError, """{"message":"Failure"}"""));
        var service = CreateService(handler);
        await LoginAsync(service);

        var onlineResult = await service.GetLocationsAsync();
        Assert.False(service.LastResultFromCache);
        var cachedResult = await service.GetLocationsAsync();

        Assert.Single(onlineResult);
        Assert.Single(cachedResult);
        Assert.Equal("Cached Gym", cachedResult[0].Name);
        Assert.True(service.IsOffline);
        Assert.True(service.LastResultFromCache);
        Assert.NotNull(service.LastCacheUpdatedAt);
    }

    [Fact]
    public async Task ReadEndpoint_WhenApiFailsAndCacheIsEmpty_ReturnsEmptyListAndPublishesError()
    {
        var dateWithoutCache = new DateTime(2098, 12, 30);
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.InternalServerError, """{"message":"Failure"}"""));
        var service = CreateService(handler);
        AppStatus? status = null;
        service.StatusChanged += value => status = value;
        await LoginAsync(service);

        var result = await service.GetClassesByDateAsync(dateWithoutCache);

        Assert.Empty(result);
        Assert.True(service.IsOffline);
        Assert.False(service.LastResultFromCache);
        Assert.NotNull(status);
        Assert.Equal(AppStatusKind.Error, status.Kind);
    }

    [Fact]
    public async Task ReadEndpoint_WhenApiRecoversAfterOffline_ReturnsOnlineStatus()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}"""),
            JsonResponse(HttpStatusCode.OK, """[{"id":92,"name":"Recovery Gym","city":"Kraków","address":"Rynek 2"}]"""),
            JsonResponse(HttpStatusCode.InternalServerError, """{"message":"Failure"}"""),
            JsonResponse(HttpStatusCode.OK, """[{"id":93,"name":"Online Again","city":"Gdańsk","address":"Morska 3"}]"""));
        var service = CreateService(handler);
        await LoginAsync(service);

        await service.GetLocationsAsync();
        await service.GetLocationsAsync();
        var recovered = await service.GetLocationsAsync();

        Assert.False(service.IsOffline);
        Assert.False(service.LastResultFromCache);
        Assert.Equal("Online Again", recovered[0].Name);
    }

    [Fact]
    public async Task GetClassesByDateAsync_SendsExpectedRequestAndParsesResponse()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """[{"id":7,"name":"Yoga","trainerName":"Anna","startTime":"2026-05-17T09:00:00Z","endTime":"2026-05-17T10:00:00Z","currentParticipants":4,"maxParticipants":12,"locationName":"Sala A","personalTraining":false}]""")
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var result = await service.GetClassesByDateAsync(new DateTime(2026, 5, 17));

        Assert.Single(result);
        Assert.Equal("Yoga", result[0].Name);
        Assert.Equal("/api/classes/by-date", handler.Requests[2].RequestUri?.AbsolutePath);
        Assert.Contains("date=", handler.Requests[2].RequestUri?.Query);
    }

    [Fact]
    public async Task TrainerEndpoints_ParseAndSendExpectedRequests()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """[{"id":11,"firstName":"Adam","lastName":"Trener","specialization":"Siła","bio":"Bio","photoUrl":"photo"}]"""),
                JsonResponse(HttpStatusCode.OK, "")
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var trainers = await service.GetTrainersAsync();
        var updated = await service.UpdateTrainerAsync(11, new UpdateTrainerRequest
        {
            FirstName = "Adam",
            LastName = "Trener",
            Specialization = "Mobility",
            Bio = "Updated",
            PhotoUrl = "photo2"
        });

        Assert.Single(trainers);
        Assert.Equal("Adam", trainers[0].FirstName);
        Assert.True(updated);
        Assert.Equal("/api/trainers", handler.Requests[2].RequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.Requests[3].Method);
        Assert.Equal("/api/admin/trainers/11", handler.Requests[3].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetRoleVerifiedTrainersAsync_ReturnsOnlyUsersWithTrainerRole()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":11,"firstName":"Adam","lastName":"Trener","specialization":"Sila","bio":"Bio","photoUrl":"photo"},
                  {"id":12,"firstName":"Ewa","lastName":"Byla","specialization":"Yoga","bio":"Bio","photoUrl":"photo"},
                  {"id":13,"userId":21,"firstName":"Marta","lastName":"Aktywna","specialization":"Pilates","bio":"Bio","photoUrl":"photo"}
                ]
                """),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":11,"email":"adam@test.pl","role":"ROLE_TRAINER","firstName":"Adam","lastName":"Trener"},
                  {"id":12,"email":"ewa@test.pl","role":"ROLE_USER","firstName":"Ewa","lastName":"Byla"},
                  {"id":21,"email":"marta@test.pl","role":"ROLE_TRAINER","firstName":"Marta","lastName":"Aktywna"}
                ]
                """)
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var trainers = await service.GetRoleVerifiedTrainersAsync();

        Assert.Equal([11, 13], trainers.Select(trainer => trainer.Id));
        Assert.Equal("/api/trainers", handler.Requests[2].RequestUri?.AbsolutePath);
        Assert.Equal("/api/admin/users", handler.Requests[3].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetRoleVerifiedTrainersAsync_WhenProfileIdDiffersFromUserId_ReturnsOnlyTrainerUsers()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":101,"firstName":"Adam","lastName":"Trener","specialization":"Sila","bio":"Bio","photoUrl":"photo"},
                  {"id":102,"firstName":"Ewa","lastName":"Byla","specialization":"Yoga","bio":"Bio","photoUrl":"photo"}
                ]
                """),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":11,"email":"adam@test.pl","role":"ROLE_TRAINER","firstName":"Adam","lastName":"Trener"},
                  {"id":12,"email":"ewa@test.pl","role":"ROLE_USER","firstName":"Ewa","lastName":"Byla"}
                ]
                """)
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var trainers = await service.GetRoleVerifiedTrainersAsync();

        var trainer = Assert.Single(trainers);
        Assert.Equal(101, trainer.Id);
        Assert.Equal("Adam", trainer.FirstName);
        Assert.Equal("Trener", trainer.LastName);
        Assert.Equal("Bio", trainer.Bio);
    }

    [Fact]
    public async Task GetRoleVerifiedTrainersAsync_WhenProfileIsUnlinked_UsesRemainingProfileForBlankTrainerUser()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":0,"firstName":"Stary","lastName":"Profil","specialization":"Yoga","bio":"Bio","photoUrl":"photo"}
                ]
                """),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":11,"email":"active.trainer@test.pl","role":"ROLE_TRAINER","firstName":"","lastName":""},
                  {"id":12,"email":"regular.user@test.pl","role":"ROLE_USER","firstName":"","lastName":""}
                ]
                """)
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var trainers = await service.GetRoleVerifiedTrainersAsync();

        var trainer = Assert.Single(trainers);
        Assert.Equal(11, trainer.Id);
        Assert.Equal("active.trainer@test.pl", trainer.Email);
        Assert.Equal("Stary", trainer.FirstName);
        Assert.Equal("Profil", trainer.LastName);
        Assert.Equal("Bio", trainer.Bio);
    }

    [Fact]
    public async Task GetRoleVerifiedTrainersAsync_WhenUserEmailMatchesProfileName_DoesNotAddDuplicateUserCard()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":0,"firstName":"Mikolaj","lastName":"Woloszyn","specialization":"Mobility","bio":"Opis profilu","photoUrl":"photo"}
                ]
                """),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":19,"email":"mikolaj@email.pl","role":"ROLE_TRAINER","firstName":"","lastName":""}
                ]
                """)
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var trainers = await service.GetRoleVerifiedTrainersAsync();

        var trainer = Assert.Single(trainers);
        Assert.Equal(19, trainer.Id);
        Assert.Equal(19, trainer.UserId);
        Assert.Equal("Mikolaj", trainer.FirstName);
        Assert.Equal("Woloszyn", trainer.LastName);
        Assert.Equal("Opis profilu", trainer.Bio);
    }

    [Fact]
    public async Task GetRoleVerifiedTrainersAsync_UsesScheduledTrainerNameToFillMissingProfileDetails()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":7,"name":"Boxing","trainerName":"Unique Coach","startTime":"2097-01-01T09:00:00Z","endTime":"2097-01-01T10:00:00Z","currentParticipants":1,"maxParticipants":10,"locationName":"Sala A","personalTraining":false}
                ]
                """),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":0,"firstName":"Unique","lastName":"Coach","specialization":"Box","bio":"Opis z profilu","photoUrl":"photo"}
                ]
                """),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":28,"email":"trainer2@example.com","role":"ROLE_TRAINER","firstName":"","lastName":""}
                ]
                """)
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);
        await service.GetClassesByDateAsync(new DateTime(2097, 1, 1));

        var trainers = await service.GetRoleVerifiedTrainersAsync();

        var trainer = Assert.Single(trainers);
        Assert.Equal(28, trainer.Id);
        Assert.Equal(28, trainer.UserId);
        Assert.Equal("trainer2@example.com", trainer.Email);
        Assert.Equal("Unique", trainer.FirstName);
        Assert.Equal("Coach", trainer.LastName);
        Assert.Equal("Opis z profilu", trainer.Bio);
    }

    [Fact]
    public async Task GetRoleVerifiedTrainersAsync_WhenNoProfilesExist_ReturnsTrainerUsers()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, "[]"),
                JsonResponse(HttpStatusCode.OK, """
                [
                  {"id":11,"email":"active.trainer@test.pl","role":"ROLE_TRAINER","firstName":"","lastName":""}
                ]
                """)
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var trainers = await service.GetRoleVerifiedTrainersAsync();

        var trainer = Assert.Single(trainers);
        Assert.Equal(11, trainer.Id);
        Assert.Equal("active.trainer@test.pl", trainer.Email);
    }

    [Fact]
    public async Task AuditLogsAndLocationsEndpoints_ParseResponses()
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(HttpStatusCode.OK, """[{"changedBy":"admin@test.pl","action":"CREATE","details":"Added","timestamp":"2026-05-17T10:00:00Z"}]"""),
                JsonResponse(HttpStatusCode.OK, """[{"id":12,"name":"Main","city":"Łódź","address":"Centralna 1"}]""")
            }).ToArray());
        var service = CreateService(handler);
        await LoginAsync(service);

        var logs = await service.GetAuditLogsAsync();
        var locations = await service.GetLocationsAsync();

        Assert.Single(logs);
        Assert.Equal("CREATE", logs[0].Action);
        Assert.Single(locations);
        Assert.Equal("Łódź", locations[0].City);
        Assert.Equal("/api/admin/audit-logs", handler.Requests[2].RequestUri?.AbsolutePath);
        Assert.Equal("/api/locations", handler.Requests[3].RequestUri?.AbsolutePath);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Sesja wygasła. Zaloguj się ponownie.")]
    [InlineData(HttpStatusCode.Forbidden, "Brak uprawnień administratora. Zaloguj się na konto z odpowiednimi uprawnieniami.")]
    public async Task ReadEndpoints_WhenAuthorizationFails_RaiseSessionExpiredWithoutOffline(HttpStatusCode statusCode, string expectedMessage)
    {
        var handler = new QueueHttpMessageHandler(
            LoginResponses().Concat(new[]
            {
                JsonResponse(statusCode, """{"message":"Auth"}""")
            }).ToArray());
        var service = CreateService(handler);
        var expiredMessage = string.Empty;
        service.SessionExpired += message => expiredMessage = message;
        await LoginAsync(service);

        var result = await service.GetTrainersAsync();

        Assert.Empty(result);
        Assert.Empty(service.Token);
        Assert.False(service.IsOffline);
        Assert.Equal(expectedMessage, expiredMessage);
    }

    private static async Task LoginAsync(ApiService service)
    {
        var loggedIn = await service.LoginAsync("admin@test.pl", "secret");
        Assert.True(loggedIn);
    }

    private static ApiService CreateService(HttpMessageHandler handler)
        => ApiServiceTestFactory.Create(handler);

    private static HttpResponseMessage[] LoginResponses() =>
    [
        JsonResponse(HttpStatusCode.OK, """{"token":"admin-token"}"""),
        JsonResponse(HttpStatusCode.OK, """{"id":1,"email":"admin@test.pl","role":"ROLE_ADMIN"}""")
    ];

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
