using Jellyfin.Plugin.PremiumUI.Configuration;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class ConfigurationValidatorTests
{
    [Fact]
    public void Normalize_Null_ReturnsDefaults()
    {
        var result = ConfigurationValidator.Normalize(null);

        Assert.Equal(ConfigurationValidator.CurrentSchemaVersion, result.SchemaVersion);
        Assert.True(result.General.Enabled);
        Assert.True(result.General.EnableHome);
        Assert.True(result.General.EnableDetails);
        Assert.Equal("cinema", result.Design.Theme);
        Assert.Equal(55, result.Design.Transparency);
        Assert.Equal(68, result.Design.HeroHeight);
        Assert.Equal("medium", result.Design.CardSize);
        Assert.Equal(220, result.Design.TransitionMs);
        Assert.Equal(100, result.Design.HoverIntensity);
        Assert.Equal(string.Empty, result.Design.AccentColor);
        Assert.Equal(string.Empty, result.Design.ShadowColor);
        Assert.Equal(string.Empty, result.Design.BackgroundColor);
        Assert.False(result.Design.BackgroundGradient);
        Assert.Equal("bottom", result.Design.BackgroundGradientDirection);
        Assert.Equal(string.Empty, result.Design.BackgroundGradientColor);
        Assert.Equal(12, result.Home.ItemsPerSection);
        Assert.Equal(12, result.Home.RecommendationsCount);
        Assert.Equal(10, result.Performance.CacheMinutes);
        Assert.Equal("auto", result.Performance.ReducedMotion);
        Assert.Equal(ConfigurationValidator.SectionIds, result.Home.SectionOrder);
    }

    [Fact]
    public void Configuration_XmlReload_DoesNotDuplicateSectionOrder()
    {
        var original = new PluginConfiguration();
        original.Home.SectionOrder = ["movies", "hero"];

        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PluginConfiguration));
        using var stream = new MemoryStream();
        serializer.Serialize(stream, original);
        stream.Position = 0;

        var reloaded = (PluginConfiguration)serializer.Deserialize(stream)!;
        var normalized = ConfigurationValidator.Normalize(reloaded);

        Assert.Equal(
            ["movies", "hero", "estudios", "media", "continue", "recent", "series", "recommendations"],
            normalized.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_LegacyOrder_MovesStudiosBelowHero()
    {
        var input = new PluginConfiguration
        {
            SchemaVersion = 1,
            Home = new HomeOptions
            {
                SectionOrder = ["hero", "continue", "recent", "movies", "series", "recommendations", "estudios"]
            }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(
            ["hero", "estudios", "media", "continue", "recent", "movies", "series", "recommendations"],
            result.Home.SectionOrder);
        Assert.Equal(ConfigurationValidator.CurrentSchemaVersion, result.SchemaVersion);
    }

    [Fact]
    public void Normalize_CurrentSchema_KeepsCustomOrder()
    {
        var input = new PluginConfiguration
        {
            SchemaVersion = ConfigurationValidator.CurrentSchemaVersion,
            Home = new HomeOptions
            {
                SectionOrder = ["hero", "continue", "estudios", "recent", "movies", "series", "recommendations"]
            }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(
            ["hero", "continue", "estudios", "recent", "movies", "series", "recommendations", "media"],
            result.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_Schema2Order_InsertsMediaBelowStudios()
    {
        var input = new PluginConfiguration
        {
            SchemaVersion = 2,
            Home = new HomeOptions
            {
                SectionOrder = ["hero", "estudios", "continue", "recent", "movies", "series", "recommendations"]
            }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(
            ["hero", "estudios", "media", "continue", "recent", "movies", "series", "recommendations"],
            result.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_Schema2Order_KeepsStudiosWhereTheUserPutThem()
    {
        var input = new PluginConfiguration
        {
            SchemaVersion = 2,
            Home = new HomeOptions
            {
                SectionOrder = ["hero", "recent", "estudios", "continue", "movies", "series", "recommendations"]
            }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(
            ["hero", "recent", "estudios", "media", "continue", "movies", "series", "recommendations"],
            result.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_MediaLibraries_DefaultsTrueAndPreservesFalse()
    {
        Assert.True(ConfigurationValidator.Normalize(null).Home.MediaLibraries);

        var disabled = ConfigurationValidator.Normalize(new PluginConfiguration
        {
            Home = new HomeOptions { MediaLibraries = false }
        });

        Assert.False(disabled.Home.MediaLibraries);
    }

    [Fact]
    public void Normalize_Home_StudiosEmptyAndEnabled_FillsDefaults()
    {
        var input = new PluginConfiguration { Home = new HomeOptions { StudiosEnabled = true, Studios = [] } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(["Disney", "Marvel", "DC", "Paramount", "Netflix"], result.Home.Studios);
    }

    [Fact]
    public void Normalize_Home_StudiosEmptyAndDisabled_LeavesEmpty()
    {
        var input = new PluginConfiguration { Home = new HomeOptions { StudiosEnabled = false, Studios = [] } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Empty(result.Home.Studios);
    }

    [Fact]
    public void Normalize_Home_Studios_TrimsAndDeduplicatesCaseInsensitive()
    {
        var input = new PluginConfiguration
        {
            Home = new HomeOptions { StudiosEnabled = true, Studios = ["  Marvel  ", "", "marvel", "DC", "DC", "   ", "Paramount"] }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(["Marvel", "DC", "Paramount"], result.Home.Studios);
    }

    [Fact]
    public void Normalize_Home_Studios_CapsAtForty()
    {
        var input = new PluginConfiguration
        {
            Home = new HomeOptions
            {
                StudiosEnabled = true,
                Studios = Enumerable.Range(1, 60).Select(i => $"Studio {i}").ToList()
            }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(40, result.Home.Studios.Count);
        Assert.Equal("Studio 40", result.Home.Studios[^1]);
    }

    [Fact]
    public void Normalize_Home_Studios_LongNamesAreTruncated()
    {
        var input = new PluginConfiguration
        {
            Home = new HomeOptions { StudiosEnabled = true, Studios = [new string('x', 80)] }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(60, Assert.Single(result.Home.Studios).Length);
    }

    [Fact]
    public void Normalize_ClampsNumericRanges()    {
        var input = new PluginConfiguration
        {
            Design = new DesignOptions { Transparency = -40, HeroHeight = 400, TransitionMs = 5000, HoverIntensity = 500 },
            Home = new HomeOptions { ItemsPerSection = 500, RecommendationsCount = 1 },
            Performance = new PerformanceOptions { CacheMinutes = 0 }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(0, result.Design.Transparency);
        Assert.Equal(90, result.Design.HeroHeight);
        Assert.Equal(600, result.Design.TransitionMs);
        Assert.Equal(150, result.Design.HoverIntensity);
        Assert.Equal(20, result.Home.ItemsPerSection);
        Assert.Equal(4, result.Home.RecommendationsCount);
        Assert.Equal(1, result.Performance.CacheMinutes);
    }

    [Theory]
    [InlineData("CINEMA", "cinema")]
    [InlineData(" midnight ", "midnight")]
    [InlineData("AMOLED", "amoled")]
    [InlineData("contrast", "contrast")]
    [InlineData("noir", "noir")]
    [InlineData("AURORA", "aurora")]
    [InlineData("ember", "ember")]
    [InlineData("purple", "cinema")]
    [InlineData("", "cinema")]
    [InlineData(null, "cinema")]
    public void Normalize_Design_ThemeIsCanonical(string? theme, string expected)
    {
        var input = new PluginConfiguration { Design = new DesignOptions { Theme = theme! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Design.Theme);
    }

    [Theory]
    [InlineData("LARGE", "large")]
    [InlineData("huge", "medium")]
    [InlineData(null, "medium")]
    public void Normalize_Design_CardSizeIsCanonical(string? size, string expected)
    {
        var input = new PluginConfiguration { Design = new DesignOptions { CardSize = size! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Design.CardSize);
    }

    [Theory]
    [InlineData("#E6B45E", "#e6b45e")]
    [InlineData("7aa2ff", "#7aa2ff")]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("#GGGGGG", "")]
    [InlineData("naranja", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_Design_AccentColorIsValidHex(string? color, string expected)
    {
        var input = new PluginConfiguration { Design = new DesignOptions { AccentColor = color! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Design.AccentColor);
    }

    [Theory]
    [InlineData("#000000", "#000000")]
    [InlineData("112233", "#112233")]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("#GGGGGG", "")]
    [InlineData("gris", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_Design_ShadowColorIsValidHex(string? color, string expected)
    {
        var input = new PluginConfiguration { Design = new DesignOptions { ShadowColor = color! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Design.ShadowColor);
    }

    [Theory]
    [InlineData("#000000", "#000000")]
    [InlineData("112233", "#112233")]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("#GGGGGG", "")]
    [InlineData("gris", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_Design_BackgroundColorIsValidHex(string? color, string expected)
    {
        var input = new PluginConfiguration { Design = new DesignOptions { BackgroundColor = color! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Design.BackgroundColor);
    }

    [Theory]
    [InlineData("bottom", "bottom")]
    [InlineData("Top-Left", "top-left")]
    [InlineData("radial-top", "radial-top")]
    [InlineData("radial-bottom", "radial-bottom")]
    [InlineData("diagonal", "bottom")]
    [InlineData("", "bottom")]
    [InlineData(null, "bottom")]
    public void Normalize_Design_BackgroundGradientDirectionIsCanonical(string? direction, string expected)
    {
        var input = new PluginConfiguration
        {
            Design = new DesignOptions { BackgroundGradient = true, BackgroundGradientDirection = direction! }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.True(result.Design.BackgroundGradient);
        Assert.Equal(expected, result.Design.BackgroundGradientDirection);
    }

    [Theory]
    [InlineData("#22d3ee", "#22d3ee")]
    [InlineData("#ABC", "#aabbcc")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_Design_BackgroundGradientColorIsValidHex(string? color, string expected)
    {
        var input = new PluginConfiguration
        {
            Design = new DesignOptions { BackgroundGradient = true, BackgroundGradientColor = color! }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Design.BackgroundGradientColor);
    }

    [Theory]
    [InlineData("latest", "latest")]
    [InlineData("Movies", "movies")]
    [InlineData("resume", "random")]
    [InlineData("trending", "random")]
    [InlineData(null, "random")]
    public void Normalize_Home_HeroSourceIsCanonical(string? source, string expected)
    {
        var input = new PluginConfiguration { Home = new HomeOptions { HeroSource = source! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Home.HeroSource);
    }

    [Theory]
    [InlineData("ON", "on")]
    [InlineData("reduce", "auto")]
    [InlineData(null, "auto")]
    public void Normalize_Performance_ReducedMotionIsCanonical(string? policy, string expected)
    {
        var input = new PluginConfiguration { Performance = new PerformanceOptions { ReducedMotion = policy! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(expected, result.Performance.ReducedMotion);
    }

    [Fact]
    public void Normalize_Order_RemovesUnknownDuplicatesAndAppendsMissing()
    {
        var input = new PluginConfiguration
        {
            Home = new HomeOptions
            {
                SectionOrder = ["series", "unknown", "series", " hero ", ""]
            }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(
            ["series", "hero", "estudios", "media", "continue", "recent", "movies", "recommendations"],
            result.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_Order_NullUsesDefaultOrder()
    {
        var input = new PluginConfiguration { Home = new HomeOptions { SectionOrder = null! } };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(ConfigurationValidator.SectionIds, result.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_Order_ReturnsMutableCopy()
    {
        var input = new PluginConfiguration { Home = new HomeOptions() };

        var first = ConfigurationValidator.Normalize(input);
        first.Home.SectionOrder.Remove("hero");

        var second = ConfigurationValidator.Normalize(input);

        Assert.Equal(ConfigurationValidator.SectionIds, second.Home.SectionOrder);
        Assert.Contains("hero", second.Home.SectionOrder);
    }

    [Fact]
    public void Normalize_PreservesBooleans()
    {
        var input = new PluginConfiguration
        {
            General = new GeneralOptions { Enabled = false, EnableHome = false, EnableDetails = true },
            Home = new HomeOptions { HeroEnabled = false, Movies = false, Recommendations = false },
            Details = new DetailsOptions { Backdrop = false, Metadata = true },
            Performance = new PerformanceOptions { Preload = false, LazyLoading = false }
        };

        var result = ConfigurationValidator.Normalize(input);

        Assert.False(result.General.Enabled);
        Assert.False(result.General.EnableHome);
        Assert.True(result.General.EnableDetails);
        Assert.False(result.Home.HeroEnabled);
        Assert.False(result.Home.Movies);
        Assert.False(result.Home.Recommendations);
        Assert.False(result.Details.Backdrop);
        Assert.True(result.Details.Metadata);
        Assert.False(result.Performance.Preload);
        Assert.False(result.Performance.LazyLoading);
    }

    [Fact]
    public void Normalize_SchemaVersionIsAlwaysCurrent()
    {
        var input = new PluginConfiguration { SchemaVersion = 99 };

        var result = ConfigurationValidator.Normalize(input);

        Assert.Equal(ConfigurationValidator.CurrentSchemaVersion, result.SchemaVersion);
    }
}
