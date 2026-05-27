using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class VideoAssemblyService(ILogger<VideoAssemblyService> logger) : IVideoAssemblyService
{
    public async Task<byte[]?> AssembleAsync(byte[] imageBytes, byte[] audioBytes)
    {
        var tmpDir  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);

        var imagePath  = Path.Combine(tmpDir, "frame.png");
        var audioPath  = Path.Combine(tmpDir, "audio.mp3");
        var outputPath = Path.Combine(tmpDir, "short.mp4");

        try
        {
            await File.WriteAllBytesAsync(imagePath, imageBytes);
            await File.WriteAllBytesAsync(audioPath, audioBytes);

            var args = $"-y -loop 1 -i \"{imagePath}\" -i \"{audioPath}\" " +
                       "-c:v libx264 -tune stillimage -c:a aac -b:a 192k " +
                       "-pix_fmt yuv420p -shortest \"{outputPath}\"";

            var result = await RunFfmpegAsync(args.Replace("\"{outputPath}\"", $"\"{outputPath}\""));
            if (!result) return null;

            return await File.ReadAllBytesAsync(outputPath);
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

    private async Task<bool> RunFfmpegAsync(string args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = "ffmpeg",
                Arguments              = args,
                RedirectStandardError  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            }
        };

        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            logger.LogWarning("FFmpeg exited with code {Code}: {Stderr}", process.ExitCode, stderr);

        return process.ExitCode == 0;
    }
}
