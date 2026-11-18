using System.Globalization;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Minimal <c>Accept-Encoding</c> negotiation for precompressed assets.
/// </summary>
internal static class EncodingNegotiation
{
    /// <summary>
    /// Chooses the best supported content encoding.
    /// </summary>
    /// <param name="acceptEncoding">Raw <c>Accept-Encoding</c> header.</param>
    /// <returns><c>br</c>, <c>gzip</c> or <c>null</c> for identity.</returns>
    public static string? Select(string? acceptEncoding)
    {
        if (string.IsNullOrWhiteSpace(acceptEncoding))
        {
            return null;
        }

        double brotli = 0;
        double gzip = 0;

        foreach (var entry in acceptEncoding.Split(','))
        {
            var segment = entry.Trim();
            if (segment.Length == 0)
            {
                continue;
            }

            var parts = segment.Split(';');
            var token = parts[0].Trim();
            var quality = 1d;

            for (var i = 1; i < parts.Length; i++)
            {
                var parameter = parts[i].Trim();
                if (parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(parameter[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    quality = Math.Clamp(parsed, 0, 1);
                }
            }

            if (token.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                brotli = quality;
            }
            else if (token.Equals("gzip", StringComparison.OrdinalIgnoreCase))
            {
                gzip = quality;
            }
            else if (token == "*")
            {
                brotli = Math.Max(brotli, quality);
                gzip = Math.Max(gzip, quality);
            }
        }

        if (brotli > 0 && brotli >= gzip)
        {
            return "br";
        }

        return gzip > 0 ? "gzip" : null;
    }
}
