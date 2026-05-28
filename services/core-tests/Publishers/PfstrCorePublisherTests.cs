using System.Net;
using System.Net.Http.Json;
using DevPulse.Application.Publishing;
using DevPulse.Infrastructure.Publishers;
using DevPulse.Tests.Fixtures;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace DevPulse.Tests.Publishers;

public class PfstrCorePublisherTests
{
    private static readonly Guid PostId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    private static PfstrCorePublisher BuildPublisher(MockHttpMessageHandler mock)
    {
        var client = mock.ToHttpClient();
        client.BaseAddress = new Uri("http://pfstr-core/");
        return new PfstrCorePublisher(client);
    }

    [Fact]
    public async Task PublishAsync_returns_published_with_post_id_on_success()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/posts")
            .Respond("application/json", $$$"""{"id":"{{{PostId}}}"}""");
        mock.When(HttpMethod.Put, $"*/api/posts/{PostId}")
            .Respond(HttpStatusCode.NoContent);
        mock.When(HttpMethod.Post, $"*/api/posts/{PostId}/publish")
            .Respond(HttpStatusCode.NoContent);

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Published>()
            .Which.platformId.Should().Be(PostId.ToString());
    }

    [Fact]
    public async Task PublishAsync_returns_failed_when_create_returns_error()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/posts")
            .Respond(HttpStatusCode.InternalServerError);

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Failed>();
    }

    [Fact]
    public async Task PublishAsync_returns_failed_when_update_returns_error()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/posts")
            .Respond("application/json", $$$"""{"id":"{{{PostId}}}"}""");
        mock.When(HttpMethod.Put, $"*/api/posts/{PostId}")
            .Respond(HttpStatusCode.BadRequest);

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Failed>();
    }

    [Fact]
    public async Task PublishAsync_treats_422_as_published_already_published()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/posts")
            .Respond("application/json", $$$"""{"id":"{{{PostId}}}"}""");
        mock.When(HttpMethod.Put, $"*/api/posts/{PostId}")
            .Respond(HttpStatusCode.NoContent);
        mock.When(HttpMethod.Post, $"*/api/posts/{PostId}/publish")
            .Respond(HttpStatusCode.UnprocessableEntity);

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Published>();
    }

    [Fact]
    public async Task PublishAsync_returns_failed_when_publish_endpoint_returns_server_error()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/posts")
            .Respond("application/json", $$$"""{"id":"{{{PostId}}}"}""");
        mock.When(HttpMethod.Put, $"*/api/posts/{PostId}")
            .Respond(HttpStatusCode.NoContent);
        mock.When(HttpMethod.Post, $"*/api/posts/{PostId}/publish")
            .Respond(HttpStatusCode.InternalServerError);

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Failed>();
    }

    [Fact]
    public async Task PublishAsync_returns_failed_on_network_exception()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*").Throw(new HttpRequestException("Connection refused"));

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Failed>();
    }
}
