using System.Text;
using System.Text.Json;
using DevPulse.Domain.Episodes;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.Notifications;

public class ResendNotifier(
    HttpClient             http,
    ResendConfig           config,
    ILogger<ResendNotifier> logger) : IEmailNotifier
{
    public async Task SendDraftReadyAsync(Episode episode)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey)) return;

        var subject = $"[DevPulse] Draft ready — #{episode.EpisodeNumber}: {episode.Concept}";
        var html    = $"""
            <h2>New draft is ready for review</h2>
            <p><strong>Episode #{episode.EpisodeNumber}:</strong> {episode.Concept}</p>
            <p><strong>Tag:</strong> {episode.Tag}</p>
            <p>Review it at <a href="http://localhost:3000/draft">localhost:3000/draft</a></p>
            <p>You have 30 minutes before it auto-publishes.</p>
            """;

        await Send(subject, html);
    }

    public async Task SendPublishedAsync(Episode episode)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey)) return;

        var subject = $"[DevPulse] Published — #{episode.EpisodeNumber}: {episode.Concept}";
        var html    = $"""
            <h2>Episode published successfully</h2>
            <p><strong>Episode #{episode.EpisodeNumber}:</strong> {episode.Concept}</p>
            <p><strong>Tag:</strong> {episode.Tag}</p>
            <p><strong>Published at:</strong> {episode.PublishedAt?.Value:u}</p>
            """;

        await Send(subject, html);
    }

    private async Task Send(string subject, string html)
    {
        var body    = new { from = config.FromEmail, to = config.ToEmail, subject, html };
        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            var resp = await http.PostAsync("emails", content);
            if (!resp.IsSuccessStatusCode)
                logger.LogWarning("Resend returned {Status} for subject '{Subject}'", resp.StatusCode, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email '{Subject}'", subject);
        }
    }
}
