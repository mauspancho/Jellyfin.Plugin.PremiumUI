using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// In-memory catalog of the plugin's embedded web assets, served with hashed names.
/// </summary>
internal sealed class AssetCatalog
{
    private readonly Dictionary<string, Asset> _assets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Asset> _logical = new(StringComparer.Ordinal);

    private AssetCatalog()
    {
    }

    /// <summary>
    /// Gets the public path of the main stylesheet.
    /// </summary>
    public string? StylesheetName { get; private set; }

    /// <summary>
    /// Gets the public path of the main script.
    /// </summary>
    public string? ScriptName { get; private set; }

    /// <summary>
    /// Gets the number of catalogued assets.
    /// </summary>
    public int Count => _assets.Count;

    /// <summary>
    /// Builds a catalog from embedded resources.
    /// </summary>
    /// <param name="assembly">Assembly that contains the resources.</param>
    /// <param name="resourcePrefix">Namespace prefix, including the trailing dot.</param>
    /// <returns>The catalog.</returns>
    public static AssetCatalog FromAssembly(Assembly assembly, string resourcePrefix)
    {
        var catalog = new AssetCatalog();

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(resourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var logicalName = resource[resourcePrefix.Length..];
            if (logicalName.Length == 0 || !logicalName.Contains('.'))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                continue;
            }

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var content = buffer.ToArray();
            if (content.Length == 0)
            {
                continue;
            }

            var extension = Path.GetExtension(logicalName);
            var contentType = GetContentType(extension);
            if (contentType is null)
            {
                continue;
            }

            var hash = Convert.ToHexString(SHA256.HashData(content))[..10].ToLowerInvariant();
            var publicName = string.Concat(
                Path.GetFileNameWithoutExtension(logicalName),
                ".",
                hash,
                extension);

            var asset = new Asset(
                publicName,
                contentType,
                content,
                Compress(content, "br"),
                Compress(content, "gzip"),
                Quote(hash));

            catalog._assets[publicName] = asset;
            catalog._logical[logicalName] = asset;

            if (logicalName.Equals("app.css", StringComparison.Ordinal))
            {
                catalog.StylesheetName = publicName;
            }
            else if (logicalName.Equals("app.js", StringComparison.Ordinal))
            {
                catalog.ScriptName = publicName;
            }
        }

        return catalog;
    }

    /// <summary>
    /// Looks up a served asset by its hashed name.
    /// </summary>
    /// <param name="name">File name requested under the premiumui prefix.</param>
    /// <param name="asset">Matching asset.</param>
    /// <returns>Whether the asset exists.</returns>
    public bool TryGet(string name, out Asset asset)
        => _assets.TryGetValue(name, out asset!);

    /// <summary>
    /// Looks up a served asset by its stable logical name (for example
    /// <c>studios/disney.webp</c>). Used for artwork that the frontend
    /// references without knowing the content hash.
    /// </summary>
    /// <param name="name">Logical resource name.</param>
    /// <param name="asset">Matching asset.</param>
    /// <returns>Whether the asset exists.</returns>
    public bool TryGetLogical(string name, out Asset asset)
    {
        if (_logical.TryGetValue(name, out asset!))
        {
            return true;
        }

        // MSBuild convierte las carpetas en puntos dentro del nombre del
        // recurso (`Web/studios/disney.webp` → `studios.disney.webp`), así que
        // se acepta también esa forma para los nombres con carpeta.
        if (name.Contains('/'))
        {
            return _logical.TryGetValue(name.Replace('/', '.'), out asset!);
        }

        return false;
    }

    private static byte[]? Compress(byte[] content, string encoding)
    {
        // Imágenes y vídeos ya vienen comprimidos: recomprimirlos gastaría CPU
        // al arrancar y memoria sin reducir el tamaño de forma apreciable.
        if (content.Length < 256 || !IsCompressible(content))
        {
            return null;
        }

        using var output = new MemoryStream();
        if (encoding == "br")
        {
            using var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true);
            brotli.Write(content);
        }
        else
        {
            using var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true);
            gzip.Write(content);
        }

        return output.ToArray();
    }

    private static string? GetContentType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".woff2" => "font/woff2",
            _ => null
        };

    private static bool IsCompressible(byte[] content)
    {
        // WebP: contenedor RIFF; MP4: caja `ftyp` al principio.
        if (content.Length >= 12
            && content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F')
        {
            return false;
        }

        if (content.Length >= 8
            && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47)
        {
            return false;
        }

        return !(content.Length >= 12
            && content[0] == 0x00 && content[1] == 0x00 && content[2] == 0x00
            && content[4] == (byte)'f' && content[5] == (byte)'t' && content[6] == (byte)'y' && content[7] == (byte)'p');
    }

    private static string Quote(string hash)
        => string.Create(CultureInfo.InvariantCulture, $"\"{hash}\"");

    /// <summary>
    /// A single precompressed asset.
    /// </summary>
    /// <param name="Name">Public hashed file name.</param>
    /// <param name="ContentType">MIME type.</param>
    /// <param name="Identity">Uncompressed bytes.</param>
    /// <param name="Brotli">Brotli bytes, if computed.</param>
    /// <param name="Gzip">Gzip bytes, if computed.</param>
    /// <param name="ETag">Quoted strong ETag.</param>
    internal sealed record Asset(
        string Name,
        string ContentType,
        byte[] Identity,
        byte[]? Brotli,
        byte[]? Gzip,
        string ETag)
    {
        /// <summary>
        /// Returns the payload for the requested encoding.
        /// </summary>
        /// <param name="encoding">Selected encoding.</param>
        /// <param name="contentEncoding">Header value to send.</param>
        /// <returns>Payload bytes.</returns>
        public byte[] Payload(string? encoding, out string? contentEncoding)
        {
            if (encoding == "br" && Brotli is not null)
            {
                contentEncoding = "br";
                return Brotli;
            }

            if (encoding == "gzip" && Gzip is not null)
            {
                contentEncoding = "gzip";
                return Gzip;
            }

            contentEncoding = null;
            return Identity;
        }
    }
}
