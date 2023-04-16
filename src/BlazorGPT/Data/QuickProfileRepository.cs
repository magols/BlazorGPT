using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Data;

public class QuickProfileRepository
{
    private readonly IDbContextFactory<BlazorGptDBContext> _dbContextFactory;

    public QuickProfileRepository(IDbContextFactory<BlazorGptDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }


    // get all quickprofiles
    public async Task<IList<QuickProfile>> GetQuickProfiles()
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        return await ctx.QuickProfiles
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IList<QuickProfile>> GetQuickProfiles(string? userId, InsertAt? conversationLocation = null)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        if (conversationLocation == null)
            return await ctx.QuickProfiles
                .Where(q => q.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();


        return await ctx.QuickProfiles.Where(q => q.UserId == userId)
            .Where(p => p.InsertAt == conversationLocation)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }


    // update a quickprofile
    public async Task<bool> UpdateQuickProfile(QuickProfile quickProfile)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        ctx.QuickProfiles.Update(quickProfile);
        var res = await ctx.SaveChangesAsync();
        return res == 1;
    }

    // save a quickprofile
    public async Task<bool> SaveQuickProfile(QuickProfile quickProfile)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        ctx.QuickProfiles.Add(quickProfile);
        var res = await ctx.SaveChangesAsync();
        return res == 1;
    }

    // delete a quickprofile
    public async Task DeleteQuickProfile(Guid id)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var quickProfile = await ctx.QuickProfiles.FindAsync(id);
        if (quickProfile == null)
            return;

        ctx.QuickProfiles.Remove(quickProfile);
        await ctx.SaveChangesAsync();
    }
}