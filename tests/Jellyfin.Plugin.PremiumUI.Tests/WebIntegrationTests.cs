using System.Text;
using Jellyfin.Plugin.PremiumUI.Diagnostics;
using Jellyfin.Plugin.PremiumUI.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class WebIntegrationTests : IDisposable
{
    private readonly string _webPath;

    public WebIntegrationTests()
    {
        _webPath = Path.Combine(Path.GetTempPath(), "premiumui-web-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webPath);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_webPath, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Theory]
    [InlineData("br", "br")]
    [InlineData("gzip", "gzip")]
    [InlineData("gzip, br", "br")]
    [InlineData("br;q=0.5, gzip;q=0.9", "gzip")]
    [InlineData("gzip;q=0, br;q=0", null)]
    [InlineData("*", "br")]
    [InlineData("deflate", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void EncodingNegotiation_SelectsBestEncoding(string? header, string? expected)
    {
        Assert.Equal(expected, EncodingNegotiation.Select(header));
    }

    [Fact]
    public void AssetCatalog_IndexesEmbeddedWebAssets()
    {
        var catalog = AssetCatalog.FromAssembly(
            typeof(PremiumUiWebAssets).Assembly,
            PremiumUiWebAssets.ResourcePrefix);

        Assert.True(catalog.Count >= 26, "app.js, app.css y el arte de los estudios deben estar embebidos");
        Assert.NotNull(catalog.ScriptName);
        Assert.NotNull(catalog.StylesheetName);
        Assert.Matches("^app\\.[0-9a-f]{10}\\.js$", catalog.ScriptName);
        Assert.Matches("^app\\.[0-9a-f]{10}\\.css$", catalog.StylesheetName);

        Assert.True(catalog.TryGet(catalog.ScriptName!, out var script));
        Assert.Equal("text/javascript; charset=utf-8", script.ContentType);
        Assert.StartsWith("\"", script.ETag);
        Assert.True(script.Brotli!.Length < script.Identity.Length);
        Assert.True(script.Gzip!.Length < script.Identity.Length);

        Assert.False(catalog.TryGet("app.js", out _));
        Assert.False(catalog.TryGet("../../etc/passwd", out _));

        // Arte estable de los estudios: se sirve por nombre lógico, sin hash.
        Assert.True(catalog.TryGetLogical("studios/disney.webp", out var logo));
        Assert.Equal("image/webp", logo.ContentType);
        Assert.Null(logo.Brotli);
        Assert.Null(logo.Gzip);
        Assert.True(logo.Identity.Length > 0);

        Assert.True(catalog.TryGetLogical("studios/disney.mp4", out var video));
        Assert.Equal("video/mp4", video.ContentType);
        Assert.Null(video.Brotli);

        Assert.False(catalog.TryGetLogical("studios/desconocido.webp", out _));
        Assert.False(catalog.TryGetLogical("studios/../../secret", out _));
    }

    [Fact]
    public void AssetPayload_NegotiatesEncoding()
    {
        var catalog = AssetCatalog.FromAssembly(
            typeof(PremiumUiWebAssets).Assembly,
            PremiumUiWebAssets.ResourcePrefix);
        Assert.True(catalog.TryGet(catalog.ScriptName!, out var asset));

        var brotli = asset.Payload("br", out var brHeader);
        Assert.Equal("br", brHeader);
        Assert.Same(asset.Brotli, brotli);

        var identity = asset.Payload(null, out var identityHeader);
        Assert.Null(identityHeader);
        Assert.Same(asset.Identity, identity);
    }

    [Fact]
    public void IndexProvider_InjectsPayloadBeforeHeadEnd()
    {
        var original = "<!DOCTYPE html><html><head><title>x</title></head><body>hi</body></html>";
        File.WriteAllText(Path.Combine(_webPath, "index.html"), original);

        var provider = new IndexHtmlProvider(_webPath, "<!-- premiumui:start --><script src=\"a.js\"></script>", "1.0.0.0");
        Assert.True(provider.TryGet(out var snapshot, out var error));
        Assert.Null(error);
        Assert.NotNull(snapshot);

        var html = Encoding.UTF8.GetString(snapshot!.Content);
        Assert.Contains("premiumui:start", html);
        Assert.True(html.IndexOf("premiumui:start", StringComparison.Ordinal) < html.IndexOf("</head>", StringComparison.Ordinal));
        Assert.Equal(1, provider.Reads);
    }

    [Fact]
    public void IndexProvider_ReusesCacheUntilFileChanges()
    {
        var path = Path.Combine(_webPath, "index.html");
        File.WriteAllText(path, "<html><head></head><body>1</body></html>");

        var provider = new IndexHtmlProvider(_webPath, "<!-- p -->", "1.0.0.0");
        Assert.True(provider.TryGet(out var first, out _));
        Assert.True(provider.TryGet(out var second, out _));

        Assert.Same(first, second);
        Assert.Equal(1, provider.Reads);

        File.WriteAllText(path, "<html><head></head><body>2</body></html>");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(5));

        Assert.True(provider.TryGet(out var third, out _));
        Assert.Equal(2, provider.Reads);
        Assert.NotEqual(first!.ETag, third!.ETag);
    }

    [Fact]
    public void IndexProvider_ReportsMissingFile()
    {
        var provider = new IndexHtmlProvider(_webPath, "<!-- p -->", "1.0.0.0");

        Assert.False(provider.TryGet(out var snapshot, out var error));
        Assert.Null(snapshot);
        Assert.NotNull(error);
    }

    [Fact]
    public void IndexProvider_ReportsMissingHead()
    {
        File.WriteAllText(Path.Combine(_webPath, "index.html"), "<html><body>no head</body></html>");

        var provider = new IndexHtmlProvider(_webPath, "<!-- p -->", "1.0.0.0");

        Assert.False(provider.TryGet(out _, out var error));
        Assert.Contains("</head>", error);
    }

    [Fact]
    public void IndexProvider_KeepsUtf8Bom()
    {
        var path = Path.Combine(_webPath, "index.html");
        File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("<html><head></head><body>á</body></html>")]);

        var provider = new IndexHtmlProvider(_webPath, "<!-- p -->", "1.0.0.0");
        Assert.True(provider.TryGet(out var snapshot, out _));

        Assert.Equal(0xEF, snapshot!.Content[0]);
        Assert.Equal(0xBB, snapshot.Content[1]);
        Assert.Equal(0xBF, snapshot.Content[2]);
    }

    [Fact]
    public void WebAssets_BuildsInjectionPayloadWithHashedNames()
    {
        var paths = new TestApplicationPaths(_webPath);
        var assets = new PremiumUiWebAssets(
            paths,
            new IntegrationStatus(),
            NullLogger<PremiumUiWebAssets>.Instance);

        Assert.True(assets.IsComplete);
        Assert.Contains("premiumui:start", assets.Payload);
        Assert.Contains("./premiumui/", assets.Payload);
        Assert.Contains(assets.Catalog.ScriptName!, assets.Payload);
        Assert.Contains(assets.Catalog.StylesheetName!, assets.Payload);
        Assert.Contains("type=\"module\"", assets.Payload);
    }

    [Fact]
    public void RecordError_CollapsesConsecutiveDuplicates()
    {
        var status = new IntegrationStatus();

        status.RecordError("index", "missing");
        status.RecordError("index", "missing");
        status.RecordError("index", "other");

        Assert.Equal(2, status.GetErrors().Count);
    }

    [Theory]
    [InlineData("bytes=0-99", 1000, 0, 99)]
    [InlineData("bytes=100-", 1000, 100, 999)]
    [InlineData("bytes=-250", 1000, 750, 999)]
    [InlineData("bytes=900-5000", 1000, 900, 999)]
    [InlineData(" Bytes=5-5 ", 1000, 5, 5)]
    public void ByteRange_ParsesSingleRanges(string header, long length, long start, long end)
    {
        Assert.True(ByteRange.TryParse(header, length, out var actualStart, out var actualEnd));
        Assert.Equal(start, actualStart);
        Assert.Equal(end, actualEnd);
    }

    [Theory]
    [InlineData(null, 1000)]
    [InlineData("", 1000)]
    [InlineData("items=0-10", 1000)]
    [InlineData("bytes=", 1000)]
    [InlineData("bytes=abc", 1000)]
    [InlineData("bytes=0-10,20-30", 1000)]
    [InlineData("bytes=10-5", 1000)]
    [InlineData("bytes=-0", 1000)]
    [InlineData("bytes=2000-", 1000)]
    [InlineData("bytes=0-10", 0)]
    public void ByteRange_RejectsUnsatisfiableOrComplexRanges(string? header, long length)
        => Assert.False(ByteRange.TryParse(header, length, out _, out _));

    [Fact]
    public void IntegrationStatus_TracksTrafficCounters()
    {
        var status = new IntegrationStatus();

        status.RecordIndexRequest();
        status.RecordIndexRequest();
        status.RecordAssetRequest();
        status.RecordInjection();
        status.RecordBootstrap();
        status.RecordBootstrap();

        Assert.Equal(2, status.IndexRequests);
        Assert.Equal(1, status.AssetRequests);
        Assert.Equal(1, status.Injections);
        Assert.Equal(2, status.BootstrapRequests);
        Assert.NotNull(status.LastInjectionUtc);
    }
}
