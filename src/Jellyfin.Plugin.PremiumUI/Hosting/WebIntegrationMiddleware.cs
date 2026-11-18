using Jellyfin.Plugin.PremiumUI.Configuration;
using Jellyfin.Plugin.PremiumUI.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Serves the injected Jellyfin Web index and the plugin's embedded assets.
/// Runs before Jellyfin's compression and static files middleware.
/// </summary>
internal sealed class WebIntegrationMiddleware
{
    private const string IndexContentType = "text/html; charset=utf-8";
    private const string NoCache = "no-cache";
    private const string Immutable = "public, max-age=31536000, immutable";

    private readonly RequestDelegate _next;
    private readonly PremiumUiWebAssets _assets;
    private readonly IBaseUrlProvider _baseUrl;
    private readonly IntegrationStatus _status;
    private readonly ILogger<WebIntegrationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebIntegrationMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    /// <param name="assets">Embedded assets.</param>
    /// <param name="serverConfiguration">Server configuration (BaseUrl).</param>
    /// <param name="status">Shared diagnostics state.</param>
    /// <param name="logger">Logger.</param>
    public WebIntegrationMiddleware(
        RequestDelegate next,
        PremiumUiWebAssets assets,
        IBaseUrlProvider baseUrl,
        IntegrationStatus status,
        ILogger<WebIntegrationMiddleware> logger)
    {
        _next = next;
        _assets = assets;
        _baseUrl = baseUrl;
        _status = status;
        _logger = logger;
    }

    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    /// <returns>A task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await InvokeCoreAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _status.RecordError("middleware", ex.GetType().Name + ": " + ex.Message);
            _logger.LogError(ex, "Jellyfin Premium UI: error en el middleware de integración.");
            throw;
        }
    }

    private async Task InvokeCoreAsync(HttpContext context)
    {
        var configuration = PremiumUiPlugin.Instance?.Configuration;

        if (configuration is null || !configuration.General.Enabled || !_assets.IsComplete)
        {
            _status.InjectionActive = false;
            await _next(context).ConfigureAwait(false);
            return;
        }

        var method = context.Request.Method;
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var baseUrl = _baseUrl.GetBaseUrl();
        var fullPath = context.Request.PathBase.Add(context.Request.Path).Value ?? string.Empty;

        if (!TryStripBaseUrl(fullPath, baseUrl, out var localPath))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (IsIndexPath(localPath))
        {
            await HandleIndexAsync(context, localPath, baseUrl, configuration).ConfigureAwait(false);
            return;
        }

        if (TryGetAssetName(localPath, out var assetName))
        {
            await HandleAssetAsync(context, assetName).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private async Task HandleIndexAsync(
        HttpContext context,
        string localPath,
        string baseUrl,
        PluginConfiguration configuration)
    {
        if (localPath.Equals("/web", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = baseUrl
                + "/web/"
                + context.Request.QueryString.Value;
            return;
        }

        _status.RecordIndexRequest();

        if (!_assets.Index.TryGet(out var snapshot, out var error) || snapshot is null)
        {
            _status.RecordError("index", error ?? "Snapshot no disponible.");
            _status.InjectionActive = false;
            await _next(context).ConfigureAwait(false);
            return;
        }

        var inject = configuration.General.EnableHome || configuration.General.EnableDetails;

        if (!inject)
        {
            _status.InjectionActive = false;
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (IsNotModified(context, snapshot.ETag))
        {
            ApplyIndexHeaders(context, snapshot.ETag);
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        ApplyIndexHeaders(context, snapshot.ETag);
        context.Response.ContentLength = snapshot.Content.Length;

        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        _status.RecordInjection();
        _status.InjectionActive = true;
        await context.Response.Body.WriteAsync(snapshot.Content).ConfigureAwait(false);
    }

    private async Task HandleAssetAsync(HttpContext context, string assetName)
    {
        _status.RecordAssetRequest();

        if (!_assets.Catalog.TryGet(assetName, out var asset)
            && !_assets.Catalog.TryGetLogical(assetName, out asset))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.Headers.CacheControl = "no-store";
            return;
        }

        if (IsNotModified(context, asset.ETag))
        {
            ApplyAssetHeaders(context, asset, contentEncoding: null);
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        // Con Range se responde siempre en identidad: un 206 sobre una carga
        // comprimida tendría offsets incorrectos.
        var hasRange = context.Request.Headers.ContainsKey("Range");
        var encoding = hasRange
            ? null
            : EncodingNegotiation.Select(context.Request.Headers.AcceptEncoding);
        var payload = asset.Payload(encoding, out var contentEncoding);

        ApplyAssetHeaders(context, asset, contentEncoding);
        context.Response.Headers.AcceptRanges = "bytes";

        if (hasRange
            && contentEncoding is null
            && ByteRange.TryParse(context.Request.Headers.Range.ToString(), payload.Length, out var start, out var end))
        {
            var size = end - start + 1;

            context.Response.StatusCode = StatusCodes.Status206PartialContent;
            context.Response.ContentLength = size;
            context.Response.Headers.ContentRange = $"bytes {start}-{end}/{payload.Length}";

            if (HttpMethods.IsHead(context.Request.Method))
            {
                return;
            }

            await context.Response.Body
                .WriteAsync(payload.AsMemory((int)start, (int)size))
                .ConfigureAwait(false);
            return;
        }

        context.Response.ContentLength = payload.Length;

        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        await context.Response.Body.WriteAsync(payload).ConfigureAwait(false);
    }

    private void ApplyIndexHeaders(HttpContext context, string etag)
    {
        var headers = context.Response.Headers;
        headers.ContentType = IndexContentType;
        headers.CacheControl = NoCache;
        headers.ETag = etag;
        headers.Vary = "Accept-Encoding";
        headers["X-Premium-UI"] = "index";
    }

    private static void ApplyAssetHeaders(HttpContext context, AssetCatalog.Asset asset, string? contentEncoding)
    {
        var headers = context.Response.Headers;
        headers.ContentType = asset.ContentType;
        headers.CacheControl = Immutable;
        headers.ETag = asset.ETag;
        headers.Vary = "Accept-Encoding";
        headers["X-Premium-UI"] = "asset";

        if (contentEncoding is not null)
        {
            headers.ContentEncoding = new StringValues(contentEncoding);
        }
    }

    private static bool IsNotModified(HttpContext context, string etag)
    {
        if (!context.Request.Headers.TryGetValue("If-None-Match", out var values))
        {
            return false;
        }

        foreach (var raw in values)
        {
            if (string.IsNullOrEmpty(raw))
            {
                continue;
            }

            foreach (var candidate in raw.Split(','))
            {
                var value = candidate.Trim();
                if (value == "*" || value.Equals(etag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsIndexPath(string localPath)
        => localPath.Equals("/web", StringComparison.OrdinalIgnoreCase)
            || localPath.Equals("/web/", StringComparison.OrdinalIgnoreCase)
            || localPath.Equals("/web/index.html", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetAssetName(string localPath, out string name)
    {
        name = string.Empty;

        if (!localPath.StartsWith(PremiumUiWebAssets.PublicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var candidate = localPath[PremiumUiWebAssets.PublicPrefix.Length..];

        if (candidate.Length == 0 || candidate.Contains('\\') || candidate.Contains(".."))
        {
            return false;
        }

        // Los assets del catálogo se sirven con nombre hasheado (sin carpetas).
        // El arte de los estudios usa nombres estables y predecibles.
        if (candidate.StartsWith("studios/", StringComparison.OrdinalIgnoreCase))
        {
            var file = candidate["studios/".Length..];

            if (file.Length == 0 || file.Contains('/'))
            {
                return false;
            }

            name = "studios/" + file;
            return true;
        }

        if (candidate.Contains('/'))
        {
            return false;
        }

        name = candidate;
        return true;
    }

    private static bool TryStripBaseUrl(string fullPath, string baseUrl, out string localPath)
    {
        if (baseUrl.Length == 0)
        {
            localPath = fullPath;
            return true;
        }

        if (!fullPath.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            localPath = string.Empty;
            return false;
        }

        localPath = fullPath[baseUrl.Length..];
        return localPath.Length > 0;
    }
}
