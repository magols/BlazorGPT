using BlazorGPT.Pipeline.Interceptors;
using Microsoft.AspNetCore.HttpOverrides;

namespace BlazorGPT.Web;

public static class BuilderExtensions
{ 
    public static WebApplication UseProxyHeaders(this WebApplication app)
    {
        var forwardingOptions = new ForwardedHeadersOptions()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            ForwardLimit = 3,
        };
        forwardingOptions.KnownNetworks.Clear(); // Loopback by default, this should be temporary
        forwardingOptions.KnownProxies.Clear(); // Update to include

        app.UseForwardedHeaders(forwardingOptions);

        return app;
    }

    public static IServiceCollection AddScrutorScanning(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromCallingAssembly()
            .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .FromAssembliesOf(typeof(BlazorGPT._Imports))
            .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .FromAssemblies(PluginsLoader.GetAssembliesFromPluginsFolder())
            .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .AsSelf()
            .WithTransientLifetime());


        services.Scan(scan => scan
            .FromCallingAssembly()
            .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .FromAssembliesOf(typeof(BlazorGPT._Imports))
            .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .FromAssemblies(PluginsLoader.GetAssembliesFromPluginsFolder())
            .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .AsSelf()
            .WithScopedLifetime());

        return services;
    }
}