namespace BlazorGPT.Settings
{
    public class SettingsStateNotificationService
    {
        public event Func<SettingsChangedNotification, Task>? OnUpdate;
        public async Task NotifySettingsChanged(SettingsChangedNotification notification)
        {
            if (OnUpdate != null)
            {
               await OnUpdate.Invoke(notification);
            }
        }
    }

    public class SettingsChangedNotification
    {
        public Type Type { get; set; }
        public string UserId { get; set; }
    }
}
