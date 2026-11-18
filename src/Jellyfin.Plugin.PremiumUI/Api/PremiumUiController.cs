using System.Reflection;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.PremiumUI.Configuration;
using Jellyfin.Plugin.PremiumUI.Diagnostics;
using Jellyfin.Plugin.PremiumUI.Library;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PremiumUI.Api;

/// <summary>
/// Administrative diagnostics for the plugin.
/// </summary>
[ApiController]
[Route("PremiumUI")]
public sealed class PremiumUiController : ControllerBase
{
    private readonly IServerApplicationHost _applicationHost;
    private readonly ILibraryManager _libraryManager;
    private readonly IntegrationStatus _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="PremiumUiController"/> class.
    /// </summary>
    /// <param name="applicationHost">Jellyfin application host.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="status">Shared integration status.</param>
    public PremiumUiController(
        IServerApplicationHost applicationHost,
        ILibraryManager libraryManager,
        IntegrationStatus status)
    {
        _applicationHost = applicationHost;
        _libraryManager = libraryManager;
        _status = status;
    }

    /// <summary>
    /// Returns the integration and compatibility diagnostics.
    /// </summary>
    /// <returns>Diagnostic snapshot.</returns>
    [HttpGet("Diagnostics")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult<DiagnosticsDto> GetDiagnostics()
    {
        var configuration = PremiumUiPlugin.Instance?.Configuration ?? new PluginConfiguration();
        var pluginVersion = typeof(PremiumUiPlugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(PremiumUiPlugin).Assembly.GetName().Version?.ToString()
            ?? "0.0.0.0";

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";

        return Ok(new DiagnosticsDto(
            PluginVersion: pluginVersion,
            TargetAbi: "12.4.0.0",
            ServerVersion: _applicationHost.ApplicationVersion.ToString(),
            Supported: _status.Supported,
            Integration: new IntegrationDto(
                MiddlewareRegistered: _status.MiddlewareRegistered,
                InjectionActive: _status.InjectionActive,
                LastMessage: _status.LastMessage,
                IndexRequests: _status.IndexRequests,
                AssetRequests: _status.AssetRequests,
                Injections: _status.Injections,
                BootstrapRequests: _status.BootstrapRequests,
                AssetCount: _status.AssetCount,
                LastInjectionUtc: _status.LastInjectionUtc),
            Configuration: new ConfigurationSummaryDto(
                Enabled: configuration.General.Enabled,
                HomeEnabled: configuration.General.EnableHome,
                DetailsEnabled: configuration.General.EnableDetails,
                HeroEnabled: configuration.Home.HeroEnabled,
                Carousels: new CarouselSummaryDto(
                    ContinueWatching: configuration.Home.ContinueWatching,
                    RecentlyAdded: configuration.Home.RecentlyAdded,
                    Movies: configuration.Home.Movies,
                    Series: configuration.Home.Series,
                    Recommendations: configuration.Home.Recommendations)),
            Frontend: new FrontendDto(
                Status: _status.InjectionActive
                    ? "Activo"
                    : (_status.AssetCount > 0 ? "Instalado, sin inyección" : "No instalado"),
                Version: _status.FrontendVersion,
                InitializationMs: null),
            Errors: _status.GetErrors()
                .Select(e => new ErrorDto(e.Timestamp, e.Source, e.Message))
                .ToArray()));
    }

    /// <summary>
    /// Lists every studio that exists in the server library, so the
    /// configuration page can offer them as a checkbox list.
    /// </summary>
    /// <returns>Compact studio list ordered by name.</returns>
    [HttpGet("Studios")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult<StudioListDto> GetStudios()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";

        var (_, entries) = LoadStudios();
        var groups = StudioGrouper.Group(entries);
        var studios = entries
            .Select(item => new StudioOptionDto(item.Id, item.Name))
            .ToArray();
        var grouped = groups
            .Select(group => new StudioGroupDto(
                group.Key,
                group.Name,
                group.Members.Select(member => new StudioOptionDto(member.Id, member.Name)).ToArray()))
            .ToArray();

        return Ok(new StudioListDto(studios.Length, studios, grouped));
    }

    private (BaseItem[] Items, StudioEntry[] Entries) LoadStudios()
    {
        var items = _libraryManager
            .GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Studio],
                Recursive = true,
                Limit = 10000
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        return (
            items,
            items.Select(item => new StudioEntry(item.Id.ToString("N"), item.Name)).ToArray());
    }

    private static StudioCollectionBootstrapDto ToBootstrap(
        StudioCollection collection,
        IReadOnlyDictionary<string, BaseItem> byId)
    {
        // Se prefiere el estudio que da nombre al grupo para el logo de
        // respaldo y, si no tiene imagen, el primer miembro con Primary/Thumb.
        var ordered = collection.Members
            .OrderByDescending(member => string.Equals(member.Name, collection.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        BaseItem? imageItem = null;
        string? imageType = null;

        foreach (var member in ordered)
        {
            if (byId.TryGetValue(member.Id, out var item) && item.HasImage(ImageType.Primary, 0))
            {
                imageItem = item;
                imageType = "Primary";
                break;
            }
        }

        if (imageItem is null)
        {
            foreach (var member in ordered)
            {
                if (byId.TryGetValue(member.Id, out var item) && item.HasImage(ImageType.Thumb, 0))
                {
                    imageItem = item;
                    imageType = "Thumb";
                    break;
                }
            }
        }

        return new StudioCollectionBootstrapDto(
            collection.Key,
            collection.Name,
            collection.Members.Select(member => member.Id).ToArray(),
            imageItem?.Id.ToString("N"),
            imageType);
    }

    /// <summary>
    /// Returns the non-administrative configuration used by the injected frontend.
    /// </summary>
    /// <returns>Bootstrap snapshot.</returns>
    [HttpGet("Bootstrap")]
    [Authorize]
    public ActionResult<BootstrapDto> GetBootstrap()
    {
        var configuration = PremiumUiPlugin.Instance?.Configuration ?? new PluginConfiguration();
        var normalized = ConfigurationValidator.Normalize(configuration);

        _status.RecordBootstrap();

        // El panel puede cambiar la configuración en cualquier momento: el
        // navegador no debe reutilizar una respuesta anterior. La caché real
        // vive en memoria del plugin (Performance.CacheMinutes).
        Response.Headers.CacheControl = "private, no-store";

        var collections = Array.Empty<StudioCollectionBootstrapDto>();

        if (normalized.General.EnableHome
            && normalized.Home.StudiosEnabled
            && normalized.Home.Studios.Count > 0)
        {
            var (items, entries) = LoadStudios();
            var groups = StudioGrouper.Group(entries);
            var resolved = StudioGrouper.Resolve(groups, entries, normalized.Home.Studios);
            var byId = items.ToDictionary(item => item.Id.ToString("N"), StringComparer.Ordinal);
            collections = resolved
                .Select(collection => ToBootstrap(collection, byId))
                .ToArray();
        }

        return Ok(new BootstrapDto(
            Version: _status.FrontendVersion,
            ServerVersion: _applicationHost.ApplicationVersion.ToString(),
            Supported: _status.Supported,
            Enabled: normalized.General.Enabled,
            Design: new DesignBootstrapDto(
                normalized.Design.Theme,
                normalized.Design.Transparency,
                normalized.Design.HeroHeight,
                normalized.Design.CardSize,
                normalized.Design.TransitionMs,
                normalized.Design.HoverIntensity,
                normalized.Design.AccentColor,
                normalized.Design.ShadowColor,
                normalized.Design.BackgroundColor),
            Home: new HomeBootstrapDto(
                Enabled: normalized.General.EnableHome,
                HeroEnabled: normalized.Home.HeroEnabled,
                HeroSource: normalized.Home.HeroSource,
                ContinueWatching: normalized.Home.ContinueWatching,
                RecentlyAdded: normalized.Home.RecentlyAdded,
                Movies: normalized.Home.Movies,
                Series: normalized.Home.Series,
                Recommendations: normalized.Home.Recommendations,
                MediaLibraries: normalized.Home.MediaLibraries,
                SectionOrder: normalized.Home.SectionOrder.ToArray(),
                ItemsPerSection: normalized.Home.ItemsPerSection,
                RecommendationsCount: normalized.Home.RecommendationsCount,
                StudioSections: normalized.Home.StudiosEnabled
                    ? normalized.Home.Studios.ToArray()
                    : Array.Empty<string>(),
                StudioCollections: collections),
            Details: new DetailsBootstrapDto(
                Enabled: normalized.General.EnableDetails,
                Backdrop: normalized.Details.Backdrop,
                Metadata: normalized.Details.Metadata,
                ExtraInfo: normalized.Details.ExtraInfo,
                Recommendations: normalized.Details.Recommendations),
            Performance: new PerformanceBootstrapDto(
                normalized.Performance.Preload,
                normalized.Performance.LazyLoading,
                normalized.Performance.CacheMinutes,
                normalized.Performance.ReducedMotion)));
    }
}

/// <summary>
/// Non-administrative configuration for the injected frontend.
/// </summary>
/// <param name="Version">Frontend version.</param>
/// <param name="ServerVersion">Running server version.</param>
/// <param name="Supported">Whether the server is supported.</param>
/// <param name="Enabled">Whether the plugin is enabled.</param>
/// <param name="Design">Design options.</param>
/// <param name="Home">Home options.</param>
/// <param name="Details">Details options.</param>
/// <param name="Performance">Performance options.</param>
public sealed record BootstrapDto(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("serverVersion")] string ServerVersion,
    [property: JsonPropertyName("supported")] bool Supported,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("design")] DesignBootstrapDto Design,
    [property: JsonPropertyName("home")] HomeBootstrapDto Home,
    [property: JsonPropertyName("details")] DetailsBootstrapDto Details,
    [property: JsonPropertyName("performance")] PerformanceBootstrapDto Performance);

/// <summary>
/// Design options for the frontend.
/// </summary>
/// <param name="Theme">Theme identifier.</param>
/// <param name="Transparency">Transparency percentage.</param>
/// <param name="HeroHeight">Hero height percentage.</param>
/// <param name="CardSize">Card size identifier.</param>
/// <param name="TransitionMs">Transition duration.</param>
/// <param name="HoverIntensity">Card hover intensity percentage (0-150).</param>
/// <param name="AccentColor">Optional accent color override (#rrggbb), empty for the theme color.</param>
/// <param name="ShadowColor">Optional card shadow color override (#rrggbb), empty for the theme color.</param>
/// <param name="BackgroundColor">Optional application background color override (#rrggbb), empty for the theme background.</param>
public sealed record DesignBootstrapDto(
    [property: JsonPropertyName("theme")] string Theme,
    [property: JsonPropertyName("transparency")] int Transparency,
    [property: JsonPropertyName("heroHeight")] int HeroHeight,
    [property: JsonPropertyName("cardSize")] string CardSize,
    [property: JsonPropertyName("transitionMs")] int TransitionMs,
    [property: JsonPropertyName("hoverIntensity")] int HoverIntensity,
    [property: JsonPropertyName("accentColor")] string AccentColor,
    [property: JsonPropertyName("shadowColor")] string ShadowColor,
    [property: JsonPropertyName("backgroundColor")] string BackgroundColor);

/// <summary>
/// Home options for the frontend.
/// </summary>
/// <param name="Enabled">Whether home enhancements are enabled.</param>
/// <param name="HeroEnabled">Whether the hero is enabled.</param>
/// <param name="HeroSource">Hero content source.</param>
/// <param name="ContinueWatching">Continue watching carousel.</param>
/// <param name="RecentlyAdded">Recently added carousel.</param>
/// <param name="Movies">Movies carousel.</param>
/// <param name="Series">Series carousel.</param>
/// <param name="Recommendations">Recommendations carousel.</param>
/// <param name="MediaLibraries">Whether the "My Media" libraries row is shown.</param>
/// <param name="SectionOrder">Section order.</param>
/// <param name="ItemsPerSection">Number of hero items.</param>
/// <param name="RecommendationsCount">Number of recommendation cards.</param>
/// <param name="StudioSections">Studios rendered as logo sections; empty when disabled.</param>
/// <param name="StudioCollections">Studio groups resolved for the current configuration.</param>
public sealed record HomeBootstrapDto(
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("heroEnabled")] bool HeroEnabled,
    [property: JsonPropertyName("heroSource")] string HeroSource,
    [property: JsonPropertyName("continueWatching")] bool ContinueWatching,
    [property: JsonPropertyName("recentlyAdded")] bool RecentlyAdded,
    [property: JsonPropertyName("movies")] bool Movies,
    [property: JsonPropertyName("series")] bool Series,
    [property: JsonPropertyName("recommendations")] bool Recommendations,
    [property: JsonPropertyName("mediaLibraries")] bool MediaLibraries,
    [property: JsonPropertyName("sectionOrder")] string[] SectionOrder,
    [property: JsonPropertyName("itemsPerSection")] int ItemsPerSection,
    [property: JsonPropertyName("recommendationsCount")] int RecommendationsCount,
    [property: JsonPropertyName("studioSections")] string[] StudioSections,
    [property: JsonPropertyName("studioCollections")] IReadOnlyList<StudioCollectionBootstrapDto> StudioCollections);

/// <summary>
/// Details options for the frontend.
/// </summary>
/// <param name="Enabled">Whether details enhancements are enabled.</param>
/// <param name="Backdrop">Backdrop enhancement.</param>
/// <param name="Metadata">Metadata enhancement.</param>
/// <param name="ExtraInfo">Extra information.</param>
/// <param name="Recommendations">Related content.</param>
public sealed record DetailsBootstrapDto(
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("backdrop")] bool Backdrop,
    [property: JsonPropertyName("metadata")] bool Metadata,
    [property: JsonPropertyName("extraInfo")] bool ExtraInfo,
    [property: JsonPropertyName("recommendations")] bool Recommendations);

/// <summary>
/// Performance options for the frontend.
/// </summary>
/// <param name="Preload">Image preloading.</param>
/// <param name="LazyLoading">Lazy loading.</param>
/// <param name="CacheMinutes">Cache TTL in minutes.</param>
/// <param name="ReducedMotion">Reduced motion policy.</param>
public sealed record PerformanceBootstrapDto(
    [property: JsonPropertyName("preload")] bool Preload,
    [property: JsonPropertyName("lazyLoading")] bool LazyLoading,
    [property: JsonPropertyName("cacheMinutes")] int CacheMinutes,
    [property: JsonPropertyName("reducedMotion")] string ReducedMotion);

/// <summary>
/// Complete diagnostics payload.
/// </summary>
/// <param name="PluginVersion">Plugin version.</param>
/// <param name="TargetAbi">Target Jellyfin ABI.</param>
/// <param name="ServerVersion">Running server version.</param>
/// <param name="Supported">Whether the running server is supported.</param>
/// <param name="Integration">Middleware integration state.</param>
/// <param name="Configuration">Configuration summary.</param>
/// <param name="Frontend">Frontend state.</param>
/// <param name="Errors">Recent errors.</param>
public sealed record DiagnosticsDto(
    string PluginVersion,
    string TargetAbi,
    string ServerVersion,
    bool Supported,
    IntegrationDto Integration,
    ConfigurationSummaryDto Configuration,
    FrontendDto Frontend,
    IReadOnlyList<ErrorDto> Errors);

/// <summary>
/// Middleware integration state.
/// </summary>
/// <param name="MiddlewareRegistered">Whether the startup filter was registered.</param>
/// <param name="InjectionActive">Whether index injection is active.</param>
/// <param name="LastMessage">Last integration message.</param>
/// <param name="IndexRequests">Intercepted index requests.</param>
/// <param name="AssetRequests">Served asset requests.</param>
/// <param name="Injections">Successful injections.</param>
/// <param name="BootstrapRequests">Authenticated frontend bootstrap requests.</param>
/// <param name="AssetCount">Number of embedded frontend assets.</param>
/// <param name="LastInjectionUtc">Last injection timestamp.</param>
public sealed record IntegrationDto(
    bool MiddlewareRegistered,
    bool InjectionActive,
    string LastMessage,
    long IndexRequests,
    long AssetRequests,
    long Injections,
    long BootstrapRequests,
    int AssetCount,
    DateTimeOffset? LastInjectionUtc);

/// <summary>
/// Safe configuration summary for the admin panel.
/// </summary>
/// <param name="Enabled">Plugin enabled.</param>
/// <param name="HomeEnabled">Home enabled.</param>
/// <param name="DetailsEnabled">Details enabled.</param>
/// <param name="HeroEnabled">Hero enabled.</param>
/// <param name="Carousels">Carousel switches.</param>
public sealed record ConfigurationSummaryDto(
    bool Enabled,
    bool HomeEnabled,
    bool DetailsEnabled,
    bool HeroEnabled,
    CarouselSummaryDto Carousels);

/// <summary>
/// Carousel switches.
/// </summary>
/// <param name="ContinueWatching">Continue watching.</param>
/// <param name="RecentlyAdded">Recently added.</param>
/// <param name="Movies">Movies.</param>
/// <param name="Series">Series.</param>
/// <param name="Recommendations">Recommendations.</param>
public sealed record CarouselSummaryDto(
    bool ContinueWatching,
    bool RecentlyAdded,
    bool Movies,
    bool Series,
    bool Recommendations);

/// <summary>
/// Frontend state.
/// </summary>
/// <param name="Status">Status text.</param>
/// <param name="Version">Frontend version.</param>
/// <param name="InitializationMs">Measured initialization time.</param>
public sealed record FrontendDto(string Status, string Version, double? InitializationMs);

/// <summary>
/// Studio option for the configuration page.
/// </summary>
/// <param name="Id">Studio identifier.</param>
/// <param name="Name">Studio name as stored by Jellyfin.</param>
public sealed record StudioOptionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

/// <summary>
/// Studio list response.
/// </summary>
/// <param name="Total">Number of studios.</param>
/// <param name="Items">Studios ordered by name.</param>
/// <param name="Groups">Groups of studios that share a brand.</param>
public sealed record StudioListDto(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("items")] IReadOnlyList<StudioOptionDto> Items,
    [property: JsonPropertyName("groups")] IReadOnlyList<StudioGroupDto> Groups);

/// <summary>
/// A group of studios that share a brand.
/// </summary>
/// <param name="Key">Stable group key.</param>
/// <param name="Name">Display name.</param>
/// <param name="Members">Studios in the group.</param>
public sealed record StudioGroupDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("members")] IReadOnlyList<StudioOptionDto> Members);

/// <summary>
/// Resolved studio collection for the injected frontend.
/// </summary>
/// <param name="Key">Stable collection key.</param>
/// <param name="Name">Display name.</param>
/// <param name="StudioIds">Member studio identifiers.</param>
/// <param name="ImageItemId">Studio whose image is used as fallback.</param>
/// <param name="ImageType">Image type of the fallback logo (Primary/Thumb).</param>
public sealed record StudioCollectionBootstrapDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("studioIds")] string[] StudioIds,
    [property: JsonPropertyName("imageItemId")] string? ImageItemId,
    [property: JsonPropertyName("imageType")] string? ImageType);

/// <summary>
/// Recent error entry.
/// </summary>
/// <param name="Timestamp">UTC timestamp.</param>
/// <param name="Source">Source.</param>
/// <param name="Message">Message.</param>
public sealed record ErrorDto(DateTimeOffset Timestamp, string Source, string Message);
