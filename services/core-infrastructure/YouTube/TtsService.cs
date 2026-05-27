using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class TtsService(
    HttpClient            http,
    TtsConfig             config,
    ILogger<TtsService>   logger) : ITtsService
{
    public async Task<byte[]?> SynthesizeAsync(string text)
    {
        try
        {
            var request = new { model = config.Model, voice = config.Voice, input = text };

            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/audio/speech");
            req.Content = JsonContent.Create(request);

            var res = await http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("TTS API returned {Status}: {Body}", res.StatusCode, await res.Content.ReadAsStringAsync());
                return null;
            }

            return await res.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TTS synthesis failed");
            return null;
        }
    }
}
