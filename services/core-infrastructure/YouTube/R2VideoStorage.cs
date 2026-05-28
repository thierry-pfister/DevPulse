using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DevPulse.Infrastructure.ImageGeneration;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class R2VideoStorage(R2Config r2Config, ILogger<R2VideoStorage> logger) : IR2VideoStorage
{
    public async Task<string?> UploadVideoAsync(byte[] videoBytes, Guid episodeId)
    {
        try
        {
            var s3Config = new AmazonS3Config { ServiceURL = r2Config.Endpoint, ForcePathStyle = true };
            var creds    = new BasicAWSCredentials(r2Config.AccessKeyId, r2Config.SecretAccessKey);
            using var s3 = new AmazonS3Client(creds, s3Config);

            var key = $"shorts/{episodeId}.mp4";
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName            = r2Config.BucketName,
                Key                   = key,
                InputStream           = new MemoryStream(videoBytes),
                ContentType           = "video/mp4",
                DisablePayloadSigning = true,
            });

            return $"{r2Config.PublicUrl.TrimEnd('/')}/{key}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "R2 upload failed for short {Id}", episodeId);
            return null;
        }
    }
}
