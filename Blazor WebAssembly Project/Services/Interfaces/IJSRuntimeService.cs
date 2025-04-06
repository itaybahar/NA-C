using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IJSRuntimeService
    {
        Task<string> GetItemFromLocalStorage(string key);
        Task SetItemInLocalStorage(string key, string value);
        Task RemoveItemFromLocalStorage(string key);
    }
}
