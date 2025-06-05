using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Utilities.Logging
{
    public class BrowserConsoleLogger
    {
        private readonly IJSRuntime _jsRuntime;

        public BrowserConsoleLogger(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        public async Task LogAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorConsoleLog.log", message);
            }
            catch 
            {
                // Ignore errors in browser console logging
            }
        }

        public async Task LogWarningAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorConsoleLog.warn", message);
            }
            catch
            {
                // Ignore errors in browser console logging
            }
        }

        public async Task LogErrorAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorConsoleLog.error", message);
            }
            catch
            {
                // Ignore errors in browser console logging
            }
        }
    }
} 