namespace DevPulse.Infrastructure.YouTube;

public interface ITtsService
{
    Task<byte[]?> SynthesizeAsync(string text);
}
