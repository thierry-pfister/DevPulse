using System.Net;
using DevPulse.Application.Publishing;
using DevPulse.Infrastructure.Publishers;
using DevPulse.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace DevPulse.Tests.Publishers;

public class DevToPublisherTests
{
    private static DevToPublisher BuildPublisher(MockHttpMessageHandler mock, string apiKey = "test-key")
    {
        var client = mock.ToHttpClient();
        client.BaseAddress = new Uri("https://dev.to/");
        var config = Options.Create(new DevToConfig { ApiKey = apiKey });
        return new DevToPublisher(client, config);
    }

    [Fact]
    public async Task PublishAsync_skips_when_api_key_is_empty()
    {
        var mock = new MockHttpMessageHandler();

        var result = await BuildPublisher(mock, apiKey: "").PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Skipped>();
        mock.GetMatchCount(mock.When("*")) .Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_returns_published_with_article_id_on_success()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/articles")
            .Respond("application/json", """{"id":42,"url":"https://dev.to/user/article"}""");

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Published>()
            .Which.platformId.Should().Be("42");
    }

    [Fact]
    public async Task PublishAsync_returns_failed_when_api_returns_error()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/articles")
            .Respond(HttpStatusCode.UnprocessableEntity, "application/json", """{"error":"tag not found"}""");

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Failed>();
    }

    [Fact]
    public async Task PublishAsync_returns_failed_on_network_exception()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*").Throw(new HttpRequestException("Network error"));

        var result = await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        result.Should().BeOfType<PublishResult.Failed>();
    }

    [Fact]
    public async Task PublishAsync_sets_canonical_url_to_blog_slug()
    {
        string? capturedBody = null;
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "*/api/articles")
            .With(req => { capturedBody = req.Content?.ReadAsStringAsync().Result; return true; })
            .Respond("application/json", """{"id":1,"url":"https://dev.to/user/article"}""");

        await BuildPublisher(mock).PublishAsync(EpisodeFixtures.Simple());

        capturedBody.Should().Contain("thierrypfister.dev/blog/the-maybe-monad");
    }
}
