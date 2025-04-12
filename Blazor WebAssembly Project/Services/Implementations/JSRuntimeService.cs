using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.JSInterop;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class JSRuntimeService : IJSRuntimeService
    {
        private readonly IJSRuntime _jsRuntime;

        public JSRuntimeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> GetItemFromLocalStorage(string key)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving from localStorage: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task SetItemInLocalStorage(string key, string value)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting localStorage: {ex.Message}");
            }
        }

        public async Task RemoveItemFromLocalStorage(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from localStorage: {ex.Message}");
            }
        }
    }
}
