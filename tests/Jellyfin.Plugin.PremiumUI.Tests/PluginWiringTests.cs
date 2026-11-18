using System.Reflection;
using System.Xml.Serialization;
using Jellyfin.Plugin.PremiumUI.Configuration;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class PluginWiringTests
{
    [Fact]
    public void Configuration_RoundTripsThroughXml()
    {
        var original = new PluginConfiguration();
        original.General.Enabled = false;
        original.Design.Theme = "midnight";
        original.Design.AccentColor = "#112233";
        original.Home.SectionOrder = ["movies", "hero"];
        original.Performance.CacheMinutes = 42;

        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var stream = new MemoryStream();

        serializer.Serialize(stream, original);
        stream.Position = 0;
        var restored = (PluginConfiguration)serializer.Deserialize(stream)!;

        Assert.Equal(1, restored.SchemaVersion);
        Assert.False(restored.General.Enabled);
        Assert.Equal("midnight", restored.Design.Theme);
        Assert.Equal("#112233", restored.Design.AccentColor);
        Assert.Equal(["movies", "hero"], restored.Home.SectionOrder);
        Assert.Equal(42, restored.Performance.CacheMinutes);
    }

    [Fact]
    public void Plugin_ReportsStableIdentity()
    {
        Assert.Equal(
            Guid.Parse("a15478cd-8875-46af-b528-e3c020f2cd71"),
            PremiumUiPlugin.PluginId);
    }

    [Fact]
    public void Plugin_EmbedsConfigurationPage()
    {
        var resources = typeof(PremiumUiPlugin).Assembly.GetManifestResourceNames();

        Assert.Contains(
            "Jellyfin.Plugin.PremiumUI.Configuration.configPage.html",
            resources);
    }

    [Fact]
    public void Plugin_DeclaresSupportedAssemblyVersion()
    {
        var version = typeof(PremiumUiPlugin).Assembly.GetName().Version;

        Assert.NotNull(version);
        Assert.Equal(1, version!.Major);
    }
}
