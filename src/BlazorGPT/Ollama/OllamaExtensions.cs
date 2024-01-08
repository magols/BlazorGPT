using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace BlazorGPT.Ollama;

public static class OllamaExtensions
{
    public static IKernelBuilder AddCustomOllamaChatCompletion(
        this IKernelBuilder builder,
        string modelId,
        string baseUrl,
        string? serviceId = null)
    {
        Func<IServiceProvider, object, OllamaChatCompletion> implementationFactory = (Func<IServiceProvider, object, OllamaChatCompletion>)((provider, _) => new OllamaChatCompletion(modelId, baseUrl, provider.GetRequiredService<HttpClient>(), provider.GetService<ILoggerFactory>()));
        builder.Services.AddKeyedSingleton<IChatCompletionService>((object)serviceId, (Func<IServiceProvider, object, IChatCompletionService>)implementationFactory);
        return builder;
    }
}