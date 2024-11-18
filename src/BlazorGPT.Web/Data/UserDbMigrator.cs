using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Web.Data;

public static class UserDbMigrator
{
    public static void MigrateUserDbIfEnabled(this WebApplication app)
    {
        var config = app.Configuration;
        if (!bool.TryParse(config["ApplyMigrationsAtStart"], out var applyMigrationsAtStart) ||
            !applyMigrationsAtStart) return;


        using var scope = app.Services.CreateScope();
        using var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (ctx.Database.IsSqlServer()) ctx.Database.Migrate();
        if (ctx.Database.IsSqlite()) ctx.Database.EnsureCreated();
    }
}