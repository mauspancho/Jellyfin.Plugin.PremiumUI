using System.IO.Compression;
using System.Text;
using Jellyfin.Plugin.PremiumUI.Configuration;
using Jellyfin.Plugin.PremiumUI.Diagnostics;
using Jellyfin.Plugin.PremiumUI.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class WebIntegrationMiddlewareTests : IDisposable
{
    private readonly string _root;
    private readonly string _webPath;
    private readonly PremiumUiWebAssets _assets;
    private readonly IntegrationStatus _status;
    private readonly PremiumUiPlugin _plugin;

    public WebIntegrationMiddlewareTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "premiumui-mw-" + Guid.NewGuid().ToString("N"));
        _webPath = Path.Combine(_root, "web");
        Directory.CreateDirectory(_webPath);

        File.WriteAllText(
            Path.Combine(_webPath, "index.html"),
            "<!DOCTYPE html><html><head><title>Jellyfin</title></head><body><div id=\"app\"></div></body></html>");

        var paths = new TestApplicationPaths(_root);
        _status = new IntegrationStatus();
        _status.MarkStartup("12.4.0.0", supported: true, "test");
        _assets = new PremiumUiWebAssets(paths, _status, NullLogger<PremiumUiWebAssets>.Instance);
        _plugin = new PremiumUiPlugin(paths, new TestXmlSerializer());
        _plugin.UpdateConfiguration(new PluginConfiguration());
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task Index_IsInjectedAndCacheable()
    {
        var context = await InvokeAsync("/web/");

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
        Assert.Equal("no-cache", context.Response.Headers.CacheControl);
        Assert.False(string.IsNullOrEmpty(context.Response.Headers.ETag));
        Assert.Equal(context.Response.ContentLength, (long)ReadBody(context).Length);
        Assert.Contains("premiumui:start", ReadBody(context));
        Assert.Contains("type=\"module\"", ReadBody(context));
        Assert.True(_status.InjectionActive);
        Assert.True(_status.Injections > 0);
    }

    [Fact]
    public async Task Index_Returns304WhenETagMatches()
    {
        var first = await InvokeAsync("/web/");
        var etag = first.Response.Headers.ETag.ToString();

        var second = await InvokeAsync("/web/", ifNoneMatch: etag);

        Assert.Equal(StatusCodes.Status304NotModified, second.Response.StatusCode);
        Assert.Equal(etag, second.Response.Headers.ETag.ToString());
        Assert.Empty(ReadBody(second));
    }

    [Fact]
    public async Task Index_HeadReturnsHeadersWithoutBody()
    {
        var context = await InvokeAsync("/web/", method: "HEAD");

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(context.Response.ContentLength > 0);
        Assert.Empty(ReadBody(context));
    }

    [Fact]
    public async Task WebWithoutSlash_RedirectsPreservingQuery()
    {
        var context = await InvokeAsync("/web?foo=bar");

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/web/?foo=bar", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task IndexHtmlPath_IsAlsoInjected()
    {
        var context = await InvokeAsync("/web/index.html");

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("premiumui:start", ReadBody(context));
    }

    [Fact]
    public async Task Script_IsServedCompressedWhenAccepted()
    {
        var context = await InvokeAsync("/web/premiumui/" + _assets.Catalog.ScriptName, acceptEncoding: "gzip");

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("gzip", context.Response.Headers.ContentEncoding.ToString());
        Assert.Equal("text/javascript; charset=utf-8", context.Response.ContentType);

        using var input = new MemoryStream(ReadBytes(context));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        Assert.Contains("PremiumUI", reader.ReadToEnd());
    }

    [Fact]
    public async Task Script_SupportsBrotliAndETag()
    {
        var first = await InvokeAsync("/web/premiumui/" + _assets.Catalog.ScriptName, acceptEncoding: "br");
        Assert.Equal("br", first.Response.Headers.ContentEncoding.ToString());

        var etag = first.Response.Headers.ETag.ToString();
        var second = await InvokeAsync(
            "/web/premiumui/" + _assets.Catalog.ScriptName,
            acceptEncoding: "br",
            ifNoneMatch: etag);

        Assert.Equal(StatusCodes.Status304NotModified, second.Response.StatusCode);
        Assert.Empty(ReadBody(second));
    }

    [Fact]
    public async Task Script_IdentityWhenNoEncodingAccepted()
    {
        var context = await InvokeAsync("/web/premiumui/" + _assets.Catalog.ScriptName, acceptEncoding: "deflate");

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(string.IsNullOrEmpty(context.Response.Headers.ContentEncoding));
        Assert.Contains("PremiumUI", ReadBody(context));
    }

    [Fact]
    public async Task UnknownAsset_Returns404()
    {
        var context = await InvokeAsync("/web/premiumui/missing.js");

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
    }

    [Theory]
    [InlineData("/web/premiumui/../app.js")]
    [InlineData("/web/premiumui/")]
    [InlineData("/web/premiumui")]
    public async Task InvalidAssetPath_PassesThrough(string path)
    {
        var context = await InvokeAsync(path);

        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/web/app.js")]
    [InlineData("/Items/abc")]
    [InlineData("/")]
    public async Task OtherPaths_PassThrough(string path)
    {
        var context = await InvokeAsync(path);

        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
    }

    [Fact]
    public async Task BaseUrl_IsRespected()
    {
        var context = await InvokeAsync("/jf/web/", baseUrl: "/jf");
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("premiumui:start", ReadBody(context));

        var outside = await InvokeAsync("/web/", baseUrl: "/jf");
        Assert.Equal(StatusCodes.Status418ImATeapot, outside.Response.StatusCode);
    }

    [Fact]
    public async Task DisabledPlugin_PassesThrough()
    {
        var config = new PluginConfiguration();
        config.General.Enabled = false;
        _plugin.UpdateConfiguration(config);

        var context = await InvokeAsync("/web/");

        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
        Assert.False(_status.InjectionActive);
    }

    [Fact]
    public async Task BothScreensDisabled_PassesThrough()
    {
        var config = new PluginConfiguration();
        config.General.EnableHome = false;
        config.General.EnableDetails = false;
        _plugin.UpdateConfiguration(config);

        var context = await InvokeAsync("/web/");

        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
        Assert.False(_status.InjectionActive);
    }

    private async Task<DefaultHttpContext> InvokeAsync(
        string path,
        string method = "GET",
        string? acceptEncoding = null,
        string? ifNoneMatch = null,
        string? baseUrl = null)
    {
        var middleware = new WebIntegrationMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                return Task.CompletedTask;
            },
            _assets,
            new FakeBaseUrlProvider(baseUrl ?? string.Empty),
            _status,
            NullLogger<WebIntegrationMiddleware>.Instance);

        var separator = path.IndexOf('?', StringComparison.Ordinal);
        var rawPath = separator >= 0 ? path[..separator] : path;
        var rawQuery = separator >= 0 ? path[separator..] : string.Empty;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = rawPath;
        httpContext.Request.QueryString = new QueryString(rawQuery);
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 8096);
        httpContext.Response.Body = new MemoryStream();

        if (acceptEncoding is not null)
        {
            httpContext.Request.Headers.AcceptEncoding = acceptEncoding;
        }

        if (ifNoneMatch is not null)
        {
            httpContext.Request.Headers.IfNoneMatch = ifNoneMatch;
        }

        await middleware.InvokeAsync(httpContext);

        return httpContext;
    }

    private static byte[] ReadBytes(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var buffer = new MemoryStream();
        context.Response.Body.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd();
    }

    [Fact]
    public void Middleware_IsCreatableThroughDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_status);
        services.AddSingleton(_assets);
        services.AddSingleton<IBaseUrlProvider>(new FakeBaseUrlProvider(string.Empty));

        using var provider = services.BuildServiceProvider();
        var middleware = ActivatorUtilities.CreateInstance<WebIntegrationMiddleware>(
            provider,
            (RequestDelegate)(_ => Task.CompletedTask));

        Assert.NotNull(middleware);
    }

    private sealed class FakeBaseUrlProvider : IBaseUrlProvider
    {
        private readonly string _baseUrl;

        public FakeBaseUrlProvider(string baseUrl) => _baseUrl = baseUrl;

        public string GetBaseUrl() => _baseUrl;
    }
}
