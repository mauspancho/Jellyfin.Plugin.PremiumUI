using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.PremiumUI.Library;

/// <summary>
/// A studio as it exists in the Jellyfin library.
/// </summary>
/// <param name="Id">Studio identifier (32 hex chars).</param>
/// <param name="Name">Studio display name.</param>
public sealed record StudioEntry(string Id, string Name);

/// <summary>
/// Studios that share a brand or differ only in their corporate suffix.
/// </summary>
/// <param name="Key">Stable group key.</param>
/// <param name="Name">Display name.</param>
/// <param name="Members">Member studios.</param>
public sealed record StudioGroup(string Key, string Name, IReadOnlyList<StudioEntry> Members);

/// <summary>
/// A collection rendered as a single logo tile in Home.
/// </summary>
/// <param name="Key">Stable collection key.</param>
/// <param name="Name">Display name.</param>
/// <param name="Members">Studios whose titles belong to the collection.</param>
public sealed record StudioCollection(string Key, string Name, IReadOnlyList<StudioEntry> Members);

/// <summary>
/// Groups studios that are different labels for the same brand.
///
/// Jellyfin creates one studio entry per metadata spelling, so a library can
/// contain "Disney", "Disney Channel", "Walt Disney Pictures" and "Walt Disney
/// Animation" as four unrelated studios. This groups them without touching the
/// library: only the configuration page and the injected frontend use the
/// result.
/// </summary>
public static partial class StudioGrouper
{
    private static readonly HashSet<string> CorporateSuffixes = new(StringComparer.Ordinal)
    {
        "studio", "studios", "picture", "pictures", "film", "films", "movie", "movies",
        "cinema", "entretenimento", "filmes", "entertainment", "production", "productions",
        "company", "co", "inc", "ltd", "llc", "corporation", "corp", "group", "holdings",
        "international", "distribution", "home", "video", "television", "tv", "channel",
        "network", "animation", "animations", "interactive", "kids", "family", "feature",
        "features", "documentary", "documentaries", "and", "of", "a", "an"
    };

    // Las marcas conocidas se agrupan por nombre completo, no por prefijo de
    // token: así "Nippon Columbia" no cae dentro de Sony/Columbia y "Atomic
    // Cartoons" no cae dentro de Cartoon Network. El orden importa: la primera
    // regla que coincide decide la etiqueta del grupo.
    private static readonly BrandRule[] Brands =
    [
        new("disney", "Disney", [], ["disney"]),
        new("marvel", "Marvel", [], ["marvel"]),
        new("dc", "DC", [], ["dc"]),
        new("warner", "Warner Bros.", ["warner"], []),
        new("universal", "Universal", ["universal"], []),
        new("paramount", "Paramount", ["paramount"], []),
        new("sony", "Sony Pictures", ["sony", "columbia", "tristar"], []),
        new("fox", "20th Century Fox", [], ["fox"]),
        new("fox", "20th Century Fox", [], ["20th"]),
        new("mgm", "MGM", [], ["mgm"]),
        new("netflix", "Netflix", ["netflix"], []),
        new("pixar", "Pixar", ["pixar"], []),
        new("dreamworks", "DreamWorks", ["dreamworks"], []),
        new("lucasfilm", "Lucasfilm", ["lucasfilm"], []),
        new("ghibli", "Studio Ghibli", [], ["ghibli"]),
        new("hbo", "HBO", [], ["hbo"]),
        new("bbc", "BBC", [], ["bbc"]),
        new("nickelodeon", "Nickelodeon", ["nickelodeon"], []),
        new("cartoon-network", "Cartoon Network", ["cartoonnetwork"], []),
        new("amazon", "Amazon", [], ["amazon"]),
        new("apple", "Apple", [], ["apple"]),
        new("lionsgate", "Lionsgate", [], ["lionsgate"]),
        new("toei", "Toei", [], ["toei"]),
        new("toho", "Toho", [], ["toho"]),
        new("a24", "A24", [], ["a24"]),
        new("blumhouse", "Blumhouse", [], ["blumhouse"]),
        new("legendary", "Legendary", [], ["legendary"]),
        new("new-line", "New Line Cinema", [], ["new", "line"]),
        new("focus", "Focus Features", [], ["focus"]),
        new("miramax", "Miramax", [], ["miramax"]),
        new("illumination", "Illumination", [], ["illumination"]),
        new("laika", "Laika", [], ["laika"]),
        new("saban", "Saban", [], ["saban"]),
        new("hasbro", "Hasbro", [], ["hasbro"]),
        new("funimation", "Funimation", [], ["funimation"]),
        new("crunchyroll", "Crunchyroll", [], ["crunchyroll"]),
        new("cbs", "CBS", [], ["cbs"]),
        new("amc", "AMC", [], ["amc"]),
        new("fx", "FX", ["fx"], []),
        new("cbc", "CBC", [], ["cbc"])
    ];

    /// <summary>
    /// Groups studios that share a brand or a base name. Groups with a single
    /// member are not returned: they stay as individual studios.
    /// </summary>
    /// <param name="studios">All studios in the server.</param>
    /// <returns>Groups ordered by display name.</returns>
    public static IReadOnlyList<StudioGroup> Group(IReadOnlyList<StudioEntry> studios)
    {
        if (studios.Count == 0)
        {
            return [];
        }

        var bases = studios.Select(studio => BaseTokens(studio.Name)).ToArray();
        var brands = studios.Select(studio => MatchBrand(studio.Name)).ToArray();
        var parent = Enumerable.Range(0, studios.Count).ToArray();

        int Find(int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }

            return index;
        }

        void Union(int left, int right)
        {
            var a = Find(left);
            var b = Find(right);

            if (a != b)
            {
                parent[b] = a;
            }
        }

        // Misma base tras quitar sufijos corporativos: "Working Title" y
        // "Working Title Films" son el mismo estudio.
        var byBase = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < studios.Count; index++)
        {
            var key = string.Join('\u001f', bases[index]);

            if (byBase.TryGetValue(key, out var first))
            {
                Union(first, index);
            }
            else
            {
                byBase[key] = index;
            }
        }

        // Misma marca aunque la base cambie: "Disney Channel" y "Walt Disney
        // Pictures", o "20th Century Fox" y "Fox Searchlight Pictures".
        var byBrand = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < studios.Count; index++)
        {
            var brand = brands[index];

            if (brand is null)
            {
                continue;
            }

            if (byBrand.TryGetValue(brand.Key, out var first))
            {
                Union(first, index);
            }
            else
            {
                byBrand[brand.Key] = index;
            }
        }

        var clusters = new Dictionary<int, List<int>>();

        for (var index = 0; index < studios.Count; index++)
        {
            var root = Find(index);

            if (!clusters.TryGetValue(root, out var members))
            {
                members = [];
                clusters[root] = members;
            }

            members.Add(index);
        }

        var groups = new List<StudioGroup>();

        foreach (var members in clusters.Values)
        {
            if (members.Count < 2)
            {
                continue;
            }

            var memberEntries = members
                .Select(index => studios[index])
                .OrderBy(studio => studio.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(studio => studio.Name, StringComparer.Ordinal)
                .ToArray();

            var clusterBrands = members
                .Select(index => brands[index])
                .Where(brand => brand is not null)
                .Select(brand => brand!)
                .DistinctBy(brand => brand.Key)
                .OrderBy(brand => Array.FindIndex(Brands, item => item.Key == brand.Key))
                .ToArray();

            var key = clusterBrands.Length > 0
                ? "brand:" + string.Join('-', clusterBrands.Select(brand => brand.Key))
                : "base:" + Slug(bases[members[0]]);

            var name = clusterBrands.Length switch
            {
                1 => clusterBrands[0].Label,
                > 1 => string.Join(" / ", clusterBrands.Select(brand => brand.Label)),
                _ => memberEntries
                    .OrderBy(studio => studio.Name.Length)
                    .ThenBy(studio => studio.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(studio => studio.Name, StringComparer.Ordinal)
                    .First()
                    .Name
            };

            groups.Add(new StudioGroup(key, name, memberEntries));
        }

        return groups
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Resolves the configured names to collections. A configured name may be
    /// a group label ("Disney"), a real studio ("A24") or a member of a group
    /// ("Walt Disney Pictures"); all of them resolve to the whole group.
    /// </summary>
    /// <param name="groups">Groups from <see cref="Group"/>.</param>
    /// <param name="studios">All studios in the server.</param>
    /// <param name="names">Configured studio names, in order.</param>
    /// <returns>Collections in configuration order, without duplicates.</returns>
    public static IReadOnlyList<StudioCollection> Resolve(
        IReadOnlyList<StudioGroup> groups,
        IReadOnlyList<StudioEntry> studios,
        IReadOnlyList<string> names)
    {
        var byGroupName = new Dictionary<string, StudioGroup>(StringComparer.Ordinal);
        var byMember = new Dictionary<string, StudioGroup>(StringComparer.Ordinal);

        foreach (var group in groups)
        {
            byGroupName[Normalize(group.Name)] = group;

            foreach (var member in group.Members)
            {
                byMember[Normalize(member.Name)] = group;
            }
        }

        var byStudioName = new Dictionary<string, StudioEntry>(StringComparer.Ordinal);

        foreach (var studio in studios)
        {
            byStudioName.TryAdd(Normalize(studio.Name), studio);
        }

        var result = new List<StudioCollection>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in names)
        {
            var name = (raw ?? string.Empty).Trim();

            if (name.Length == 0)
            {
                continue;
            }

            var normalized = Normalize(name);
            StudioGroup? group = null;

            if (!byGroupName.TryGetValue(normalized, out group)
                && !byMember.TryGetValue(normalized, out group))
            {
                var brand = MatchBrand(name);

                if (brand is not null)
                {
                    group = groups.FirstOrDefault(item => item.Key == "brand:" + brand.Key);
                }
            }

            if (group is not null)
            {
                if (seen.Add(group.Key))
                {
                    result.Add(new StudioCollection(group.Key, group.Name, group.Members));
                }

                continue;
            }

            if (byStudioName.TryGetValue(normalized, out var studio))
            {
                if (seen.Add("studio:" + studio.Id))
                {
                    result.Add(new StudioCollection("studio:" + studio.Id, studio.Name, [studio]));
                }

                continue;
            }

            // Último recurso: una variante ortográfica ("Walt Disney" cuando la
            // lista del servidor solo tiene "Walt Disney Pictures").
            group = groups
                .Where(item => item.Members.Any(member => Similar(member.Name, name)))
                .OrderByDescending(item => item.Members.Count)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (group is not null && seen.Add(group.Key))
            {
                result.Add(new StudioCollection(group.Key, group.Name, group.Members));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the brand key of a studio name, if any.
    /// </summary>
    /// <param name="name">Studio name.</param>
    /// <returns>Brand key or <c>null</c>.</returns>
    public static string? BrandKey(string name) => MatchBrand(name)?.Key;

    private static BrandRule? MatchBrand(string name)
    {
        var tokens = new HashSet<string>(Tokenize(name), StringComparer.Ordinal);
        var compact = Compact(name);

        foreach (var brand in Brands)
        {
            if (brand.Words.Length > 0 && brand.Words.All(tokens.Contains))
            {
                return brand;
            }

            if (brand.Prefixes.Any(prefix => compact.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return brand;
            }
        }

        return null;
    }

    private static string[] BaseTokens(string name)
    {
        var tokens = Tokenize(name).ToList();

        if (tokens.Count > 1 && tokens[0] == "the")
        {
            tokens.RemoveAt(0);
        }

        var meaningful = tokens
            .Where(token => !CorporateSuffixes.Contains(token))
            .ToArray();

        return meaningful.Length > 0 ? meaningful : tokens.ToArray();
    }

    private static string[] Tokenize(string name)
    {
        var decomposed = name
            .Replace("&", " and ", StringComparison.Ordinal)
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        var previousWasLower = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var isLetter = char.IsLetter(character);
            var isLower = char.IsLower(character);

            // "DreamWorks" y "StudioCanal" se separan en dos palabras para que
            // las reglas de marca puedan compararlas.
            if (previousWasLower && char.IsUpper(character))
            {
                builder.Append(' ');
            }

            builder.Append(char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? char.ToLowerInvariant(character) : ' ');
            previousWasLower = isLetter && isLower;
        }

        return TokenPattern()
            .Matches(builder.ToString())
            .Select(match => match.Value)
            .ToArray();
    }

    private static string Compact(string name)
        => string.Concat(Tokenize(name));

    private static string Normalize(string name)
        => string.Join(' ', Tokenize(name));

    private static string Slug(string[] tokens)
    {
        var slug = string.Join('-', tokens.Where(token => token.Length > 0));

        return slug.Length > 0 ? slug : "studio";
    }

    private static bool Similar(string left, string right)
    {
        var a = Normalize(left);
        var b = Normalize(right);

        if (a.Length < 3 || b.Length < 3)
        {
            return false;
        }

        return a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal);
    }

    [GeneratedRegex("[a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    private sealed record BrandRule(string Key, string Label, string[] Prefixes, string[] Words);
}
