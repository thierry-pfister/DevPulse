using System.Net.Http.Json;
using DevPulse.Application.Publishing;
using DevPulse.Domain.Episodes;

namespace DevPulse.Infrastructure.Publishers;

public class PfstrCorePublisher(HttpClient http) : IPublisher
{
    public string Name => "pfstr-core";

    public async Task<PublishResult> PublishAsync(EpisodeOutput episode)
    {
        try
        {
            var article = episode.Article;
            var slug    = SlugGenerator.From(article.Title);

            var createRes = await http.PostAsJsonAsync("/api/posts", new
            {
                title   = article.Title,
                slug    = slug,
                summary = article.Subtitle,
            });

            if (!createRes.IsSuccessStatusCode)
                return PublishResult.NewFailed($"Create failed: {createRes.StatusCode}");

            var created = await createRes.Content.ReadFromJsonAsync<CreatePostResponse>();
            if (created is null)
                return PublishResult.NewFailed("Empty create response from pfstr-core");

            var updateRes = await http.PutAsJsonAsync($"/api/posts/{created.Id}", new
            {
                title   = article.Title,
                summary = article.Subtitle,
                content = article.Body,
                tags    = article.Tags,
            });

            if (!updateRes.IsSuccessStatusCode)
                return PublishResult.NewFailed($"Update failed: {updateRes.StatusCode}");

            var publishRes = await http.PostAsync($"/api/posts/{created.Id}/publish", null);

            return publishRes.IsSuccessStatusCode
                ? PublishResult.NewPublished(created.Id.ToString())
                : PublishResult.NewFailed($"Publish failed: {publishRes.StatusCode}");
        }
        catch (Exception ex)
        {
            return PublishResult.NewFailed($"pfstr-core publisher error: {ex.Message}");
        }
    }
}

file record CreatePostResponse(Guid Id);
