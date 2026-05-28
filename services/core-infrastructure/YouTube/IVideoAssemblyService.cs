namespace DevPulse.Infrastructure.YouTube;

public interface IVideoAssemblyService
{
    Task<byte[]?> AssembleFromSlidesAsync(
        IReadOnlyList<(byte[] Image, double DurationSeconds)> slides,
        byte[] audioBytes);
}
