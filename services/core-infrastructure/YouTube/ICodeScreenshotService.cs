namespace DevPulse.Infrastructure.YouTube;

public interface ICodeScreenshotService
{
    Task<byte[]?[]> CaptureMultipleAsync(IReadOnlyList<SlideContent> slides);
}
