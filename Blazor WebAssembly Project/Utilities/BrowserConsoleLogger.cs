using Microsoft.JSInterop;
using System.Text;

namespace Blazor_WebAssembly.Utilities.BrowserConsoleLogger
{
    public static class DtoHelpers
    {
        private static IJSRuntime? _jsRuntime;

        public static void Initialize(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public static void Log(string message)
        {
            if (_jsRuntime != null)
            {
                _jsRuntime.InvokeVoidAsync("console.log", message);
            }
        }
    }

    public class CustomConsoleWriter : TextWriter
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly StringBuilder _buffer = new();

        public CustomConsoleWriter(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override void Write(char value)
        {
            // Collect characters until we get a newline
            if (value == '\n')
            {
                // Send the buffered line to the browser console
                string line = _buffer.ToString().TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    _jsRuntime.InvokeVoidAsync("blazorConsoleLog.log", line);
                }
                _buffer.Clear();
            }
            else
            {
                _buffer.Append(value);
            }
        }

        public override Encoding Encoding => Encoding.UTF8;
    }

}