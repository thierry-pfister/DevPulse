using System.Text.RegularExpressions;

namespace DevPulse.Infrastructure.YouTube;

internal static class YouTubeDescriptionCleaner
{
    internal static string Clean(string description)
    {
        var clean = Regex.Replace(description, @"https?://\S+", "");
        clean = Regex.Replace(clean, @"[<>]", "");
        clean = Regex.Replace(clean, @"[^ -~\n]", "");
        clean = clean.Trim();
        if (clean.Length > 4500) clean = clean[..4500];
        return $"{clean}\n\n#Shorts";
    }
}
