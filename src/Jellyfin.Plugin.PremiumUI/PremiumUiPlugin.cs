using System.Globalization;
using Jellyfin.Plugin.PremiumUI.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PremiumUI;

/// <summary>
/// Jellyfin Premium UI plugin entry point.
/// </summary>
public sealed class PremiumUiPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Stable plugin identifier. Keep in sync with meta.json and the admin page.
    /// </summary>
    public static readonly Guid PluginId = Guid.Parse("a15478cd-8875-46af-b528-e3c020f2cd71");

    /// <summary>
    /// Initializes a new instance of the <see cref="PremiumUiPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Jellyfin application paths.</param>
    /// <param name="xmlSerializer">Jellyfin XML serializer.</param>
    public PremiumUiPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        Configuration = ConfigurationValidator.Normalize(Configuration);
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static PremiumUiPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Jellyfin Premium UI";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <inheritdoc />
    public override string Description =>
        "Interfaz cinematográfica premium para la pantalla de inicio y los detalles de Jellyfin Web.";

    /// <inheritdoc />
    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        var normalized = ConfigurationValidator.Normalize(configuration as PluginConfiguration);
        base.UpdateConfiguration(normalized);
    }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    typeof(PremiumUiPlugin).Namespace)
            }
        ];
    }
}
