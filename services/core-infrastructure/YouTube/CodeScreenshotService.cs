using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class CodeScreenshotService(ILogger<CodeScreenshotService> logger) : ICodeScreenshotService
{
    public async Task<byte[]?[]> CaptureMultipleAsync(IReadOnlyList<SlideContent> slides)
    {
        var results = new byte[]?[slides.Count];
        try
        {
            using var playwright  = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args     = ["--no-sandbox", "--disable-setuid-sandbox"],
            });

            for (var i = 0; i < slides.Count; i++)
            {
                var page = await browser.NewPageAsync(new BrowserNewPageOptions
                {
                    ViewportSize = new ViewportSize { Width = 1080, Height = 1920 },
                });
                await page.SetContentAsync(SlideRenderer.Render(slides[i]));
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                results[i] = await page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png });
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Screenshot capture failed");
        }
        return results;
    }
}
