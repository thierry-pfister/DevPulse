using System.Text.RegularExpressions;

namespace DevPulse.Infrastructure.Publishers;

public static class SlugGenerator
{
    public static string From(string title) =>
        Regex.Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
}
