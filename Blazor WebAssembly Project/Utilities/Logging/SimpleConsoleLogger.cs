using Microsoft.Extensions.Logging;
using System;

namespace Blazor_WebAssembly.Utilities.Logging
{
    public class SimpleConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleConsoleLogger(categoryName);
        }

        public void Dispose() { }

        private class SimpleConsoleLogger : ILogger
        {
            private readonly string _categoryName;

            public SimpleConsoleLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            IDisposable ILogger.BeginScope<TState>(TState state) => default!;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Console.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            }
        }
    }
} 