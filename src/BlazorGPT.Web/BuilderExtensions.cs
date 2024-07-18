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
}