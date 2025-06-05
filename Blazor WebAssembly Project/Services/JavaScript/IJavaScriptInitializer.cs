using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.JavaScript
{
    public interface IJavaScriptInitializer
    {
        Task InitializeAsync();
    }
} 