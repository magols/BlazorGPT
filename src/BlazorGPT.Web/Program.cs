using System.Reflection;
using Blazored.LocalStorage;
using BlazorGPT;
using BlazorGPT.Components.Account;
using BlazorGPT.Web;
using BlazorGPT.Web.Data;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using BlazorGPT.Data.Model;
using Microsoft.AspNetCore.Identity.UI.Services;
using OpenTelemetry.Logs;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.Extensions.Configuration;



var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    //.WriteTo.Console(theme: AnsiConsoleTheme.Literate, applyThemeToRedirectedOutput: true)
    //.WriteTo.File(@"logs/log.txt", shared: true, rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Error("hj�lp");

builder.Services.AddSerilog();
builder.Services.AddTransient<ILoggerFactory>(b =>
{
    var loggerFactory = LoggerFactory.Create(logBuilder =>
    {
        logBuilder.SetMinimumLevel(LogLevel.Information);
        logBuilder.AddOpenTelemetry(options =>
        {
            options.AddConsoleExporter();

        });
    });
    loggerFactory.AddSerilog();
    return loggerFactory;
});

var connectionString = builder.Configuration.GetConnectionString("UserDB") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddBlazorGPT(builder.Configuration);

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

builder.Services.AddRadzenComponents();


builder.Services.AddMediaQueryService();
builder.Services.AddScoped<IResizeListener, ResizeListener>();

builder.WebHost.UseWebRoot("wwwroot");

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseProxyHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAntiforgery();

app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

List<Assembly> pluginAssemblies = PluginsLoader.GetAssembliesFromPluginsFolder().ToList();
pluginAssemblies.Add(typeof(Conversation).Assembly);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(pluginAssemblies.ToArray());

app.MapAdditionalIdentityEndpoints();

app.Run();