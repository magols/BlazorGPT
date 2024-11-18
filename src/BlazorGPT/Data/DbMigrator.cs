using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorGPT.Data;

public static class DbMigrator
{
    public static void MigrateBlazorGptDb(this WebApplication app)
    {
        var config = app.Configuration;
        if (!bool.TryParse(config["ApplyMigrationsAtStart"], out var applyMigrationsAtStart) ||
            !applyMigrationsAtStart) return;
        using var scope = app.Services.CreateScope();
        using var ctx = scope.ServiceProvider.GetRequiredService<BlazorGptDBContext>();

        if (ctx.Database.IsSqlServer()) ctx.Database.Migrate();
        if (ctx.Database.IsSqlite()) ctx.Database.EnsureCreated();
    }
}