using System.Security.Claims;

namespace BlazorGPT.Web.Shared;

public class UserState
{
    public ClaimsPrincipal? User { get; private set; }
    
    public bool IsAdmin => User != null && User.IsInRole("Admin");
    public event Func<Task>? Notify;

    public async Task NotifyStateChangedAsync()
    {
        if (Notify != null) await Notify.Invoke();
    }


    public event Action? OnChange;

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }

    public async Task SetUser(ClaimsPrincipal user)
    {
        User = user;
        NotifyStateChanged();
    }
}