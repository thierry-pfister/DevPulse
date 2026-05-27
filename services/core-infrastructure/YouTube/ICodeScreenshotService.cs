namespace DevPulse.Infrastructure.YouTube;

public interface ICodeScreenshotService
{
    Task<byte[]?> CaptureAsync(string code, string title);
}
