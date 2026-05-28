using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Upload;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class YouTubeUploadService(
    YouTubeConfig                   config,
    ILogger<YouTubeUploadService>   logger) : IYouTubeUploadService
{
    public async Task<string?> UploadAsync(byte[] videoBytes, string title, string description, IEnumerable<string> tags)
    {
        try
        {
            var credential = await BuildCredentialAsync();
            using var youtube = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName       = "DevPulse",
            });

            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title       = title,
                    Description = YouTubeDescriptionCleaner.Clean(description),
                    Tags        = tags.Concat(["Shorts"]).ToList(),
                },
                Status = new VideoStatus { PrivacyStatus = "public" },
            };

            using var stream = new MemoryStream(videoBytes);
            var insert = youtube.Videos.Insert(video, "snippet,status", stream, "video/mp4");

            IUploadProgress result = await insert.UploadAsync();
            if (result.Status != UploadStatus.Completed)
            {
                logger.LogWarning("YouTube upload did not complete: {Status} — {Exception}", result.Status, result.Exception?.Message);
                return null;
            }

            var videoId = insert.ResponseBody?.Id;
            logger.LogInformation("Uploaded YouTube Short: https://youtube.com/shorts/{Id}", videoId);
            return videoId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YouTube upload failed for '{Title}'", title);
            return null;
        }
    }

    private async Task<UserCredential> BuildCredentialAsync()
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId     = config.ClientId,
                ClientSecret = config.ClientSecret,
            },
            Scopes = [YouTubeService.Scope.YoutubeUpload],
        });

        var token      = new TokenResponse { RefreshToken = config.RefreshToken };
        var credential = new UserCredential(flow, "user", token);
        await credential.RefreshTokenAsync(CancellationToken.None);
        return credential;
    }
}
