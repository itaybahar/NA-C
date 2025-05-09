using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services
{
    public class ApiConnectionService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<ApiConnectionService> _logger;
        private bool? _isConnected;

        public ApiConnectionService(IJSRuntime jsRuntime, ILogger<ApiConnectionService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task<bool> EnsureConnectedAsync()
        {
            if (_isConnected.HasValue && _isConnected.Value)
                return true;

            try
            {
                var apiUrl = await _jsRuntime.InvokeAsync<string>("apiConnection.getBaseUrl");
                if (!string.IsNullOrEmpty(apiUrl))
                {
                    _isConnected = true;
                    return true;
                }

                // Try to discover API
                apiUrl = await _jsRuntime.InvokeAsync<string>("apiConnection.discoverApi");
                _isConnected = !string.IsNullOrEmpty(apiUrl);
                return _isConnected.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API connection");
                _isConnected = false;
                return false;
            }
        }

        public void Reset()
        {
            _isConnected = null;
        }
    }
}
