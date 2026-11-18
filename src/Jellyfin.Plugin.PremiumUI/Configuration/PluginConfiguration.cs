using System.Xml.Serialization;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PremiumUI.Configuration;

/// <summary>
/// Persistent plugin configuration. Values are global for every user in this version.
/// </summary>
[XmlRoot("PluginConfiguration")]
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Current configuration schema version.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the general options.
    /// </summary>
    public GeneralOptions General { get; set; } = new();

    /// <summary>
    /// Gets or sets the design options.
    /// </summary>
    public DesignOptions Design { get; set; } = new();

    /// <summary>
    /// Gets or sets the home screen options.
    /// </summary>
    public HomeOptions Home { get; set; } = new();

    /// <summary>
    /// Gets or sets the details screen options.
    /// </summary>
    public DetailsOptions Details { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance options.
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();
}

/// <summary>
/// General plugin switches.
/// </summary>
public sealed class GeneralOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the home screen is enhanced.
    /// </summary>
    public bool EnableHome { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the details screen is enhanced.
    /// </summary>
    public bool EnableDetails { get; set; } = true;
}

/// <summary>
/// Visual design options.
/// </summary>
public sealed class DesignOptions
{
    /// <summary>
    /// Gets or sets the visual theme identifier.
    /// </summary>
    public string Theme { get; set; } = "cinema";

    /// <summary>
    /// Gets or sets the transparency intensity (0-100).
    /// </summary>
    public int Transparency { get; set; } = 55;

    /// <summary>
    /// Gets or sets the hero height in viewport percentage (40-90).
    /// </summary>
    public int HeroHeight { get; set; } = 68;

    /// <summary>
    /// Gets or sets the card size identifier.
    /// </summary>
    public string CardSize { get; set; } = "medium";

    /// <summary>
    /// Gets or sets the transition duration in milliseconds (0-600).
    /// </summary>
    public int TransitionMs { get; set; } = 220;

    /// <summary>
    /// Gets or sets the optional accent color override in <c>#rrggbb</c> format.
    /// Empty means the selected theme defines the accent.
    /// </summary>
    public string AccentColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the card hover intensity percentage (0-150).
    /// 100 is the designed default; lower values soften the effect and higher
    /// values make the lift, ring and shadow more pronounced.
    /// </summary>
    public int HoverIntensity { get; set; } = 100;
}

/// <summary>
/// Home screen options.
/// </summary>
public sealed class HomeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the hero is rendered.
    /// </summary>
    public bool HeroEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the hero content source.
    /// </summary>
    public string HeroSource { get; set; } = "random";

    /// <summary>
    /// Gets or sets a value indicating whether the continue watching carousel is rendered.
    /// </summary>
    public bool ContinueWatching { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the recently added carousel is rendered.
    /// </summary>
    public bool RecentlyAdded { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the movies carousel is rendered.
    /// </summary>
    public bool Movies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the series carousel is rendered.
    /// </summary>
    public bool Series { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the recommendations carousel is rendered.
    /// </summary>
    public bool Recommendations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the "My Media" libraries row is shown.
    /// </summary>
    public bool MediaLibraries { get; set; } = true;

    /// <summary>
    /// Gets or sets the section order.
    /// Kept empty by default: <see cref="ConfigurationValidator"/> fills the canonical order.
    /// An initialized default list would be appended to by XmlSerializer on every reload.
    /// </summary>
    public List<string> SectionOrder { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of hero items (4-20).
    /// </summary>
    public int ItemsPerSection { get; set; } = 12;

    /// <summary>
    /// Gets or sets the number of recommendation cards (4-20).
    /// Independent from <see cref="ItemsPerSection"/>, which sizes the hero.
    /// </summary>
    public int RecommendationsCount { get; set; } = 12;

    /// <summary>
    /// Gets or sets a value indicating whether studio collections are rendered.
    /// </summary>
    public bool StudiosEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the studio names (exactly as the server names them).
    /// An empty list is completed with the canonical defaults while enabled.
    /// </summary>
    public List<string> Studios { get; set; } = new();
}

/// <summary>
/// Details screen options.
/// </summary>
public sealed class DetailsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the backdrop is enhanced.
    /// </summary>
    public bool Backdrop { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metadata is enhanced.
    /// </summary>
    public bool Metadata { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether extra information is rendered.
    /// </summary>
    public bool ExtraInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether related content is rendered.
    /// </summary>
    public bool Recommendations { get; set; } = true;
}

/// <summary>
/// Performance options.
/// </summary>
public sealed class PerformanceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether images are preloaded.
    /// </summary>
    public bool Preload { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether lazy loading is used.
    /// </summary>
    public bool LazyLoading { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes (1-120).
    /// </summary>
    public int CacheMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the reduced motion policy: auto, on or off.
    /// </summary>
    public string ReducedMotion { get; set; } = "auto";
}
