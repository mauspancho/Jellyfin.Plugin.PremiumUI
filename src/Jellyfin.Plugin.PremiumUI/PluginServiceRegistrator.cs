using Jellyfin.Plugin.PremiumUI.Diagnostics;
using Jellyfin.Plugin.PremiumUI.Hosting;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PremiumUI;

/// <summary>
/// Registers plugin services and the ASP.NET Core startup filter.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IntegrationStatus>();
        serviceCollection.AddSingleton<PremiumUiWebAssets>();
        serviceCollection.AddSingleton<IBaseUrlProvider, ServerBaseUrlProvider>();
        serviceCollection.AddTransient<IStartupFilter, PremiumUiStartupFilter>();
    }
}
