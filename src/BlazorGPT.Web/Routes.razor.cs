using BlazorGPT.Pages;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace BlazorGPT.Web;

public partial class Routes
{
    static IEnumerable<Assembly>? AvailableAssemblies { get; set; }

    protected override void OnInitialized()
    {
        if (AvailableAssemblies == null)
        {
            LoadAssemblies();
        }
    }

    private void LoadAssemblies()
    {

        AvailableAssemblies = new List<Assembly> { typeof(ConversationPage).Assembly };

        var assemblies = PluginsLoader.GetAssembliesFromPluginsFolder();

        foreach (var assembly in assemblies)
        {
            var components = assembly.ExportedTypes
                .Where(t =>
                    t.IsSubclassOf(typeof(ComponentBase)));

            foreach (var component in components)
            {
                var allAttributes = component.GetCustomAttributes(inherit: true);
                var foundAttrs =
                    allAttributes.OfType<RouteAttribute>().ToArray();

                if (foundAttrs.Length > 0)
                {
                    AvailableAssemblies = AvailableAssemblies.Append(assembly);
                }
            }
        }

    }
}

