using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.ImageGeneration;

public class ImageGenerationService(
    HttpClient     http,
    OpenAiConfig   openAiConfig,
    R2Config       r2Config,
    ILogger<ImageGenerationService> logger) : IImageGenerationService
{
    public async Task<string?> GenerateAndStoreAsync(string prompt, string episodeId)
    {
        try
        {
            var imageBytes = await CallImageApi(prompt);
            if (imageBytes is null) return null;

            return await UploadToR2(imageBytes, episodeId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image generation failed for episode {EpisodeId}", episodeId);
            return null;
        }
    }

    private async Task<byte[]?> CallImageApi(string prompt)
    {
        var request = new
        {
            model   = openAiConfig.Model,
            prompt  = prompt,
            n       = 1,
            size    = openAiConfig.ImageSize,
            quality = openAiConfig.ImageQuality,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "v1/images/generations");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiConfig.ApiKey);
        req.Content = JsonContent.Create(request);

        var res = await http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            logger.LogWarning("Image API returned {Status}: {Body}", res.StatusCode, await res.Content.ReadAsStringAsync());
            return null;
        }

        var body = await res.Content.ReadFromJsonAsync<ImageApiResponse>();
        var b64  = body?.Data?.FirstOrDefault()?.B64Json;
        return b64 is null ? null : Convert.FromBase64String(b64);
    }

    private async Task<string?> UploadToR2(byte[] imageBytes, string episodeId)
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL    = r2Config.Endpoint,
            ForcePathStyle = true,
        };

        var credentials = new BasicAWSCredentials(r2Config.AccessKeyId, r2Config.SecretAccessKey);
        using var s3 = new AmazonS3Client(credentials, s3Config);

        var key = $"episodes/{episodeId}.jpg";

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName            = r2Config.BucketName,
            Key                   = key,
            InputStream           = new MemoryStream(imageBytes),
            ContentType           = "image/jpeg",
            DisablePayloadSigning = true,  // R2 doesn't support STREAMING-AWS4-HMAC-SHA256
        });

        return $"{r2Config.PublicUrl.TrimEnd('/')}/{key}";
    }
}

file record ImageApiResponse(ImageApiData[]? Data);
file record ImageApiData([property: JsonPropertyName("b64_json")] string? B64Json);
