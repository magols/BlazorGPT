using Blazored.LocalStorage;

namespace BlazorGPT.Pipeline
{
    public class UserStorageService(ILocalStorageService LocalStorage)
    {
        public async Task<string> GetUserIdFromLocalStorage()
        {
            var userId = await LocalStorage.GetItemAsync<string>("userId");
            if (userId == null)
            {
                userId = Guid.NewGuid().ToString();
                await LocalStorage.SetItemAsync("userId", userId);
            }
            return userId;
        }

    }
}


