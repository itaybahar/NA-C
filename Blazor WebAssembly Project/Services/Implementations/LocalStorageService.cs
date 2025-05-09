using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    /// <summary>
    /// Implementation that adapts Blazored.LocalStorage to work with our application's ILocalStorageService
    /// </summary>
    public class AppLocalStorageService : Blazor_WebAssembly.Services.Interfaces.ILocalStorageService
    {
        private readonly Blazored.LocalStorage.ILocalStorageService _blazoredLocalStorage;

        public AppLocalStorageService(Blazored.LocalStorage.ILocalStorageService blazoredLocalStorage)
        {
            _blazoredLocalStorage = blazoredLocalStorage ?? throw new ArgumentNullException(nameof(blazoredLocalStorage));
        }

        // Implement the methods required by our custom ILocalStorageService interface
        // These delegate to the Blazored.LocalStorage implementation

        public async Task<T> GetItemAsync<T>(string key)
        {
            return await _blazoredLocalStorage.GetItemAsync<T>(key);
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            await _blazoredLocalStorage.SetItemAsync(key, value);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _blazoredLocalStorage.RemoveItemAsync(key);
        }

        // The rest of these methods are not part of your custom interface but are
        // kept for potential future use if you expand the interface

        public ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.GetItemAsync<T>(key, cancellationToken);
        }

        public ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.SetItemAsync(key, value, cancellationToken);
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.RemoveItemAsync(key, cancellationToken);
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.ClearAsync(cancellationToken);
        }

        public ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.GetItemAsStringAsync(key, cancellationToken);
        }

        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.ContainKeyAsync(key, cancellationToken);
        }

        public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.LengthAsync(cancellationToken);
        }

        public ValueTask<string> KeyAsync(int index, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.KeyAsync(index, cancellationToken);
        }

        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.KeysAsync(cancellationToken);
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.SetItemAsStringAsync(key, data, cancellationToken);
        }

        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.RemoveItemsAsync(keys, cancellationToken);
        }
    }
}
