using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Supplies the configured <c>BaseUrl</c> so the middleware can match paths before <c>app.Map</c>.
/// </summary>
internal interface IBaseUrlProvider
{
    /// <summary>
    /// Returns the configured base URL without a trailing slash, or an empty string.
    /// </summary>
    /// <returns>Base URL.</returns>
    string GetBaseUrl();
}

/// <summary>
/// Reads <c>BaseUrl</c> from Jellyfin's network configuration.
/// </summary>
internal sealed class ServerBaseUrlProvider : IBaseUrlProvider
{
    private readonly IServerConfigurationManager _configuration;
    private readonly ILogger<ServerBaseUrlProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerBaseUrlProvider"/> class.
    /// </summary>
    /// <param name="configuration">Server configuration manager.</param>
    /// <param name="logger">Logger.</param>
    public ServerBaseUrlProvider(
        IServerConfigurationManager configuration,
        ILogger<ServerBaseUrlProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GetBaseUrl()
    {
        try
        {
            var baseUrl = _configuration.GetNetworkConfiguration().BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl) || baseUrl == "/")
            {
                return string.Empty;
            }

            return baseUrl.TrimEnd('/');
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Jellyfin Premium UI: no se pudo leer BaseUrl.");
            return string.Empty;
        }
    }
}
