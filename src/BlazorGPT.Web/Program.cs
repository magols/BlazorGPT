using BlazorGPT;
using BlazorGPT.Data;
using BlazorGPT.Managers;
using BlazorGPT.Web.Areas.Identity;
using BlazorGPT.Web.Data;
using BlazorGPT.Web.Shared;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using OpenAI.GPT3.Extensions;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("UserDB") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.Configure<PipelineOptions>(
    builder.Configuration.GetSection("PipelineOptions")); ;

builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();


builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<TokenProvider>();

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();


builder.Services.AddOpenAIService();


builder.Services.AddScoped<ConversationInterop>();
builder.Services.AddMediaQueryService();
builder.Services.AddScoped<IResizeListener, ResizeListener>();

builder.Services.AddScoped<ScriptRepository>();
builder.Services.AddScoped<QuickProfileRepository>();
builder.Services.AddScoped<ConversationsRepository>();
builder.Services.AddScoped<StateRepository>();

builder.Services.AddScoped<SampleDataSeeder>();

builder.Services.AddSingleton<StateManager>();

builder.Services.AddScoped<ChatWrapper>();

builder.Services.AddScoped<IQuickProfileHandler, QuickProfileHandler>();
builder.Services.AddScoped<IInterceptorHandler, InterceptorHandler>();
builder.Services.AddScoped<IInterceptor,JsonStateInterceptor>();
builder.Services.AddScoped<IInterceptor, StructurizrDslInterceptor>();
builder.Services.AddScoped<IInterceptor, StateFileSaveInterceptor>();

builder.Services.AddScoped<IInterceptor, StateHasChangedInterceptor>();


// register the GPT context
builder.Services.AddDbContextFactory<BlazorGptDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("BlazorGptDB"), o =>
    {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});


var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();