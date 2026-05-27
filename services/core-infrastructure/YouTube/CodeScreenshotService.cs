using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class CodeScreenshotService(ILogger<CodeScreenshotService> logger) : ICodeScreenshotService
{
    public async Task<byte[]?> CaptureAsync(string code, string title)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args     = ["--no-sandbox", "--disable-setuid-sandbox"],
            });

            var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = 1080, Height = 1920 },
            });

            await page.SetContentAsync(BuildHtml(title, code));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            return await page.ScreenshotAsync(new PageScreenshotOptions { Type = ScreenshotType.Png });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Code screenshot failed for '{Title}'", title);
            return null;
        }
    }

    private static string BuildHtml(string title, string code) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body {
            width: 1080px; height: 1920px;
            background: #0d1117;
            display: flex; flex-direction: column;
            align-items: center; justify-content: center;
            padding: 80px 60px;
            font-family: 'Segoe UI', system-ui, sans-serif;
          }
          .brand {
            color: #7c6af7;
            font-size: 32px;
            font-weight: 700;
            letter-spacing: 4px;
            text-transform: uppercase;
            margin-bottom: 48px;
          }
          .title {
            color: #e6edf3;
            font-size: 48px;
            font-weight: 700;
            text-align: center;
            margin-bottom: 56px;
            line-height: 1.3;
          }
          .code-block {
            background: #161b22;
            border: 1px solid #30363d;
            border-radius: 12px;
            padding: 48px;
            width: 100%;
          }
          pre {
            color: #e6edf3;
            font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
            font-size: 28px;
            line-height: 1.7;
            white-space: pre-wrap;
            word-break: break-word;
          }
        </style>
        </head>
        <body>
          <div class="brand">DevPulse</div>
          <div class="title">{{title}}</div>
          <div class="code-block">
            <pre>{{System.Web.HttpUtility.HtmlEncode(code)}}</pre>
          </div>
        </body>
        </html>
        """;
}
