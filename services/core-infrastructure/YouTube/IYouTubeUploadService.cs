namespace DevPulse.Infrastructure.YouTube;

public interface IYouTubeUploadService
{
    Task<string?> UploadAsync(byte[] videoBytes, string title, string description, IEnumerable<string> tags);
}
