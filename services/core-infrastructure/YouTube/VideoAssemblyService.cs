using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class VideoAssemblyService(ILogger<VideoAssemblyService> logger) : IVideoAssemblyService
{
    public async Task<byte[]?> AssembleFromSlidesAsync(
        IReadOnlyList<(byte[] Image, double DurationSeconds)> slides,
        byte[] audioBytes)
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var audioPath  = Path.Combine(tmpDir, "audio.mp3");
            var concatPath = Path.Combine(tmpDir, "slides.txt");
            var outputPath = Path.Combine(tmpDir, "short.mp4");

            await File.WriteAllBytesAsync(audioPath, audioBytes);
            await WriteConcatFile(concatPath, tmpDir, slides);

            var args = $"-y -f concat -safe 0 -i \"{concatPath}\" -i \"{audioPath}\" " +
                       $"-c:v libx264 -pix_fmt yuv420p -c:a aac -b:a 192k -shortest \"{outputPath}\"";

            return await RunFfmpegAsync(args)
                ? await File.ReadAllBytesAsync(outputPath)
                : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Video assembly failed");
            return null;
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    private static async Task WriteConcatFile(
        string concatPath,
        string tmpDir,
        IReadOnlyList<(byte[] Image, double DurationSeconds)> slides)
    {
        var lines = new List<string> { "ffconcat version 1.0" };
        for (var i = 0; i < slides.Count; i++)
        {
            var imgPath = Path.Combine(tmpDir, $"slide{i}.png");
            await File.WriteAllBytesAsync(imgPath, slides[i].Image);
            lines.Add($"file '{imgPath}'");
            lines.Add($"duration {slides[i].DurationSeconds:F1}");
        }
        // ffconcat requires repeating the last file without a duration
        var last = Path.Combine(tmpDir, $"slide{slides.Count - 1}.png");
        lines.Add($"file '{last}'");
        await File.WriteAllTextAsync(concatPath, string.Join('\n', lines));
    }

    private async Task<bool> RunFfmpegAsync(string args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName              = "ffmpeg",
                Arguments             = args,
                RedirectStandardError = true,
                UseShellExecute       = false,
                CreateNoWindow        = true,
            }
        };

        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            logger.LogWarning("FFmpeg exited {Code}: {Stderr}", process.ExitCode, stderr);

        return process.ExitCode == 0;
    }
}
