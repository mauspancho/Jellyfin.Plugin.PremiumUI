using System.Text;
using Jellyfin.Plugin.PremiumUI.Diagnostics;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PremiumUI.Hosting;

/// <summary>
/// Singleton holder for the embedded asset catalog and the injected index provider.
/// Built once per application lifetime.
/// </summary>
internal sealed class PremiumUiWebAssets
{
    /// <summary>
    /// Logical prefix of the embedded web resources.
    /// </summary>
    public const string ResourcePrefix = "Jellyfin.Plugin.PremiumUI.Web.";

    /// <summary>
    /// Path prefix under the web client where the assets are served.
    /// </summary>
    public const string PublicPrefix = "/web/premiumui/";

    /// <summary>
    /// Version of the bundled frontend.
    /// </summary>
    public const string FrontendVersion = "0.23.0";

    /// <summary>
    /// Initializes a new instance of the <see cref="PremiumUiWebAssets"/> class.
    /// </summary>
    /// <param name="applicationPaths">Jellyfin application paths.</param>
    /// <param name="status">Shared diagnostics state.</param>
    /// <param name="logger">Logger.</param>
    public PremiumUiWebAssets(
        IApplicationPaths applicationPaths,
        IntegrationStatus status,
        ILogger<PremiumUiWebAssets> logger)
    {
        Catalog = AssetCatalog.FromAssembly(typeof(PremiumUiWebAssets).Assembly, ResourcePrefix);

        var version = typeof(PremiumUiWebAssets).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

        Payload = BuildPayload();
        Index = new IndexHtmlProvider(applicationPaths.WebPath, Payload, version);

        status.AssetCount = Catalog.Count;
        status.FrontendVersion = FrontendVersion;

        if (Catalog.ScriptName is null)
        {
            logger.LogWarning(
                "Jellyfin Premium UI: no se encontró el asset app.js embebido; la inyección se desactivará.");
        }
        else
        {
            logger.LogInformation(
                "Jellyfin Premium UI: {Count} assets embebidos (script {Script}).",
                Catalog.Count,
                Catalog.ScriptName);
        }
    }

    /// <summary>
    /// Gets the embedded asset catalog.
    /// </summary>
    public AssetCatalog Catalog { get; }

    /// <summary>
    /// Gets the HTML block injected into the index.
    /// </summary>
    public string Payload { get; }

    /// <summary>
    /// Gets the index provider.
    /// </summary>
    public IndexHtmlProvider Index { get; }

    /// <summary>
    /// Gets a value indicating whether the injection has all required assets.
    /// </summary>
    public bool IsComplete => Catalog.ScriptName is not null;

    private string BuildPayload()
    {
        if (Catalog.ScriptName is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(320);
        builder.AppendLine("    <!-- premiumui:start -->");

        if (Catalog.StylesheetName is not null)
        {
            builder.Append("    <link rel=\"stylesheet\" href=\"./premiumui/")
                .Append(Catalog.StylesheetName)
                .AppendLine("\">");
        }

        builder.Append("    <script type=\"module\" src=\"./premiumui/")
            .Append(Catalog.ScriptName)
            .AppendLine("\"></script>");
        builder.AppendLine("    <!-- premiumui:end -->");

        return builder.ToString();
    }
}
