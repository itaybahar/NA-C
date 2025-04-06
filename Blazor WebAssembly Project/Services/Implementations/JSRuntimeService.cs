using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.JSInterop;

public class JSRuntimeService : IJSRuntimeService
{
    private readonly IJSRuntime _jsRuntime;

    public JSRuntimeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetItemFromLocalStorage(string key)
    {
        return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
    }

    public async Task SetItemInLocalStorage(string key, string value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public async Task RemoveItemFromLocalStorage(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }
}
