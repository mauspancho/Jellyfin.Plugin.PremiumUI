using System.Text.Json;
using System.Linq;
using Jellyfin.Plugin.PremiumUI.Api;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class BootstrapContractTests
{
    [Fact]
    public void Bootstrap_SerializesWithCamelCaseKeys()
    {
        var dto = new BootstrapDto(
            "0.9.0",
            "12.4.0.0",
            true,
            true,
            new DesignBootstrapDto("cinema", 55, 68, "medium", 220, 125, "#7aa2ff", "#000000", "#101820", true, "radial-top", "#22d3ee"),
            new HomeBootstrapDto(
                true,
                true,
                "random",
                true,
                true,
                true,
                true,
                true,
                true,
                ["hero", "continue"],
                12,
                12,
                ["Disney", "Marvel"],
                []),
            new DetailsBootstrapDto(true, true, true, true, true),
            new PerformanceBootstrapDto(true, true, 10, "auto"));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(dto));
        var root = document.RootElement;

        Assert.Equal("0.9.0", root.GetProperty("version").GetString());
        Assert.Equal("12.4.0.0", root.GetProperty("serverVersion").GetString());
        Assert.True(root.GetProperty("supported").GetBoolean());
        Assert.True(root.GetProperty("enabled").GetBoolean());
        Assert.Equal("cinema", root.GetProperty("design").GetProperty("theme").GetString());
        Assert.Equal("#7aa2ff", root.GetProperty("design").GetProperty("accentColor").GetString());
        Assert.Equal("#000000", root.GetProperty("design").GetProperty("shadowColor").GetString());
        Assert.Equal("#101820", root.GetProperty("design").GetProperty("backgroundColor").GetString());
        Assert.True(root.GetProperty("design").GetProperty("backgroundGradient").GetBoolean());
        Assert.Equal("radial-top", root.GetProperty("design").GetProperty("backgroundGradientDirection").GetString());
        Assert.Equal("#22d3ee", root.GetProperty("design").GetProperty("backgroundGradientColor").GetString());
        Assert.Equal(68, root.GetProperty("design").GetProperty("heroHeight").GetInt32());
        Assert.Equal(125, root.GetProperty("design").GetProperty("hoverIntensity").GetInt32());
        Assert.Equal("random", root.GetProperty("home").GetProperty("heroSource").GetString());
        Assert.True(root.GetProperty("home").GetProperty("mediaLibraries").GetBoolean());
        Assert.Equal(2, root.GetProperty("home").GetProperty("sectionOrder").GetArrayLength());
        Assert.Equal(12, root.GetProperty("home").GetProperty("itemsPerSection").GetInt32());
        Assert.Equal(12, root.GetProperty("home").GetProperty("recommendationsCount").GetInt32());
        Assert.Equal(
            ["Disney", "Marvel"],
            root.GetProperty("home").GetProperty("studioSections").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal(0, root.GetProperty("home").GetProperty("studioCollections").GetArrayLength());
        Assert.True(root.GetProperty("details").GetProperty("backdrop").GetBoolean());
        Assert.Equal(10, root.GetProperty("performance").GetProperty("cacheMinutes").GetInt32());
        Assert.Equal("auto", root.GetProperty("performance").GetProperty("reducedMotion").GetString());

        Assert.False(root.TryGetProperty("Version", out _));
        Assert.False(root.TryGetProperty("Design", out _));
    }
}
