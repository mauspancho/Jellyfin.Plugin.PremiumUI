using Jellyfin.Plugin.PremiumUI.Diagnostics;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Registers the Premium UI middleware before the Jellyfin web pipeline.
/// Phase 1 only records startup state; Phase 2 adds index injection and asset serving.
/// </summary>
public sealed class PremiumUiStartupFilter : IStartupFilter
{
    /// <summary>
    /// Major Jellyfin version supported by this build.
    /// </summary>
    public const int SupportedMajorVersion = 12;

    private readonly IServerApplicationHost _applicationHost;
    private readonly IntegrationStatus _status;
    private readonly ILogger<PremiumUiStartupFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PremiumUiStartupFilter"/> class.
    /// </summary>
    /// <param name="applicationHost">Jellyfin application host.</param>
    /// <param name="status">Shared integration status.</param>
    /// <param name="logger">Logger.</param>
    public PremiumUiStartupFilter(
        IServerApplicationHost applicationHost,
        IntegrationStatus status,
        ILogger<PremiumUiStartupFilter> logger)
    {
        _applicationHost = applicationHost;
        _status = status;
        _logger = logger;
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        var version = _applicationHost.ApplicationVersion;
        var supported = version.Major == SupportedMajorVersion;

        _status.MarkStartup(
            version.ToString(),
            supported,
            supported
                ? "Middleware registrado. Inyección en memoria activa."
                : $"Servidor {version} no soportado. El plugin quedará pasivo.");

        if (supported)
        {
            _logger.LogInformation(
                "Jellyfin Premium UI: startup filter registrado para Jellyfin {Version}.",
                version);
        }
        else
        {
            _logger.LogWarning(
                "Jellyfin Premium UI: versión {Version} no soportada (se esperaba major {Major}). El plugin no modificará la interfaz.",
                version,
                SupportedMajorVersion);
        }

        return app =>
        {
            if (supported)
            {
                app.UseMiddleware<WebIntegrationMiddleware>();
            }

            next(app);
        };
    }
}
