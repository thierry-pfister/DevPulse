using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class VideoAssemblyService(ILogger<VideoAssemblyService> logger) : IVideoAssemblyService
{
    public async Task<byte[]?> AssembleFromSlidesAsync(
        IReadOnlyList<(byte[] Image, double DurationSeconds)> slides,
        byte[] audioBytes)
    {
        if (slides.Count == 0) return null;

        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var audioPath  = Path.Combine(tmpDir, "audio.mp3");
            var outputPath = Path.Combine(tmpDir, "short.mp4");

            await File.WriteAllBytesAsync(audioPath, audioBytes);
            for (var i = 0; i < slides.Count; i++)
                await File.WriteAllBytesAsync(Path.Combine(tmpDir, $"slide{i}.png"), slides[i].Image);

            var args = BuildArgs(slides, tmpDir, audioPath, outputPath);
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

    private static string BuildArgs(
        IReadOnlyList<(byte[] Image, double DurationSeconds)> slides,
        string tmpDir, string audioPath, string outputPath)
    {
        var inputs = new StringBuilder("-y ");
        for (var i = 0; i < slides.Count; i++)
            inputs.Append($"-loop 1 -t {slides[i].DurationSeconds:F1} -i \"{Path.Combine(tmpDir, $"slide{i}.png")}\" ");
        inputs.Append($"-i \"{audioPath}\"");

        var filter    = BuildFilterComplex(slides);
        var audioIdx  = slides.Count;

        return $"{inputs} -filter_complex \"{filter}\" " +
               $"-map \"[vout]\" -map {audioIdx}:a " +
               $"-c:v libx264 -pix_fmt yuv420p -c:a aac -b:a 192k -shortest \"{outputPath}\"";
    }

    internal static string BuildFilterComplex(IReadOnlyList<(byte[] Image, double DurationSeconds)> slides)
    {
        const double Td = 0.3;
        var n  = slides.Count;
        var sb = new StringBuilder();

        if (n == 1)
            return "[0:v]fps=30,scale=1080:1920[vout]";

        for (var i = 0; i < n; i++)
            sb.Append($"[{i}:v]fps=30,scale=1080:1920[v{i}];");

        double cumulative = 0;
        for (var i = 0; i < n - 1; i++)
        {
            cumulative += slides[i].DurationSeconds;
            var offset = cumulative - (i + 1) * Td;
            var inA    = i == 0 ? "[v0]" : $"[x{i}]";
            var outV   = i == n - 2 ? "[vout]" : $"[x{i + 1}]";
            sb.Append($"{inA}[v{i + 1}]xfade=transition=fade:duration={Td:F2}:offset={offset:F2}{outV};");
        }

        return sb.ToString().TrimEnd(';');
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
