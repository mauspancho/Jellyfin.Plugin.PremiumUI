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
        original.Design.ShadowColor = "#445566";
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
        Assert.Equal("#445566", restored.Design.ShadowColor);
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

    // El catálogo de Jellyfin compara la versión instalada con la del manifest:
    // si el ensamblado y meta.json no coinciden, el plugin nunca se actualiza.
    [Fact]
    public void Plugin_AssemblyVersionMatchesMetaJson()
    {
        var version = typeof(PremiumUiPlugin).Assembly.GetName().Version;
        Assert.NotNull(version);

        var meta = Path.Combine(RepositoryRoot(), "build", "meta.json");
        Assert.True(File.Exists(meta), $"no se encontró {meta}");

        using var stream = File.OpenRead(meta);
        using var document = System.Text.Json.JsonDocument.Parse(stream);
        var declared = document.RootElement.GetProperty("version").GetString();

        Assert.Equal(new Version(declared!), version);
    }

    [Fact]
    public void Plugin_TargetAbiMatchesCurrentJellyfin()
    {
        var meta = Path.Combine(RepositoryRoot(), "build", "meta.json");
        using var stream = File.OpenRead(meta);
        using var document = System.Text.Json.JsonDocument.Parse(stream);

        Assert.Equal("12.4.0.0", document.RootElement.GetProperty("targetAbi").GetString());
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "build", "meta.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"no se encontró build/meta.json partiendo de {AppContext.BaseDirectory}");
    }
}
