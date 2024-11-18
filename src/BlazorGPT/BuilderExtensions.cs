using BlazorGPT.Components.Memories;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using BlazorGPT.Settings;
using BlazorGPT.Settings.PluginSelector;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorGPT
{
    public static class BuilderExtensions
    {
        public static IServiceCollection AddBlazorGPT(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IConfiguration>(config);

            services.AddMemoryCache();


            services.Configure<PipelineOptions>(config.GetSection("PipelineOptions")); ;


            if (config["Database"] == "Sqlite")
            {
                services.AddDbContextFactory<BlazorGptDBContext>(options =>
                {
                    options.UseSqlite(config.GetConnectionString("BlazorGptDB"), o =>
                    {
                       // o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                });
            }
            else
            {
                services.AddDbContextFactory<BlazorGptDBContext>(options =>
                {
                    options.UseSqlServer(config.GetConnectionString("BlazorGptDB"), o =>
                    {
                        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                });
            }

            services.AddScoped<KernelService>();
     
            services.AddScoped<ScriptRepository>();
            services.AddScoped<QuickProfileRepository>();
            services.AddScoped<ConversationsRepository>();
            services.AddScoped<StateRepository>();

            services.AddScoped<SampleDataSeeder>();
            services.AddScoped<ChatWrapper>();

            services.AddScoped<IQuickProfileHandler, QuickProfileHandler>();
            services.AddScoped<IInterceptorHandler, InterceptorHandler>();
            services.AddScoped<IInterceptor, JsonStateInterceptor>();
            services.AddScoped<IInterceptor, StateFileSaveInterceptor>();
            services.AddSingleton<StateHasChangedInterceptorService>();
            services.AddScoped<IInterceptor, StateHasChangedInterceptor>();
            services.AddScoped<IInterceptor, EmbeddingsInterceptor>();
            services.AddScoped<IInterceptor, HandleBarsPluginsInterceptor>();
            services.AddScoped<PluginsRepository>();
            services.AddScoped<InterceptorRepository>();
            services.AddScoped<IInterceptor, HandleBarsPluginsInterceptor>();

            services.AddScoped<ConversationInterop>();

            services.AddScoped<ModelConfigurationService>();
            services.AddScoped<InterceptorConfigurationService>();
            services.AddScoped<PluginsConfigurationService>();

            services.AddSingleton<ConversationTreeState>();

            services.AddScoped<UserStorageService>();

            services.AddSingleton<CurrentConversationState>();
            services.AddTransient<FunctionCallingFilter>();

            services.AddScoped<MemoriesService>();

            return services;
        }

    }
}
