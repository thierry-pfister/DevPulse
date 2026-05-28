namespace DevPulse.Infrastructure.YouTube;

public interface IR2VideoStorage
{
    Task<string?> UploadVideoAsync(byte[] videoBytes, Guid episodeId);
}
