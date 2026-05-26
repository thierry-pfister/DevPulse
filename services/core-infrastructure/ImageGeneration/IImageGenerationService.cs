namespace DevPulse.Infrastructure.ImageGeneration;

public interface IImageGenerationService
{
    Task<string?> GenerateAndStoreAsync(string prompt, string episodeId);
}
