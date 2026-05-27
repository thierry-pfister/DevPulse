namespace DevPulse.Infrastructure.YouTube;

public interface IVideoAssemblyService
{
    Task<byte[]?> AssembleAsync(byte[] imageBytes, byte[] audioBytes);
}
