using System.Net.Http.Json;
using DevPulse.Application.Publishing;
using DevPulse.Domain.Episodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevPulse.Infrastructure.Publishers;

public class DevToPublisher(HttpClient http, IOptions<DevToConfig> config, ILogger<DevToPublisher> logger) : IPublisher
{
    public string Name => "dev.to";

    public async Task<PublishResult> PublishAsync(EpisodeOutput episode)
    {
        if (string.IsNullOrWhiteSpace(config.Value.ApiKey))
            return PublishResult.NewSkipped("Dev.to API key not configured — skipping");

        try
        {
            var article   = episode.Article;
            var slug      = SlugGenerator.From(article.Title);
            var canonical = $"https://thierrypfister.dev/blog/{slug}";

            var response = await http.PostAsJsonAsync("/api/articles", new
            {
                article = new
                {
                    title         = article.Title,
                    body_markdown = article.Body,
                    published     = true,
                    tags          = article.Tags.Take(4).ToArray(),
                    canonical_url = canonical,
                }
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Dev.to failed ({Status}): {Body}", response.StatusCode, error);
                return PublishResult.NewFailed($"Dev.to failed ({response.StatusCode}): {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<DevToArticleResponse>();
            return result is not null
                ? PublishResult.NewPublished(result.Id.ToString())
                : PublishResult.NewFailed("Empty response from Dev.to");
        }
        catch (Exception ex)
        {
            return PublishResult.NewFailed($"Dev.to publisher error: {ex.Message}");
        }
    }
}

file record DevToArticleResponse(int Id, string Url);
