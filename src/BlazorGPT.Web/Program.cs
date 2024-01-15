using Blazored.LocalStorage;
using BlazorGPT;
using BlazorGPT.Components.Account;
using BlazorGPT.Components.FileUpload;
using BlazorGPT.Data;
using BlazorGPT.Embeddings;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using BlazorGPT.Web;
using BlazorGPT.Web.Data;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using BlazorGPT.Data.Model;
using BlazorGPT.Settings;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("UserDB") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

if (!string.IsNullOrEmpty( builder.Configuration["SendGridApiKey"]))
{
    builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();
    builder.Services.AddSingleton<IEmailSender<IdentityUser>, SendGridIdentityEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender<IdentityUser>, IdentityNoOpEmailSender>();
}

builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredLocalStorage();

builder.Services.Configure<PipelineOptions>(
    builder.Configuration.GetSection("PipelineOptions")); ;

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services.AddScoped<ModelConfigurationService>();

builder.Services.AddScoped<ConversationInterop>();

builder.Services.AddMediaQueryService();
builder.Services.AddScoped<IResizeListener, ResizeListener>();

builder.Services.AddScoped<ScriptRepository>();
builder.Services.AddScoped<QuickProfileRepository>();
builder.Services.AddScoped<ConversationsRepository>();
builder.Services.AddScoped<StateRepository>();

builder.Services.AddScoped<SampleDataSeeder>();

builder.Services.AddScoped<KernelService>();


builder.Services.AddScoped<ChatWrapper>();

builder.Services.AddScoped<IQuickProfileHandler, QuickProfileHandler>();
builder.Services.AddScoped<IInterceptorHandler, InterceptorHandler>();
builder.Services.AddScoped<IInterceptor,JsonStateInterceptor>();
builder.Services.AddScoped<IInterceptor, StructurizrDslInterceptor>();
builder.Services.AddScoped<IInterceptor, StateFileSaveInterceptor>();
builder.Services.AddSingleton<StateHasChangedInterceptorService>();
builder.Services.AddScoped<IInterceptor, StateHasChangedInterceptor>();
builder.Services.AddScoped<IInterceptor, EmbeddingsInterceptor>();
builder.Services.AddScoped<PluginsRepository>();
builder.Services.AddScoped<InterceptorRepository>();
builder.Services.AddScoped<IInterceptor, PluginInterceptor>();



builder.WebHost.UseWebRoot("wwwroot");

// register the GPT context
builder.Services.AddDbContextFactory<BlazorGptDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("BlazorGptDB"), o =>
    {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAntiforgery();

app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Conversation).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();