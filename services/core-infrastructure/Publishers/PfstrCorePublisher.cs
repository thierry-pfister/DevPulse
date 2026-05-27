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

            Guid postId;
            if (createRes.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var existing = await http.GetFromJsonAsync<GetPostBySlugResponse>($"/api/posts/{slug}");
                if (existing is null)
                    return PublishResult.NewFailed("Post conflict but could not find existing post by slug");
                postId = existing.Id;
            }
            else if (createRes.IsSuccessStatusCode)
            {
                var created = await createRes.Content.ReadFromJsonAsync<CreatePostResponse>();
                if (created is null)
                    return PublishResult.NewFailed("Empty create response from pfstr-core");
                postId = created.Id;
            }
            else
            {
                return PublishResult.NewFailed($"Create failed: {createRes.StatusCode}");
            }

            var updateRes = await http.PutAsJsonAsync($"/api/posts/{postId}", new
            {
                title        = article.Title,
                summary      = article.Subtitle,
                subtitle     = article.Subtitle,
                content      = article.Body,
                coverImage   = article.CoverImageUrl?.Value,
                canonicalUrl = $"https://thierrypfister.dev/blog/{slug}",
                tags         = article.Tags,
            });

            if (!updateRes.IsSuccessStatusCode)
                return PublishResult.NewFailed($"Update failed: {updateRes.StatusCode}");

            var publishRes = await http.PostAsync($"/api/posts/{postId}/publish", null);

            return publishRes.IsSuccessStatusCode
                ? PublishResult.NewPublished(postId.ToString())
                : PublishResult.NewFailed($"Publish failed: {publishRes.StatusCode}");
        }
        catch (Exception ex)
        {
            return PublishResult.NewFailed($"pfstr-core publisher error: {ex.Message}");
        }
    }
}

file record CreatePostResponse(Guid Id);
file record GetPostBySlugResponse(Guid Id);
