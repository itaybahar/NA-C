// File: Blazor WebAssembly Project/Utilities/BrowserConsoleLogger/BrowserConsoleLoggerProvider.cs
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Utilities.BrowserConsoleLogger
{
    public class BrowserConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private bool _initialized = false;

        public BrowserConsoleLoggerProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new BrowserLogger(_jsRuntime, categoryName);
        }

        public void Dispose()
        {
            // No resources to dispose
            GC.SuppressFinalize(this);
        }

        private class BrowserLogger : ILogger
        {
            private readonly IJSRuntime _jsRuntime;
            private readonly string _categoryName;

            public BrowserLogger(IJSRuntime jsRuntime, string categoryName)
            {
                _jsRuntime = jsRuntime;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) => default!;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);

                // Format the log entry
                var prefix = $"[{logLevel}] {_categoryName}: ";

                try
                {
                    // Only log if not during startup to avoid timing issues
                    if (_jsRuntime is IJSInProcessRuntime)
                    {
                        // For synchronous calls when available
                        ((IJSInProcessRuntime)_jsRuntime).InvokeVoid("console.log", prefix + message);

                        if (exception != null)
                        {
                            ((IJSInProcessRuntime)_jsRuntime).InvokeVoid("console.error",
                                $"[{logLevel}] Exception: {exception.Message}");
                        }
                    }
                    else
                    {
                        // Use a fire-and-forget approach for async logging
                        // Wrap in Task.Run to prevent blocking
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _jsRuntime.InvokeVoidAsync("console.log", prefix + message);

                                if (exception != null)
                                {
                                    await _jsRuntime.InvokeVoidAsync("console.error",
                                        $"[{logLevel}] Exception: {exception.Message}");
                                }
                            }
                            catch
                            {
                                // Suppress errors
                            }
                        });
                    }
                }
                catch
                {
                    // Suppress errors from JS runtime to avoid disrupting the application
                }
            }
        }
    }

    // Extension method to make registration cleaner
    public static class BrowserConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddBrowserConsole(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, BrowserConsoleLoggerProvider>();
            return builder;
        }
    }
}
