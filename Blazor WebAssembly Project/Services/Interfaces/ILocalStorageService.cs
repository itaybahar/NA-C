using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    /// <summary>
    /// Simple interface for local storage operations used in our application
    /// </summary>
    public interface ILocalStorageService
    {
        Task<T> GetItemAsync<T>(string key);
        Task SetItemAsync<T>(string key, T value);
        Task RemoveItemAsync(string key);
    }

    public interface IApiDiscoveryService
    {
        Task<string> DiscoverApiUrl();
    }
}
