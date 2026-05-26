using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevPulse.Application.Generation;
using DevPulse.Domain.Episodes;
using DevPulse.Infrastructure.Episodes;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.Claude;

public class ClaudeClient(
    HttpClient            http,
    ClaudeConfig          config,
    ILogger<ClaudeClient> logger) : IClaudeClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── IClaudeClient ─────────────────────────────────────────────────────────

    public async Task<FSharpResult<EpisodeOutput, string>> Generate(GenerationInput input)
    {
        var prompt = BuildGenerationPrompt(input);
        try
        {
            var text   = await CallApi(prompt);
            var output = EpisodeMapper.DeserializeContent(text);
            return FSharpResult<EpisodeOutput, string>.NewOk(output);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude generation failed for concept '{Concept}'", input.Concept);
            return FSharpResult<EpisodeOutput, string>.NewError(ex.Message);
        }
    }

    public async Task<FSharpList<string>> SuggestTopicsAsync(string tag, int count, FSharpList<string> existing)
    {
        var existingList = string.Join(", ", existing.Take(30));
        var prompt = $"""
            You are a developer educator. Suggest {count} fresh, specific concept names for the '{tag}' tag.
            These topics already exist (do not repeat them): {existingList}

            Return ONLY a JSON array of strings. Example: ["Topic A", "Topic B"]
            Focus on practical, specific concepts that intermediate developers would find valuable.
            """;
        try
        {
            var text  = await CallApi(prompt);
            var clean = StripMarkdownFences(text);
            var list  = JsonSerializer.Deserialize<List<string>>(clean, JsonOpts) ?? [];
            return ListModule.OfSeq(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude topic suggestion failed for tag '{Tag}'", tag);
            return FSharpList<string>.Empty;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> CallApi(string userMessage)
    {
        var body = new
        {
            model      = config.Model,
            max_tokens = config.MaxTokens,
            system     = "You are a developer educator. Always respond with valid JSON only, no markdown fences, no explanation.",
            messages   = new[] { new { role = "user", content = userMessage } }
        };

        var json     = JsonSerializer.Serialize(body, JsonOpts);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var resp     = await http.PostAsync("v1/messages", content);
        resp.EnsureSuccessStatusCode();

        var raw      = await resp.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<AnthropicResponse>(raw, JsonOpts)
            ?? throw new InvalidOperationException("Empty response from Claude API");

        return envelope.Content.First(c => c.Type == "text").Text;
    }

    private static string BuildGenerationPrompt(GenerationInput i)
    {
        var language   = i.Language is null   ? "any"  : i.Language.Value;
        var recentStr  = i.RecentConcepts.IsEmpty ? "none" : string.Join(", ", i.RecentConcepts);
        var foreshadow = i.ForeshadowTopic  is null ? "" : $"\n- Foreshadow tomorrow's topic: {i.ForeshadowTopic.Value}";
        var anchor     = i.RealWorldAnchorHint is null ? "" : $"\n- Real-world anchor hint: {i.RealWorldAnchorHint.Value}";

        return $$"""
            Generate a developer education post with the following parameters:
            - Concept: {{i.Concept}}
            - Tag: {{i.Tag}}
            - Language: {{language}}
            - Episode number: {{i.EpisodeNumber}}
            - Tone: {{i.Tone}}
            - Target audience: {{i.TargetAudience}}
            - Runnable code snippet: {{(i.Runnable ? "yes" : "no")}}
            - Recent concepts (do not re-explain): {{recentStr}}{{foreshadow}}{{anchor}}

            Return ONLY a JSON object matching this exact schema (camelCase keys):
            {
              "article": {
                "title": "string",
                "subtitle": "string — one compelling sentence",
                "realWorldAnchor": "string — connect to something the reader already knows",
                "body": "string — full markdown article, 400-600 words",
                "runnableSnippet": "string or null",
                "imagePrompt": "string — DALL-E prompt for a cover image",
                "foreshadow": "string — one sentence previewing tomorrow naturally",
                "tags": ["string"]
              },
              "reddit": { "title": "string", "body": "string" } or null,
              "youTube": { "title": "string", "description": "string", "script": "string" } or null
            }
            """;
    }

    private static string StripMarkdownFences(string text)
    {
        var t = text.Trim();
        if (t.StartsWith("```"))
        {
            var first  = t.IndexOf('\n');
            var last   = t.LastIndexOf("```", StringComparison.Ordinal);
            if (first >= 0 && last > first)
                return t[(first + 1)..last].Trim();
        }
        return t;
    }

    private record AnthropicResponse(AnthropicContent[] Content);
    private record AnthropicContent(string Type, string Text);
}
