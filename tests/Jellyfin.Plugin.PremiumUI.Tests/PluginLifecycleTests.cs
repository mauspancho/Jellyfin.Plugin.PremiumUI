using System.Xml.Serialization;
using Jellyfin.Plugin.PremiumUI.Configuration;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class PluginLifecycleTests : IDisposable
{
    private readonly string _root;

    public PluginLifecycleTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "premiumui-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "plugins"));
        Directory.CreateDirectory(Path.Combine(_root, "plugins", "configurations"));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }

    [Fact]
    public void Plugin_CreatesDefaultConfigurationFile()
    {
        var paths = new TestApplicationPaths(_root);

        _ = new PremiumUiPlugin(paths, new TestXmlSerializer());

        var configFile = Path.Combine(paths.PluginConfigurationsPath, "Jellyfin.Plugin.PremiumUI.xml");
        Assert.True(File.Exists(configFile));
    }

    [Fact]
    public void Plugin_UpdateConfiguration_PersistsNormalizedValues()
    {
        var paths = new TestApplicationPaths(_root);
        var plugin = new PremiumUiPlugin(paths, new TestXmlSerializer());

        plugin.UpdateConfiguration(new PluginConfiguration
        {
            Design = new DesignOptions { Theme = "inventado", Transparency = 900 },
            Home = new HomeOptions { SectionOrder = ["series", "series", "desconocida"] }
        });

        var saved = new TestXmlSerializer().DeserializeFromFile(
            typeof(PluginConfiguration),
            Path.Combine(paths.PluginConfigurationsPath, "Jellyfin.Plugin.PremiumUI.xml")) as PluginConfiguration;

        Assert.NotNull(saved);
        Assert.Equal("cinema", saved!.Design.Theme);
        Assert.Equal(100, saved.Design.Transparency);
        Assert.Equal(
            ["series", "hero", "estudios", "media", "continue", "recent", "movies", "recommendations"],
            saved.Home.SectionOrder);
    }

    [Fact]
    public void GetPages_ReturnsResolvableEmbeddedResource()
    {
        var plugin = new PremiumUiPlugin(new TestApplicationPaths(_root), new TestXmlSerializer());

        var page = Assert.Single(plugin.GetPages());

        Assert.Equal("Jellyfin Premium UI", page.Name);
        Assert.NotNull(typeof(PremiumUiPlugin).Assembly.GetManifestResourceStream(page.EmbeddedResourcePath));
    }

    [Fact]
    public void Plugin_ReloadsSavedConfiguration()
    {
        var paths = new TestApplicationPaths(_root);
        var serializer = new TestXmlSerializer();
        var plugin = new PremiumUiPlugin(paths, serializer);
        plugin.UpdateConfiguration(new PluginConfiguration
        {
            Home = new HomeOptions { SectionOrder = ["movies", "hero"], ItemsPerSection = 6 }
        });

        var reloaded = new PremiumUiPlugin(paths, serializer);

        Assert.Equal(["movies", "hero", "estudios", "media", "continue", "recent", "series", "recommendations"], reloaded.Configuration.Home.SectionOrder);
        Assert.Equal(6, reloaded.Configuration.Home.ItemsPerSection);
    }
}
