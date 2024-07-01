using System.Reflection;

namespace BlazorGPT.Web;

public static class PluginsLoader
{
    private static List<Assembly>? _cachedAssemblies;

    private static IEnumerable<Assembly> GetAssembliesFromDisk()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        var files = Directory.GetFiles(path, "*.dll");
        foreach (var file in files)
        {
            var assembly = Assembly.LoadFile(file);
            yield return assembly;
        }
    }

    public static IEnumerable<Assembly> GetAssembliesFromPluginsFolder()
    {
        return _cachedAssemblies ??= GetAssembliesFromDisk().ToList();
    }
}