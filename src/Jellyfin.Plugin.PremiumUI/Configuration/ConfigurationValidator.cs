namespace Jellyfin.Plugin.PremiumUI.Configuration;

/// <summary>
/// Validates and normalizes plugin configuration values before they are persisted.
/// Invalid values never throw: they are replaced by safe defaults or clamped.
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// Current configuration schema version. Version 2 places "Estudios" right
    /// below the hero and version 3 places "Mis medios" below the studios when
    /// migrating older configurations.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>
    /// Allowed theme identifiers.
    /// </summary>
    public static readonly string[] Themes = ["cinema", "midnight", "amoled", "contrast", "noir", "aurora", "ember"];

    /// <summary>
    /// Allowed card size identifiers.
    /// </summary>
    public static readonly string[] CardSizes = ["small", "medium", "large"];

    /// <summary>
    /// Allowed hero content sources.
    /// </summary>
    public static readonly string[] HeroSources = ["random", "latest", "movies", "series"];

    /// <summary>
    /// Allowed reduced motion policies.
    /// </summary>
    public static readonly string[] ReducedMotionPolicies = ["auto", "on", "off"];

    /// <summary>
    /// Canonical section identifiers, in their default order.
    /// </summary>
    public static readonly string[] SectionIds = ["hero", "estudios", "media", "continue", "recent", "movies", "series", "recommendations"];

    /// <summary>
    /// Studios rendered as logo sections when the list is left empty.
    /// </summary>
    public static readonly string[] DefaultStudioSections = ["Disney", "Marvel", "DC", "Paramount", "Netflix"];

    /// <summary>
    /// Maximum number of studio sections rendered on the home screen.
    /// </summary>
    public const int MaxStudioSections = 40;

    /// <summary>
    /// Normalizes the supplied configuration, returning a new validated instance.
    /// </summary>
    /// <param name="input">Configuration received from the API or the XML file.</param>
    /// <returns>A safe configuration instance.</returns>
    public static PluginConfiguration Normalize(PluginConfiguration? input)
    {
        var source = input ?? new PluginConfiguration();

        return new PluginConfiguration
        {
            SchemaVersion = CurrentSchemaVersion,
            General = NormalizeGeneral(source.General),
            Design = NormalizeDesign(source.Design),
            Home = NormalizeHome(source.Home, source.SchemaVersion),
            Details = NormalizeDetails(source.Details),
            Performance = NormalizePerformance(source.Performance)
        };
    }

    private static GeneralOptions NormalizeGeneral(GeneralOptions? input)
    {
        var source = input ?? new GeneralOptions();
        return new GeneralOptions
        {
            Enabled = source.Enabled,
            EnableHome = source.EnableHome,
            EnableDetails = source.EnableDetails
        };
    }

    private static DesignOptions NormalizeDesign(DesignOptions? input)
    {
        var source = input ?? new DesignOptions();
        return new DesignOptions
        {
            Theme = Canonical(source.Theme, Themes, "cinema"),
            Transparency = Clamp(source.Transparency, 0, 100),
            HeroHeight = Clamp(source.HeroHeight, 40, 90),
            CardSize = Canonical(source.CardSize, CardSizes, "medium"),
            TransitionMs = Clamp(source.TransitionMs, 0, 600),
            HoverIntensity = Clamp(source.HoverIntensity, 0, 150),
            AccentColor = NormalizeColor(source.AccentColor)
        };
    }

    /// <summary>
    /// Normalizes an optional <c>#rrggbb</c> color. Invalid values fall back to
    /// an empty string, which lets the theme define the accent.
    /// </summary>
    private static string NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var hex = value.Trim().TrimStart('#');

        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(character => new string(character, 2)));
        }

        return hex.Length == 6 && hex.All(Uri.IsHexDigit)
            ? "#" + hex.ToLowerInvariant()
            : string.Empty;
    }

    private static HomeOptions NormalizeHome(HomeOptions? input, int schemaVersion)
    {
        var source = input ?? new HomeOptions();
        var order = NormalizeOrder(source.SectionOrder);

        // Migración a 0.12.0: "Estudios" pasa a estar justo bajo el hero.
        // Solo se aplica a configuraciones guardadas con el esquema anterior,
        // para no pisar un orden elegido por el usuario en el panel.
        if (schemaVersion < 2)
        {
            MoveAfter(order, "estudios", "hero");
        }

        // Migración a 0.14.0: "Mis medios" pasa a estar bajo los estudios.
        if (schemaVersion < 3)
        {
            MoveAfter(order, "media", "estudios");
        }

        return new HomeOptions
        {
            HeroEnabled = source.HeroEnabled,
            HeroSource = Canonical(source.HeroSource, HeroSources, "random"),
            ContinueWatching = source.ContinueWatching,
            RecentlyAdded = source.RecentlyAdded,
            Movies = source.Movies,
            Series = source.Series,
            Recommendations = source.Recommendations,
            MediaLibraries = source.MediaLibraries,
            SectionOrder = order,
            ItemsPerSection = Clamp(source.ItemsPerSection, 4, 20),
            StudiosEnabled = source.StudiosEnabled,
            Studios = NormalizeStudios(source.Studios, source.StudiosEnabled)
        };
    }

    private static DetailsOptions NormalizeDetails(DetailsOptions? input)
    {
        var source = input ?? new DetailsOptions();
        return new DetailsOptions
        {
            Backdrop = source.Backdrop,
            Metadata = source.Metadata,
            ExtraInfo = source.ExtraInfo,
            Recommendations = source.Recommendations
        };
    }

    private static PerformanceOptions NormalizePerformance(PerformanceOptions? input)
    {
        var source = input ?? new PerformanceOptions();
        return new PerformanceOptions
        {
            Preload = source.Preload,
            LazyLoading = source.LazyLoading,
            CacheMinutes = Clamp(source.CacheMinutes, 1, 120),
            ReducedMotion = Canonical(source.ReducedMotion, ReducedMotionPolicies, "auto")
        };
    }

    /// <summary>
    /// Normalizes the studio list: trims, deduplicates case-insensitively and
    /// caps the count. An empty list falls back to the canonical defaults.
    /// </summary>
    private static List<string> NormalizeStudios(List<string>? input, bool enabled)
    {
        var result = new List<string>(MaxStudioSections);

        if (input is not null)
        {
            foreach (var raw in input)
            {
                var value = raw?.Trim();

                if (string.IsNullOrEmpty(value)
                    || result.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(value.Length > 60 ? value[..60] : value);

                if (result.Count >= MaxStudioSections)
                {
                    break;
                }
            }
        }

        if (result.Count == 0 && enabled)
        {
            return DefaultStudioSections.ToList();
        }

        return result;
    }

    /// <summary>
    /// Moves <paramref name="id"/> right after <paramref name="anchor"/>, or to
    /// the second position when the anchor is missing.
    /// </summary>
    private static void MoveAfter(List<string> order, string id, string anchor)
    {
        order.Remove(id);
        var index = order.IndexOf(anchor);
        order.Insert(index >= 0 ? index + 1 : Math.Min(1, order.Count), id);
    }

    private static List<string> NormalizeOrder(List<string>? input)
    {
        var result = new List<string>(SectionIds.Length);

        if (input is not null)
        {
            foreach (var raw in input)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var value = raw.Trim().ToLowerInvariant();
                if (SectionIds.Contains(value, StringComparer.Ordinal) && !result.Contains(value, StringComparer.Ordinal))
                {
                    result.Add(value);
                }
            }
        }

        foreach (var id in SectionIds)
        {
            if (!result.Contains(id, StringComparer.Ordinal))
            {
                result.Add(id);
            }
        }

        return result;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Clamp(value, min, max);
    }

    private static string Canonical(string? value, string[] allowed, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return allowed.Contains(normalized, StringComparer.Ordinal) ? normalized : fallback;
    }
}
