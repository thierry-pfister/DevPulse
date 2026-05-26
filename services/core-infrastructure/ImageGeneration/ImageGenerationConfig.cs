namespace DevPulse.Infrastructure.ImageGeneration;

public class OpenAiConfig
{
    public string ApiKey       { get; init; } = string.Empty;
    public string Model        { get; init; } = "gpt-image-1";
    public string ImageSize    { get; init; } = "1024x1024";
    public string ImageQuality { get; init; } = "medium";
}

public class R2Config
{
    public string AccessKeyId     { get; init; } = string.Empty;
    public string SecretAccessKey { get; init; } = string.Empty;
    public string BucketName      { get; init; } = string.Empty;
    public string Endpoint        { get; init; } = string.Empty;
    public string PublicUrl       { get; init; } = string.Empty;
}
