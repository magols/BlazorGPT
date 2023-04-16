using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Data;

public class ScriptRepository {
    private IDbContextFactory<BlazorGptDBContext> _dbContextFactory;

    public ScriptRepository(IDbContextFactory<BlazorGptDBContext> dbDbContextFactoryFactory) {
        _dbContextFactory = dbDbContextFactoryFactory;
    }

    public async Task<bool> CreateScript(string userId, string newName)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        await ctx.Scripts.AddAsync(new Script(){Name = newName, UserId = userId});
        var res = await ctx.SaveChangesAsync();
        return res == 1;
    }

    public async Task<bool> DeleteScript(Guid id)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var script = await ctx.Scripts.FindAsync(id);
        if (script == null)
            return false;

        ctx.Scripts.Remove(script);
        var res = await ctx.SaveChangesAsync();
        return res == 1;
    }

    // get all scripts
    public async Task<List<Script>> GetScripts(string userId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        return await ctx.Scripts.Where(s => s.UserId == userId).ToListAsync();
    }

    public async Task<bool> SaveScript(Script script)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        ctx.Scripts.Update(script);
        //ctx.Scripts.Update(script);
        var res = await ctx.SaveChangesAsync();

        await UpdateSteps(script.Steps);

        return res > 0;
    }


    // get script with included navigation properties
    public async Task<Script> GetScript(Guid id)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var script = await ctx.Scripts
            //.Include(o => o.SystemMessage)
            .Include(o => o.Steps)
            //.ThenInclude(o => o.Message)
            .SingleAsync(o => o.Id == id);

        script.Steps = script.Steps.OrderBy(s => s.SortOrder).ToList();
        return script;
    }

    public async Task<IEnumerable<ScriptStep>> GetSteps(Guid scriptId)
    {

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var script = await ctx.Scripts.Include(o => o.Steps)
           // .ThenInclude(o => o.Message)
            .SingleAsync(o => o.Id == scriptId);
        if (script == null)
            return new List<ScriptStep>();

        return script.Steps.OrderBy(s => s.SortOrder);  

    }

    public async Task AddStep(Script script, ScriptStep step)
    {

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        //await ctx.ScriptSteps.AddAsync(step);


        var s = await ctx.Scripts.SingleAsync(o => o.Id == script.Id);
        s.Steps.Add(step);
        await ctx.SaveChangesAsync();


    }

    public async Task SaveStep(ScriptStep value)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        ctx.ScriptSteps.Update(value);
        await ctx.SaveChangesAsync();

    }

    public async Task UpdateSteps(IEnumerable<ScriptStep> steps)
    {

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        foreach (var step in steps)
        {
            ctx.ScriptSteps.Attach(step);
            ctx.Entry(step).State = EntityState.Modified;
        }

        await ctx.SaveChangesAsync();

    }

    public async Task DeleteStep(ScriptStep value)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        ctx.ScriptSteps.Remove(value);
        await ctx.SaveChangesAsync();
    }

    public async Task SaveSystemPrompt(Guid scriptId, string systemPrompt)
    {
            await using var ctx = await _dbContextFactory.CreateDbContextAsync();
            //var msg = await ctx.Messages.SingleAsync(m => m.Id == messageId);

            var scr = await ctx.Scripts.FindAsync(scriptId);
            scr.SystemMessage = systemPrompt;
            
//            ctx.Messages.Update(scriptSystemMessage);
////            ctx.Messages.Attach(scriptSystemMessage);
//            ctx.Entry(scriptSystemMessage).State = EntityState.Modified;
            await ctx.SaveChangesAsync();
    }
}