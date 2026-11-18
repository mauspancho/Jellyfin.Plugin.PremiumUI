using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Reads Jellyfin's <c>index.html</c>, caches it and injects the Premium UI block in memory.
/// The file on disk is never modified.
/// </summary>
internal sealed class IndexHtmlProvider
{
    private static readonly UTF8Encoding _utf8 = new(encoderShouldEmitUTF8Identifier: false);
    private readonly string _indexPath;
    private readonly string _payload;
    private readonly string _etagSeed;
    private readonly object _sync = new();
    private IndexSnapshot? _snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexHtmlProvider"/> class.
    /// </summary>
    /// <param name="webPath">Jellyfin web root.</param>
    /// <param name="payload">HTML block to inject before <c>&lt;/head&gt;</c>.</param>
    /// <param name="seed">Value that invalidates the ETag when the plugin changes.</param>
    public IndexHtmlProvider(string webPath, string payload, string seed)
    {
        _indexPath = Path.Combine(webPath, "index.html");
        _payload = payload;
        _etagSeed = seed;
    }

    /// <summary>
    /// Gets the number of times the index file was read.
    /// </summary>
    public long Reads { get; private set; }

    /// <summary>
    /// Returns the injected index, rebuilding the cache when the file changes.
    /// </summary>
    /// <param name="snapshot">Current snapshot.</param>
    /// <param name="error">Failure reason, when the method returns <c>false</c>.</param>
    /// <returns>Whether a snapshot is available.</returns>
    public bool TryGet(out IndexSnapshot? snapshot, out string? error)
    {
        snapshot = null;
        error = null;

        FileInfo info;
        try
        {
            info = new FileInfo(_indexPath);
            if (!info.Exists)
            {
                error = "No se encontró index.html en el directorio web de Jellyfin.";
                return false;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = "No se pudo leer index.html: " + ex.Message;
            return false;
        }

        var stamp = info.LastWriteTimeUtc;
        var length = info.Length;

        lock (_sync)
        {
            if (_snapshot is { } cached && cached.LastWriteUtc == stamp && cached.Length == length)
            {
                snapshot = cached;
                return true;
            }

            try
            {
                var bytes = File.ReadAllBytes(_indexPath);
                Reads++;

                var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                var text = _utf8.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));

                var headIndex = text.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
                if (headIndex < 0)
                {
                    error = "index.html no contiene </head>; no se inyecta el bloque.";
                    return false;
                }

                var injected = string.Concat(
                    text.AsSpan(0, headIndex),
                    _payload,
                    text.AsSpan(headIndex));

                var output = _utf8.GetBytes(injected);
                if (hasBom)
                {
                    output = [0xEF, 0xBB, 0xBF, .. output];
                }

                var etag = BuildETag(output);

                _snapshot = new IndexSnapshot(output, etag, stamp, length);
                snapshot = _snapshot;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = "No se pudo leer index.html: " + ex.Message;
                return false;
            }
        }
    }

    private string BuildETag(byte[] content)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Create(
                CultureInfo.InvariantCulture,
                $"{_etagSeed}|{_payload}|{Convert.ToHexString(SHA256.HashData(content))}")));

        return string.Create(CultureInfo.InvariantCulture, $"\"{Convert.ToHexString(hash)[..16].ToLowerInvariant()}\"");
    }

    /// <summary>
    /// Cached injected document.
    /// </summary>
    /// <param name="Content">UTF-8 bytes.</param>
    /// <param name="ETag">Quoted strong ETag.</param>
    /// <param name="LastWriteUtc">Source file timestamp.</param>
    /// <param name="Length">Source file length.</param>
    internal sealed record IndexSnapshot(
        byte[] Content,
        string ETag,
        DateTime LastWriteUtc,
        long Length);
}
