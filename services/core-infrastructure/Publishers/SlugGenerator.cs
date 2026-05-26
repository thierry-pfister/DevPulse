using System.Text.RegularExpressions;

namespace DevPulse.Infrastructure.Publishers;

internal static class SlugGenerator
{
    internal static string From(string title) =>
        Regex.Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
}
