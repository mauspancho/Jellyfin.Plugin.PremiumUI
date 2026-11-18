using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.PremiumUI.Tests;

/// <summary>
/// Minimal IApplicationPaths implementation backed by a temporary root.
/// </summary>
internal sealed class TestApplicationPaths : IApplicationPaths
{
    public TestApplicationPaths(string root)
    {
        ProgramDataPath = root;
        WebPath = Path.Combine(root, "web");
        ProgramSystemPath = root;
        DataPath = Path.Combine(root, "data");
        ImageCachePath = Path.Combine(root, "cache", "images");
        PluginsPath = Path.Combine(root, "plugins");
        PluginConfigurationsPath = Path.Combine(root, "plugins", "configurations");
        LogDirectoryPath = Path.Combine(root, "log");
        ConfigurationDirectoryPath = Path.Combine(root, "config");
        SystemConfigurationFilePath = Path.Combine(root, "config", "system.xml");
        CachePath = Path.Combine(root, "cache");
        TempDirectory = Path.Combine(root, "cache", "temp");
        VirtualDataPath = "%DataPath%";
        TrickplayPath = Path.Combine(root, "cache", "trickplay");
        BackupPath = Path.Combine(root, "backups");
    }

    public string ProgramDataPath { get; }

    public string WebPath { get; }

    public string ProgramSystemPath { get; }

    public string DataPath { get; }

    public string ImageCachePath { get; }

    public string PluginsPath { get; }

    public string PluginConfigurationsPath { get; }

    public string LogDirectoryPath { get; }

    public string ConfigurationDirectoryPath { get; }

    public string SystemConfigurationFilePath { get; }

    public string CachePath { get; }

    public string TempDirectory { get; }

    public string VirtualDataPath { get; }

    public string TrickplayPath { get; }

    public string BackupPath { get; }

    public void MakeSanityCheckOrThrow()
    {
    }

    public void CreateAndCheckMarker(string path, string markerName, bool recursive = false)
    {
    }
}
