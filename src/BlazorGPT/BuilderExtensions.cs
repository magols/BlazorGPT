﻿using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using BlazorGPT.Settings;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorGPT
{
    public static class BuilderExtensions
    {
        public static void AddBlazorGPT(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IConfiguration>(config);
            services.Configure<PipelineOptions>(config.GetSection("PipelineOptions")); ;

            services.AddDbContextFactory<BlazorGptDBContext>(options =>
            {
                options.UseSqlServer(config.GetConnectionString("BlazorGptDB"), o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            });
            
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
            services.AddScoped<IInterceptor, StructurizrDslInterceptor>();
            services.AddScoped<IInterceptor, StateFileSaveInterceptor>();
            services.AddSingleton<StateHasChangedInterceptorService>();
            services.AddScoped<IInterceptor, StateHasChangedInterceptor>();
            services.AddScoped<IInterceptor, EmbeddingsInterceptor>();
            services.AddScoped<IInterceptor, PluginInterceptor>();
            services.AddScoped<PluginsRepository>();
            services.AddScoped<InterceptorRepository>();
            services.AddScoped<IInterceptor, PluginInterceptor>();
        }
    }
}