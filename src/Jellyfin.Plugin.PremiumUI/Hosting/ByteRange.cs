using System.Globalization;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Parses single <c>Range</c> requests for embedded assets (mainly video).
/// Multi-range and malformed headers are ignored so the full asset is served.
/// </summary>
internal static class ByteRange
{
    /// <summary>
    /// Tries to parse a single byte range from a raw <c>Range</c> header.
    /// </summary>
    /// <param name="header">Raw header value, for example <c>bytes=0-1023</c>.</param>
    /// <param name="length">Full asset length.</param>
    /// <param name="start">Inclusive start offset.</param>
    /// <param name="end">Inclusive end offset.</param>
    /// <returns>Whether a satisfiable single range was parsed.</returns>
    public static bool TryParse(string? header, long length, out long start, out long end)
    {
        start = 0;
        end = length - 1;

        if (string.IsNullOrWhiteSpace(header) || length <= 0)
        {
            return false;
        }

        var value = header.Trim();

        if (!value.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var spec = value["bytes=".Length..].Trim();

        // Los clientes pueden pedir varios rangos; se sirve el recurso completo.
        if (spec.Length == 0 || spec.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        var dash = spec.IndexOf('-', StringComparison.Ordinal);

        if (dash < 0)
        {
            return false;
        }

        var left = spec[..dash].Trim();
        var right = spec[(dash + 1)..].Trim();

        if (left.Length == 0)
        {
            // Sufijo: `bytes=-500` son los últimos 500 bytes.
            if (!TryParseOffset(right, out var suffix) || suffix <= 0)
            {
                return false;
            }

            suffix = Math.Min(suffix, length);
            start = length - suffix;
            end = length - 1;
            return true;
        }

        if (!TryParseOffset(left, out var rawStart))
        {
            return false;
        }

        if (rawStart >= length)
        {
            return false;
        }

        start = rawStart;

        if (right.Length == 0)
        {
            end = length - 1;
            return true;
        }

        if (!TryParseOffset(right, out var rawEnd) || rawEnd < rawStart)
        {
            return false;
        }

        end = Math.Min(rawEnd, length - 1);
        return true;
    }

    private static bool TryParseOffset(string value, out long offset)
        => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out offset);
}
