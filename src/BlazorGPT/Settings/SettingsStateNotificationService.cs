namespace BlazorGPT.Settings
{
    public class SettingsStateNotificationService
    {

        //   public Task<Action<SettingsChangedNotification>> OnSettingsChanged;
        public event Func<SettingsChangedNotification, Task>? OnUpdate;

        //public event Action<SettingsChangedNotification> OnSettingsChanged;

        public async Task NotifySettingsChanged(SettingsChangedNotification notification)
        {
     //       OnSettingsChanged?.Invoke(notification);

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
